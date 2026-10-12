#nullable enable
using System.Collections;
using System.Text;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Hoja y Brote al estilo Magic Sort (T11; 02 §9.1 M1-M2; propuesta de D-007 b.2 M1-M2; ficha de Magic Sort §3; D-002 y
    /// D-003): en los estrenos reales (43 con aguaje y 163 con camu camu) el aspecto cerrado, el rechazo como origen (solo la
    /// sacudida), el desbloqueo animado del paso Open (la hoja cae girando al agua en 500 ms × factor; el brote germina en 350
    /// ms × factor), Marea al terminar y a mitad del paso (02 §14) y, en el banco de <see cref="MechanicTestKit"/>, el cierre
    /// con <c>SnapAll</c> a mitad del paso y los pasos sin asignaciones con el tope de 44 partículas.
    /// </summary>
    public sealed class HojaBroteTests
    {
        private const int Aguaje = 4; // índice de motor (glosario §9)
        private const int CamuCamu = 6;
        private const int Lulo = 11;
        private const int RioNegro = 14;

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
        public IEnumerator Hoja43_ClosedVisualAndReject()
        {
            yield return MechanicTestKit.LoadPlaying(43);
            GameBootstrap boot = GameBootstrap.Current!;
            int hoja = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Hoja);
            Assert.That(boot.Flow!.Session.Current[hoja].MechanicKey, Is.EqualTo(Aguaje), "el 43 estrena la Hoja con aguaje (02 §9.1 M1)");
            CuiaView view = boot.View!.Views[hoja];
            HojaOverlay leaf = HojaOf(view);
            Assert.That(leaf.Visible && leaf.ClosedLeafVisible, Is.True, "hoja de palma cerrada sobre la boca");
            Assert.That(leaf.ShownKey, Is.EqualTo(Aguaje));
            Assert.That(leaf.FruitVisible, Is.True, "con la fruta llave en el centro");
            Assert.That(leaf.FruitSprite, Is.SameAs(view.OverlayHost.Art.FruitFlight(Aguaje)), "la fruta llave es el aguaje");
            Assert.That(leaf.FruitAngleDeg, Is.EqualTo(HojaOverlay.FruitTiltDeg).Within(0.01f), "girada sobre el medallón apaisado: no se lee como un ojo");
            Assert.That(leaf.FruitGlowAlpha, Is.EqualTo(0f), "sin luz en reposo");
            Assert.That(leaf.FallingLeafVisible || leaf.IsAnimating, Is.False);

            string closed = Signature(view.OverlayHost.BodyFront);
            TapResult tap = boot.TapCuia(hoja);
            Assert.That(tap.Kind, Is.EqualTo(TapKind.Rejected), "cerrada no es origen (02 §9.1 M1): rechazo suave");
            Assert.That(tap.Selection, Is.EqualTo(-1), "nada queda seleccionado");
            bool shook = false;
            for (int t = 0; t < 240; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                shook |= Mathf.Abs(view.BodyOffset.x) > 0.5f;
                Assert.That(Signature(view.OverlayHost.BodyFront), Is.EqualTo(closed), "la hoja no cambia: ni brillo ni dedo guía (D-002)");
            }

            Assert.That(shook, Is.True, "solo la sacudida de la cuia (02 §4.1)");
            Assert.That(view.IsLifted || view.GlassBoosted, Is.False, "no se eleva ni brilla");
            Assert.That(leaf.IsAnimating, Is.False);
        }

        [UnityTest]
        public IEnumerator Hoja43_OpenStepDuration()
        {
            yield return MechanicTestKit.LoadPlaying(43);
            GameBootstrap boot = GameBootstrap.Current!;
            int hoja = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Hoja);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.HojaOpenedMask & (1 << hoja)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            HojaOverlay leaf = HojaOf(boot.View!.Views[hoja]);
            int discarded = boot.View.Fx.DiscardedCount;
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out _);
            int animatingMs = 0;
            int elapsed = 0;
            while (!boot.View.IsViewIdle)
            {
                Assert.That(elapsed, Is.LessThan(6000), "la vista no se quedó quieta");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                elapsed += PlayModeHelpers.FrameMs;
                if (leaf.IsAnimating)
                {
                    animatingMs += PlayModeHelpers.FrameMs;
                }
            }

            MechanicTestKit.AssertLogFollowsPlan(boot.View.Animator, in visual);
            MechanicStepRecord open = MechanicTestKit.Record(boot.View.Animator, MechanicStepKind.Open, hoja);
            Assert.That(open.Stage, Is.EqualTo(StepStage.B));
            Assert.That(open.Owner, Is.EqualTo(MechanicType.Hoja), "lo ejecuta el overlay de la Hoja");
            Assert.That(open.UsedDefault || open.Snapped, Is.False, "paso propio, completo");
            Assert.That(open.SpeedFactor, Is.EqualTo(boot.View.Animator.StepSpeedBase).Within(1e-4f), "sin recorte por el presupuesto");
            float expected = HojaOverlay.OpenStepMs * open.SpeedFactor;
            Assert.That(open.EndMs - open.StartMs, Is.EqualTo(expected).Within(0.05f), "500 ms × factor de velocidad (b.2 M1)");
            Assert.That((float)animatingMs, Is.EqualTo(expected).Within(PlayModeHelpers.FrameMs), "la hoja anima ese tiempo (± 1 fotograma)");
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta");
            Assert.That(boot.View.Fx.DiscardedCount, Is.EqualTo(discarded), "destellos y salpicadura, sin descartes por los topes del pool");
            TestContext.WriteLine("Nivel 43: " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(boot.View.Animator));

            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(boot.Flow!.Session.Current[hoja].Mechanic, Is.EqualTo(MechanicType.None), "abierta: cuia normal");
            Assert.That(leaf.Visible || leaf.ClosedLeafVisible || leaf.FallingLeafVisible || leaf.FruitVisible, Is.False, "al terminar, el overlay está oculto");
        }

        [UnityTest]
        public IEnumerator Hoja43_MidStepFalling()
        {
            yield return MechanicTestKit.LoadPlaying(43);
            GameBootstrap boot = GameBootstrap.Current!;
            int hoja = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Hoja);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.HojaOpenedMask & (1 << hoja)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            CuiaView view = boot.View!.Views[hoja];
            HojaOverlay leaf = HojaOf(view);
            MechanicTestKit.TapPour(boot, path[path.Length - 1], out _);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Open, hoja, 4000);
            Assert.That(leaf.IsAnimating, Is.True, "el paso Open llegó al overlay de la Hoja");

            // Mientras la boca del origen tapa la de la Hoja (D-006), la llave solo sube con la hoja que se abre: aún no salta.
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, hoja, 0.14f);
            Assert.That(leaf.StepProgress, Is.LessThan(HojaOverlay.FruitPopStart), "antes del salto");
            Assert.That(leaf.FruitAlpha, Is.GreaterThan(0.99f), "la fruta llave, opaca");
            Assert.That(leaf.FruitScale, Is.EqualTo(1f).Within(1e-4f), "todavía sin saltar");

            // Con la boca ya libre, la llave encaja (ficha de Magic Sort §3.3): salta a 1,3× y se queda opaca, sin pasar del
            // tope (no invade la fila de arriba), y después se deshace en luz (Glint).
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, hoja, 0.35f);
            Assert.That(leaf.StepProgress, Is.InRange(HojaOverlay.FruitPopEnd, HojaOverlay.FruitHoldEnd), "con la llave quieta");
            Assert.That(leaf.FruitAlpha, Is.GreaterThan(0.99f), "la fruta llave, opaca");
            Assert.That(leaf.FruitScale, Is.EqualTo(HojaOverlay.FruitPopScale).Within(0.01f), "ya saltó a 1,3×");
            Assert.That(leaf.FruitGlowAlpha, Is.EqualTo(0f), "todavía sin luz");
            Assert.That(leaf.KeyTopAboveLipU, Is.GreaterThan(0.5f).And.LessThanOrEqualTo(HojaOverlay.KeyTopMaxU), "el medallón, a la vista y bajo el tope");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, hoja, 0.46f);
            Assert.That(leaf.StepProgress, Is.InRange(HojaOverlay.FruitHoldEnd, HojaOverlay.FruitFadeEnd), "a mitad del fundido de la llave");
            Assert.That(leaf.FruitGlowAlpha, Is.GreaterThan(0.3f), "la llave se aclara hacia la luz");
            Assert.That(leaf.FruitGlowAlpha + (leaf.FruitAlpha * (1f - leaf.FruitGlowAlpha)), Is.LessThan(0.95f), "y se desvanece");

            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, hoja, 0.5f);
            Assert.That(leaf.IsAnimating, Is.True);
            Assert.That(leaf.StepProgress, Is.InRange(0.4f, 0.62f), "a mitad del paso");
            Assert.That(leaf.ClosedLeafVisible, Is.False, "ya se abrió");
            Assert.That(leaf.FallingLeafVisible, Is.True, "la hoja se ve cayendo");
            Assert.That(leaf.FallAlpha, Is.GreaterThan(0.95f), "entera hasta tocar el agua");
            Assert.That(leaf.FallDropU, Is.GreaterThan(0.3f), "ya está por debajo de la boca");
            Assert.That(Mathf.Abs(leaf.FallAngleDeg), Is.GreaterThan(20f), "cae girando");
            Assert.That(leaf.FallScale, Is.LessThan(1f).And.GreaterThan(HojaOverlay.WaterScale), "encoge hacia el agua");
            Assert.That(view.LiquidLevel, Is.GreaterThan(0.5f), "con el aguaje dentro");
            Assert.That(leaf.GlowAlpha, Is.EqualTo(0f), "cae con su verde, sin aclararse todavía");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, hoja, HojaOverlay.FruitFadeEnd + 0.03f);
            Assert.That(leaf.FruitAlpha + leaf.FruitGlowAlpha, Is.LessThan(0.05f), "la fruta llave ya se disolvió");
            Assert.That(leaf.FallingLeafVisible && leaf.FallAlpha > 0.95f, Is.True, "y la hoja sigue cayendo, entera");

            // Toca el agua: se desvanece aclarándose hacia la luz (Glint), sin mezclarse con el tinte, y acaba antes que el paso.
            float mid = (HojaOverlay.SplashAt + HojaOverlay.GoneAt) * 0.5f;
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, hoja, mid);
            Assert.That(leaf.StepProgress, Is.InRange(HojaOverlay.SplashAt, HojaOverlay.GoneAt), "a mitad del desvanecimiento");
            Assert.That(leaf.FallingLeafVisible, Is.True);
            Assert.That(leaf.GlowAlpha, Is.GreaterThan(0.2f), "la hoja se aclara hacia la luz");
            Assert.That(leaf.GlowAlpha + leaf.FallAlpha, Is.LessThan(0.9f), "y se desvanece");
            Assert.That(leaf.FallScale, Is.LessThan(HojaOverlay.WaterScale).And.GreaterThan(HojaOverlay.GoneScale), "encoge al hundirse");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, hoja, HojaOverlay.GoneAt + 0.04f);
            Assert.That(leaf.IsAnimating, Is.True, "el paso sigue");
            Assert.That(leaf.FallingLeafVisible || leaf.ClosedLeafVisible, Is.False, "pero de la hoja ya no queda nada");

            MechanicTestKit.StepToIdle(boot, 6000);
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(boot.Flow!.Session.Current[hoja].Mechanic, Is.EqualTo(MechanicType.None));
            Assert.That(leaf.Visible || leaf.FallingLeafVisible, Is.False, "al terminar, oculta");
        }

        [UnityTest]
        public IEnumerator Hoja43_KeySeatsWithTheMouthClear()
        {
            // La llave encaja (FruitPopEnd) y se queda quieta hasta FruitHoldEnd con el medallón a la vista: el origen, que D-006
            // dibuja delante de todo con la boca sobre la del destino, ya ha dejado libre la boca al volver a su ranura. En el 43
            // real, con los tiempos de vertido por defecto (08 §9: anim_return_ms es de Remote Config).
            yield return MechanicTestKit.LoadPlaying(43);
            GameBootstrap boot = GameBootstrap.Current!;
            int hoja = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Hoja);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.HojaOpenedMask & (1 << hoja)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            Pour opening = path[path.Length - 1];
            CuiaView view = boot.View!.Views[hoja];
            CuiaView source = boot.View.Views[opening.Source];
            HojaOverlay leaf = HojaOf(view);
            MechanicTestKit.TapPour(boot, opening, out _);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Open, hoja, 4000);

            // Al empezar el Open el origen aún vierte (su retorno arranca 20 ms después, al acabar la cola del chorro): la
            // comprobación tiene que ver ese solape, o el resto del caso no mediría nada.
            bool coveredAtStart = SourceCoversKey(source, view, leaf);
            Assert.That(coveredAtStart, Is.True, "al empezar el Open, la boca del origen tapa la de la Hoja (D-006)");
            float clearFrom = -1f;
            int seated = 0;
            int inFlight = 0;
            while (leaf.IsAnimating && leaf.StepProgress < HojaOverlay.FruitHoldEnd)
            {
                float t = leaf.StepProgress;
                bool covered = SourceCoversKey(source, view, leaf);
                clearFrom = covered ? -1f : (clearFrom < 0f ? t : clearFrom);
                if (t >= HojaOverlay.FruitPopEnd)
                {
                    seated++;
                    inFlight += source.InFlight ? 1 : 0;
                    Assert.That(covered, Is.False, "t = " + t.ToString("F3") + ": la silueta del origen tapa la llave encajada");
                }

                PlayModeHelpers.TickExact(boot, 4);
            }

            Assert.That(seated, Is.GreaterThan(10), "pasos de 4 ms con la llave encajada y quieta");
            Assert.That(clearFrom, Is.InRange(0f, HojaOverlay.FruitPopEnd), "la boca queda libre antes de que encaje la llave");
            TestContext.WriteLine("Nivel 43: el origen deja libre la llave desde t = " + clearFrom.ToString("F3") + " (salto de "
                + HojaOverlay.FruitPopStart.ToString("F2") + " a " + HojaOverlay.FruitPopEnd.ToString("F2") + "); sigue volviendo en "
                + inFlight + " de los " + seated + " pasos de 4 ms con la llave encajada.");
            MechanicTestKit.StepToIdle(boot, 6000);
            MechanicTestKit.AssertViewMatchesModel(boot);
        }

        [UnityTest]
        public IEnumerator Hoja43_UndoRestoresLeaf()
        {
            yield return MechanicTestKit.LoadPlaying(43);
            GameBootstrap boot = GameBootstrap.Current!;
            int hoja = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Hoja);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.HojaOpenedMask & (1 << hoja)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            CuiaView view = boot.View!.Views[hoja];
            HojaOverlay leaf = HojaOf(view);
            string closed = Signature(view.OverlayHost.BodyFront);
            Pour opening = path[path.Length - 1];

            // Marea con la hoja ya abierta: Show la vuelve a pintar cerrada, tal cual.
            MechanicTestKit.TapPour(boot, opening, out _);
            MechanicTestKit.StepToIdle(boot, 6000);
            Assert.That(leaf.Visible, Is.False);
            Assert.That(boot.PressUndo(), Is.True, "Marea");
            MechanicTestKit.StepToIdle(boot, 4000);
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(boot.Flow!.Session.Current[hoja].Mechanic, Is.EqualTo(MechanicType.Hoja), "Marea devuelve la Hoja (02 §9.2)");
            AssertClosed(leaf);
            Assert.That(Signature(view.OverlayHost.BodyFront), Is.EqualTo(closed), "la misma hoja cerrada que antes del vertido");

            // Marea a mitad del paso (02 §14): SnapAll cierra el paso (SnapStep) y la Marea devuelve la hoja cerrada.
            MechanicTestKit.TapPour(boot, opening, out _);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Open, hoja, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, hoja, 0.5f);
            Assert.That(leaf.IsAnimating && leaf.FallingLeafVisible, Is.True, "a mitad de la caída");
            Assert.That(boot.PressUndo(), Is.True, "Marea a mitad del paso");
            Assert.That(leaf.IsAnimating, Is.False, "SnapAll terminó el paso con SnapStep");
            Assert.That(boot.View!.Animator.Steps.ActiveCount, Is.EqualTo(0));
            MechanicTestKit.StepToIdle(boot, 4000);
            MechanicTestKit.AssertViewMatchesModel(boot);
            AssertClosed(leaf);
            Assert.That(Signature(view.OverlayHost.BodyFront), Is.EqualTo(closed), "sin restos de la caída (giro, escala ni fundido)");
        }

        [UnityTest]
        public IEnumerator HojaOpen_SnapAllMidStep_EndsInAfter()
        {
            // Camino de 02 §14 con un overlay que anima de verdad (NOTAS de la ola 2, para T11): SnapAll al 50 % del Open.
            yield return null;
            BoardState before = Board(
                new[] { new int[0], new[] { 3, 4, 4 }, new[] { 4, 4 }, new[] { 3, 3, 3 } },
                new[] { new MechanicSpec(MechanicType.Hoja, key: Aguaje), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, before);
            HojaOverlay leaf = HojaOf(_rig.Views[0]);
            BoardState after = _rig.Pour(before, 1, 0, out _);
            RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.Open, 0), 3000);
            Assert.That(_rig.Animator.Steps.TryGetRunning(MechanicStepKind.Open, 0, out _, out float duration), Is.True);
            Assert.That(duration, Is.EqualTo(HojaOverlay.OpenStepMs * _rig.Animator.StepSpeedBase).Within(0.05f));
            RunUntil(_rig, () => leaf.StepProgress >= 0.5f, 1000);
            Assert.That(leaf.IsAnimating && leaf.FallingLeafVisible, Is.True, "a mitad de la caída");

            _rig.Animator.SnapAll();
            Assert.That(leaf.IsAnimating, Is.False, "SnapAll llamó a SnapStep");
            Assert.That(leaf.StepProgress, Is.EqualTo(-1f));
            Assert.That(_rig.Animator.Steps.ActiveCount, Is.EqualTo(0));
            Assert.That(_rig.Animator.IsIdle, Is.True);
            for (int i = 0; i < after.Count; i++)
            {
                Assert.That(_rig.Animator.ShownOf(i), Is.EqualTo(after[i]), "cuia " + i + ": ShownOf == After");
                Assert.That(_rig.Views[i].ShownCuia, Is.EqualTo(after[i]), "cuia " + i + ": la vista pinta After");
            }

            Assert.That(leaf.Visible || leaf.ClosedLeafVisible || leaf.FallingLeafVisible || leaf.FruitVisible, Is.False, "abierta: nada de la hoja");
            MechanicStepRecord open = MechanicTestKit.Record(_rig.Animator, MechanicStepKind.Open, 0);
            Assert.That(open.Snapped, Is.True, "el registro anota el cierre anticipado");
        }

        [UnityTest]
        public IEnumerator HojaOpen_KeyPopsLate_UnderTheRowAbove()
        {
            // La llave salta en la ventana en que el origen ya dejó libre la boca (D-006; Hoja43_KeySeatsWithTheMouthClear lo
            // mide con la pose del origen) y, en todo el paso, el medallón no pasa de KeyTopMaxU sobre lo alto de la cuia: no
            // invade el marco ni la repisa de la fila de arriba en un tablero de 3 filas.
            yield return null;
            BoardState before = Board(
                new[] { new int[0], new[] { 3, 4, 4 }, new[] { 4, 4 }, new[] { 3, 3, 3 } },
                new[] { new MechanicSpec(MechanicType.Hoja, key: Aguaje), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, before);
            HojaOverlay leaf = HojaOf(_rig.Views[0]);
            float restTop = leaf.KeyTopAboveLipU;
            Assert.That(restTop, Is.EqualTo(0.70f).Within(0.01f), "en reposo, el medallón llega a 0,70 u sobre la boca");
            _rig.Pour(before, 1, 0, out _);
            RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.Open, 0), 3000);

            float maxTop = 0f;
            bool held = false;
            int frames = 0;
            while (leaf.IsAnimating && frames < 200)
            {
                float t = leaf.StepProgress;
                if (leaf.FruitVisible)
                {
                    maxTop = Mathf.Max(maxTop, leaf.KeyTopAboveLipU);
                }

                if (t < HojaOverlay.FruitPopStart)
                {
                    Assert.That(leaf.FruitScale, Is.EqualTo(1f).Within(1e-4f), "t = " + t + ": sin saltar mientras el origen tapa la boca");
                }
                else if (t >= HojaOverlay.FruitPopEnd && t < HojaOverlay.FruitHoldEnd)
                {
                    held = true;
                    Assert.That(leaf.FruitScale, Is.EqualTo(HojaOverlay.FruitPopScale).Within(0.01f), "t = " + t + ": la llave encajada, a 1,3×");
                    Assert.That(leaf.FruitAlpha, Is.GreaterThan(0.99f), "t = " + t + ": y opaca");
                    Assert.That(leaf.FruitGlowAlpha, Is.EqualTo(0f), "t = " + t + ": todavía sin luz");
                }

                _rig.Tick(PlayModeHelpers.FrameMs);
                frames++;
            }

            Assert.That(held, Is.True, "algún fotograma con la llave encajada y quieta");
            Assert.That(maxTop, Is.GreaterThan(restTop), "sube con la hoja");
            Assert.That(maxTop, Is.LessThanOrEqualTo(HojaOverlay.KeyTopMaxU + 1e-4f), "y el medallón no pasa del tope en el salto");
            TestContext.WriteLine("Borde de arriba del medallón: " + restTop.ToString("F3") + " u en reposo y " + maxTop.ToString("F3") + " u como mucho.");
        }

        [UnityTest]
        public IEnumerator HojaOpen_EveryKeyStaysUnderTheCap()
        {
            // Con cualquiera de las 14 llaves, en reposo y en todo el paso, ni el medallón ni la fruta girada pasan de KeyTopMaxU
            // sobre lo alto de la cuia. Las frutas que más suben con el giro son el lulo (el cáliz llega al borde de su celda; es
            // la llave del 198, con la Hoja en la fila 1 de un tablero de 3 filas) y la hoja del Río Negro (la del 92, en la fila
            // 2): con ellas la llave entera baja hasta el tope.
            yield return null;
            var line = new StringBuilder("Lo más alto de la llave sobre la boca (u, en reposo / en el paso):");
            for (int key = 1; key <= 14; key++)
            {
                int other = key == 3 ? 4 : 3;
                BoardState before = Board(
                    new[] { new int[0], new[] { other, key, key }, new[] { key, key }, new[] { other, other, other } },
                    new[] { new MechanicSpec(MechanicType.Hoja, key: key), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
                _rig = new MechanicTestKit.Rig(Art, before);
                HojaOverlay leaf = HojaOf(_rig.Views[0]);
                Assert.That(leaf.ShownKey, Is.EqualTo(key));
                float rest = Mathf.Max(leaf.KeyTopAboveLipU, leaf.FruitTopAboveLipU);
                float max = rest;
                _rig.Pour(before, 1, 0, out _);
                RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.Open, 0), 3000);
                for (int frames = 0; leaf.IsAnimating && frames < 200; frames++)
                {
                    if (leaf.FruitVisible)
                    {
                        max = Mathf.Max(max, Mathf.Max(leaf.KeyTopAboveLipU, leaf.FruitTopAboveLipU));
                    }

                    _rig.Tick(PlayModeHelpers.FrameMs);
                }

                Assert.That(max, Is.LessThanOrEqualTo(HojaOverlay.KeyTopMaxU + 1e-4f), "llave " + key + ": no pasa del tope");
                if (key == Lulo || key == RioNegro)
                {
                    Assert.That(max, Is.EqualTo(HojaOverlay.KeyTopMaxU).Within(1e-3f), "llave " + key + ": llega al tope, la llave baja por la fruta");
                }

                line.Append(' ').Append(key).Append(": ").Append(rest.ToString("F2")).Append(" / ").Append(max.ToString("F2")).Append(';');
                _rig.Dispose();
                _rig = null;
                yield return null;
            }

            TestContext.WriteLine(line.ToString());
        }

        [UnityTest]
        public IEnumerator Brote163_ColorFromTop()
        {
            yield return MechanicTestKit.LoadPlaying(163);
            GameBootstrap boot = GameBootstrap.Current!;
            int brote = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Brote);
            Cuia cuia = boot.Flow!.Session.Current[brote];
            Assert.That(cuia.TopColor, Is.EqualTo(CamuCamu), "el 163 estrena el Brote con camu camu en la cima");
            BroteOverlay bud = BroteOf(boot.View!.Views[brote]);
            Assert.That(bud.Visible && bud.CorkVisible && bud.LeavesVisible, Is.True, "tapón-brote con sus dos hojitas");
            Assert.That(bud.BudColor, Is.EqualTo(Palette.Tint(CamuCamu)), "el tapón pintado (su Image), del color de la cima (02 §9.1 M2)");
            Assert.That(bud.LeafOpenDeg, Is.EqualTo(BroteOverlay.ClosedLeafDeg).Within(0.01f), "hojitas plegadas detrás del tapón");
            Assert.That(bud.IsAnimating, Is.False);
        }

        [UnityTest]
        public IEnumerator Brote163_CoversTheMouth_StandsOut_UnderTheRowAbove()
        {
            // Prueba del dueño (2026-10-09; tanda B2): el brote cubre al menos el ancho de la boca (≥ 1,1 u) y sobresale ≥ 1 u
            // de lo alto de la cuia (como lo midió la auditoría) con verde a la vista, se distingue de la tapa de sellado y de la
            // Hoja y no tapa la fila de arriba (bajo su pie, como la nube de la Lluvia), ni en reposo ni en todo el paso Open. En el
            // 163 real y en el peor caso: la cuia más ancha con dos filas (48 dp, u = 24).
            yield return MechanicTestKit.LoadPlaying(163);
            GameBootstrap boot = GameBootstrap.Current!;
            int brote = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Brote);
            CuiaView view = boot.View!.Views[brote];
            BroteOverlay bud = BroteOf(view);
            AssertBroteSizeAndLook(bud, view, boot.Flow!.Session.Current[brote], "nivel 163");
            AssertUnderTheRowAbove(boot.View.Animator, view, brote, bud.VisibleTopU, "nivel 163 en reposo");

            Pour[] path = Trigger(boot, (ev, b, a) => (ev.BroteOpenedMask & (1 << brote)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            MechanicTestKit.TapPour(boot, path[path.Length - 1], out _);
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Open, brote, 4000);
            float maxTop = float.NegativeInfinity;
            int frames = 0;
            while (bud.IsAnimating && frames++ < 200)
            {
                maxTop = Mathf.Max(maxTop, bud.VisibleTopU);
                PlayModeHelpers.TickExact(boot, 4);
            }

            Assert.That(maxTop, Is.LessThanOrEqualTo(view.HeightU + BroteOverlay.TopAboveRimU + 1e-3f), "en el paso Open nada sube más que el capullo en reposo");
            AssertUnderTheRowAbove(boot.View.Animator, view, brote, maxTop, "nivel 163 en el paso Open");
            MechanicTestKit.StepToIdle(boot, 6000);

            // Peor caso: 6 cuias en dos filas de 3 a 48 dp (BoardLayout, 360 dp), con el Brote bajo la fila de arriba.
            BoardState before = Board(
                new[] { new[] { 1, 1 }, new[] { 2, 2 }, new[] { 3, 3 }, new[] { CamuCamu, CamuCamu }, new[] { 3, CamuCamu }, new int[0] },
                new[] { MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Brote), MechanicSpec.None });
            _rig = new MechanicTestKit.Rig(Art, before);
            BoardLayoutResult l = BoardLayout.Compute(6, CuiaGeometry.HeightU(4), 360f, 150f, 690f);
            for (int i = 0; i < 6; i++)
            {
                _rig.Views[i].SetGeometry(new Vector2(l.CenterX[i], -l.FootY[i]), l.CuiaWidth, l.SlotWidth);
            }

            _rig.Animator.BoardLaidOut();
            Assert.That(l.Rows, Is.EqualTo(2));
            Assert.That(l.RowOf[4], Is.EqualTo(1), "el Brote, en la fila de abajo");
            Assert.That(l.CuiaWidth, Is.EqualTo(BoardLayout.MaxCuiaWidthFrom6).Within(0.01f), "la cuia más ancha con dos filas");
            CuiaView rigView = _rig.Views[4];
            BroteOverlay rigBud = BroteOf(rigView);
            AssertBroteSizeAndLook(rigBud, rigView, before[4], "48 dp");
            AssertUnderTheRowAbove(_rig.Animator, rigView, 4, rigBud.VisibleTopU, "48 dp en reposo");
            _rig.Pour(before, 3, 4, out PourVisual v);
            Assert.That(v.Events.BroteOpenedMask, Is.EqualTo(1 << 4));
            RunUntil(_rig, () => _rig.Animator.Steps.IsRunning(MechanicStepKind.Open, 4), 3000);
            float rigTop = float.NegativeInfinity;
            for (int f = 0; rigBud.IsAnimating && f < 200; f++)
            {
                rigTop = Mathf.Max(rigTop, rigBud.VisibleTopU);
                _rig.Tick(4f);
            }

            Assert.That(rigTop, Is.LessThanOrEqualTo(rigView.HeightU + BroteOverlay.TopAboveRimU + 1e-3f), "48 dp: en el paso Open nada sube más que el capullo en reposo");
            AssertUnderTheRowAbove(_rig.Animator, rigView, 4, rigTop, "48 dp en el paso Open");
            TestContext.WriteLine("Brote: cáliz de " + rigBud.SepalsRectU.width.ToString("F2") + " u, " + BroteOverlay.TopAboveRimU.ToString("F2") + " u sobre lo alto de la cuia ("
                + (view.HeightU + BroteOverlay.TopAboveRimU - CuiaGeometry.LipU(4)).ToString("F2") + " u sobre el labio); lo más alto en el paso, " + (rigTop - rigView.HeightU).ToString("F3")
                + " u sobre la cuia (" + ((rigTop - rigView.HeightU) * rigView.Unit).ToString("F1") + " dp a 48 dp).");
        }

        [UnityTest]
        public IEnumerator Brote163_OpenStepDuration()
        {
            yield return MechanicTestKit.LoadPlaying(163);
            GameBootstrap boot = GameBootstrap.Current!;
            int brote = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Brote);
            Pour[] path = Trigger(boot, (ev, b, a) => (ev.BroteOpenedMask & (1 << brote)) != 0);
            MechanicTestKit.PlayPrefix(boot, path);
            BroteOverlay bud = BroteOf(boot.View!.Views[brote]);
            int discarded = boot.View.Fx.DiscardedCount;
            PourVisual visual = MechanicTestKit.TapPour(boot, path[path.Length - 1], out _);
            float tapped = boot.View.Animator.Clock;
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Open, brote, 4000);
            Assert.That(bud.IsAnimating, Is.True, "el paso Open llegó al overlay del Brote");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, brote, 0.5f);
            Assert.That(bud.LeafOpenDeg, Is.GreaterThan(BroteOverlay.ClosedLeafDeg + 10f), "a mitad, las dos hojitas ya se abren hacia fuera");
            Assert.That(bud.CorkAlpha, Is.LessThan(0.8f), "y el tapón se disuelve");
            Assert.That(bud.LeavesVisible, Is.True);

            // El brote abierto en V se queda opaco hasta HoldEnd (para que se vea cuando aterriza el origen) y después se va.
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, brote, 0.7f);
            Assert.That(bud.LeafAlpha, Is.GreaterThan(0.99f), "abierto y opaco");
            Assert.That(bud.LeafOpenDeg, Is.GreaterThanOrEqualTo(BroteOverlay.OpenLeafDeg - 0.5f), "en V");
            Assert.That(bud.CorkVisible, Is.False, "el tapón ya se disolvió");
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Open, brote, 0.9f);
            Assert.That(bud.LeafAlpha, Is.InRange(0.01f, 0.9f), "se desvanece en el último cuarto");

            MechanicTestKit.StepToIdle(boot, 6000);
            float elapsed = boot.View.Animator.Clock - tapped;
            MechanicTestKit.AssertLogFollowsPlan(boot.View.Animator, in visual);
            MechanicStepRecord open = MechanicTestKit.Record(boot.View.Animator, MechanicStepKind.Open, brote);
            Assert.That(open.Stage, Is.EqualTo(StepStage.B));
            Assert.That(open.Owner, Is.EqualTo(MechanicType.Brote), "lo ejecuta el overlay del Brote");
            Assert.That(open.UsedDefault || open.Snapped, Is.False);
            Assert.That(open.EndMs - open.StartMs, Is.EqualTo(BroteOverlay.OpenStepMs * open.SpeedFactor).Within(0.05f), "350 ms × factor (02 §4.1)");
            Assert.That(open.SpeedFactor, Is.EqualTo(boot.View.Animator.StepSpeedBase).Within(1e-4f));
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta");
            Assert.That(boot.View.Fx.DiscardedCount, Is.EqualTo(discarded), "destellos sin descartes por los topes del pool");
            TestContext.WriteLine("Nivel 163: " + elapsed + " ms del toque a la vista quieta. " + MechanicTestKit.Describe(boot.View.Animator));
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(boot.Flow!.Session.Current[brote].Mechanic, Is.EqualTo(MechanicType.None), "germinado: cuia normal");
            Assert.That(bud.Visible || bud.CorkVisible || bud.LeavesVisible, Is.False, "al terminar, oculto");
        }

        [UnityTest]
        public IEnumerator NoAllocDuringSteps()
        {
            yield return null;

            // Calentamiento (JIT y primeras llamadas) y después la medida en un banco nuevo: la Hoja y el Brote se abren a la
            // vez, 60 fotogramas sin asignaciones (Recorder GC.Alloc) desde los toques y otros 60, con el pool ≤ 44.
            for (int pass = 0; pass < 2; pass++)
            {
                BoardState before = Board(
                    new[] { new int[0], new[] { 3, 4, 4 }, new[] { 3, 6 }, new[] { 6, 6, 6 }, new[] { 4, 4 }, new[] { 3, 3 } },
                    new[] { new MechanicSpec(MechanicType.Hoja, key: Aguaje), MechanicSpec.None, new MechanicSpec(MechanicType.Brote), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None });
                _rig?.Dispose();
                _rig = new MechanicTestKit.Rig(Art, before);
                Assert.That(_rig.Views[0].Overlay, Is.InstanceOf<HojaOverlay>());
                Assert.That(_rig.Views[2].Overlay, Is.InstanceOf<BroteOverlay>());
                yield return null; // el banco recién creado se dibuja una vez antes de los toques
                BoardState s = _rig.Pour(before, 1, 0, out PourVisual hoja);
                _rig.Pour(s, 3, 2, out PourVisual brote);
                Assert.That(hoja.Events.HojaOpenedMask, Is.EqualTo(1 << 0));
                Assert.That(brote.Events.BroteOpenedMask, Is.EqualTo(1 << 2));

                MechanicTestKit.Rig rig = _rig;
                int maxParticles = 0;
                int frames = 0;
                int discardedBefore = rig.Fx.DiscardedCount;
                void Run60()
                {
                    for (int f = 0; f < 60; f++)
                    {
                        rig.Tick(PlayModeHelpers.FrameMs);
                        frames++;
                        if (rig.Fx.ActiveCount > maxParticles)
                        {
                            maxParticles = rig.Fx.ActiveCount;
                        }
                    }
                }

                if (pass == 1)
                {
                    Assert.That(Run60, Is.Not.AllocatingGCMemory(), "60 fotogramas desde los toques");
                    Assert.That(Run60, Is.Not.AllocatingGCMemory(), "60 fotogramas más");
                    Assert.That(frames, Is.EqualTo(120));
                    Assert.That(maxParticles, Is.GreaterThan(0), "destellos y salpicadura del pool común");

                    // Garantía estructural, no una medida: el pool de FxLayer (T19) descarta por construcción lo que pase de 44,
                    // así que esta aserción no puede fallar. Lo que mide algo es que los efectos no se pierdan por los topes, y eso
                    // se comprueba en los estrenos reales (Hoja43_OpenStepDuration y Brote163_OpenStepDuration), con un solo
                    // desbloqueo. Aquí, con los dos desbloqueos y los dos vertidos a la vez, el tope de 8 brillos aditivos
                    // (destellos y aros) puede dejar fuera alguno: es lo que debe hacer el pool, y se anota.
                    Assert.That(maxParticles, Is.LessThanOrEqualTo(FxLayer.MaxParticles), "garantía estructural del pool");
                    TestContext.WriteLine("Hoja y Brote a la vez: " + maxParticles + " partículas como mucho; descartadas por los topes: " + (rig.Fx.DiscardedCount - discardedBefore));
                }

                rig.RunUntilIdle(4000);
                Assert.That(rig.Animator.IsIdle, Is.True);
                MechanicTestKit.Record(rig.Animator, MechanicStepKind.Open, 0);
                MechanicTestKit.Record(rig.Animator, MechanicStepKind.Open, 2);

                // Un fotograma de verdad entre las dos pasadas: UGUI reconstruye y vacía sus colas y el banco viejo se destruye,
                // así la medida no cuenta el crecimiento de las colas de UGUI con los gráficos de los dos bancos a la vez.
                _rig.Dispose();
                _rig = null;
                yield return null;
            }
        }

        // El Brote cerrado de la cuia cuia: cubre la boca, sobresale ≥ 1 u de lo alto de la cuia con verde a la vista, lleva el
        // color de la cima con un capullo alto y en punta (no la cúpula baja de la tapa ni la hoja apaisada de la Hoja) y sin tapa.
        private static void AssertBroteSizeAndLook(BroteOverlay bud, CuiaView view, Cuia cuia, string what)
        {
            Assert.That(bud.Visible && bud.CorkVisible && bud.SepalsVisible && bud.LeavesVisible, Is.True, what + ": capullo, cáliz y hojitas a la vista");
            Rect sepals = bud.SepalsRectU;
            Rect cork = bud.CorkRectU;
            float lip = CuiaGeometry.LipU(cuia.Capacity);
            Assert.That(sepals.width, Is.GreaterThanOrEqualTo(2f * CuiaGeometry.RimHalfWidthU), what + ": el cáliz cubre la boca entera (≥ 1,1 u)");
            Assert.That(Mathf.Abs(sepals.center.x), Is.LessThan(0.01f), what + ": centrado en la boca");
            Assert.That(sepals.yMin, Is.LessThanOrEqualTo(lip + 0.01f), what + ": baja hasta el labio de vertido");
            Assert.That(sepals.yMax - view.HeightU, Is.GreaterThanOrEqualTo(0.3f), what + ": el verde del cáliz asoma sobre la boca");

            // «Sobresalir ≥ 1 u» (hoja del dueño, 2026-10-09) medido como la auditoría de la tanda B2: desde lo alto de la cuia
            // (HeightU), no desde el labio de vertido (HeightU − 0,25). El brote viejo sobresalía 0,8 u y el de 985455a, 0,9 u: los
            // dos fallan aquí.
            Assert.That(bud.VisibleTopU - view.HeightU, Is.GreaterThanOrEqualTo(1f - 1e-3f), what + ": sobresale ≥ 1 u de lo alto de la cuia");
            Assert.That(bud.VisibleTopU, Is.EqualTo(view.HeightU + BroteOverlay.TopAboveRimU).Within(0.01f), what + ": la punta del capullo es lo más alto");
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 tip = bud.LeafTip(side);
                Assert.That(tip.x * side, Is.GreaterThan(0.5f * cork.width), what + ": la hojita " + side + " asoma por fuera del capullo");
                Assert.That(tip.y, Is.GreaterThan(view.HeightU + 0.5f), what + ": y por encima de la boca");
            }

            // Distinto de la tapa (cúpula de 1,10 × 0,35 u en madera de calabaza) y de la Hoja (hoja apaisada de 2,3 × 1 u).
            Assert.That(cork.height / cork.width, Is.GreaterThanOrEqualTo(1f), what + ": capullo alto y en punta");
            Assert.That(cork.height / cork.width, Is.GreaterThan(2f * (0.35f / 1.10f)).And.GreaterThan(2f * (1f / 2.3f)), what + ": ni la tapa ni la hoja de la Hoja");
            Assert.That(bud.BudColor, Is.EqualTo(Palette.Tint(cuia.TopColor)), what + ": del color de la cima");
            Assert.That(ColorMath.DeltaE00(bud.BudColor, Palette.CapWood), Is.GreaterThanOrEqualTo(10f), what + ": no se confunde con la madera de la tapa");
            Assert.That(ColorMath.DeltaE00(ProceduralArt.BroteLeafLight, Palette.CapWood), Is.GreaterThanOrEqualTo(10f));
            Assert.That(view.Sealed, Is.False, what + ": sin tapa");
        }

        // Lo más alto del Brote (topU, u desde el pie de la cuia index) no tapa la fila de arriba (B2.md, ola3.Brote.forma): queda
        // bajo su pie, con el mismo límite que la nube de la Lluvia al irse (como mucho RiseTopMaxDp sobre la boca, 1 dp bajo el
        // pie), y en la fila de arriba, bajo el canto interior del marco (CloudTopMaxDp, ≈ 24 dp sobre la boca). En el peor caso
        // (48 dp) roza la madera de la repisa de la fila de arriba (17 dp bajo su pie) como mucho 1 dp [P, pendiente del dueño].
        private static void AssertUnderTheRowAbove(BoardAnimator board, CuiaView view, int index, float topU, string what)
        {
            float u = view.Unit;
            Vector2 foot = board.Foot(index);
            float top = foot.y + (topU * u);
            float rim = foot.y + (view.HeightU * u);
            float above = float.PositiveInfinity;
            int n = ((ICuiaBoard)board).Count;
            for (int c = 0; c < n; c++)
            {
                float f = board.Foot(c).y;
                if (f > rim + 1f)
                {
                    above = Mathf.Min(above, f);
                }
            }

            if (float.IsPositiveInfinity(above))
            {
                Assert.That(top - rim, Is.LessThanOrEqualTo(LluviaOverlay.CloudTopMaxDp + 0.01f), what + ": bajo el marco del panel");
                return;
            }

            float shelfBottom = above - RemolinoOverlay.ShelfBottomBelowFootDp;
            Assert.That(top - rim, Is.LessThanOrEqualTo(LluviaOverlay.RiseTopMaxDp), what + ": como mucho RiseTopMaxDp sobre la boca, como la nube de la Lluvia");
            Assert.That(top, Is.LessThanOrEqualTo(above - 1f), what + ": bajo el pie de la fila de arriba (" + (above - top).ToString("F1") + " dp de aire)");
            Assert.That(top - shelfBottom, Is.LessThanOrEqualTo(1f + 0.01f), what + ": roza la madera de la repisa de arriba como mucho 1 dp [P] (" + (top - shelfBottom).ToString("F1") + " dp)");
        }

        private static void AssertClosed(HojaOverlay leaf)
        {
            Assert.That(leaf.Visible && leaf.ClosedLeafVisible, Is.True, "la hoja vuelve a la boca");
            Assert.That(leaf.FruitVisible, Is.True, "con su fruta llave");
            Assert.That(leaf.ShownKey, Is.EqualTo(Aguaje));
            Assert.That(leaf.FallingLeafVisible || leaf.IsAnimating, Is.False);
        }

        private static HojaOverlay HojaOf(CuiaView view)
        {
            Assert.That(view.Overlay, Is.InstanceOf<HojaOverlay>(), "la fábrica crea el overlay de la Hoja");
            return (HojaOverlay)view.Overlay!;
        }

        private static BroteOverlay BroteOf(CuiaView view)
        {
            Assert.That(view.Overlay, Is.InstanceOf<BroteOverlay>(), "la fábrica crea el overlay del Brote");
            return (BroteOverlay)view.Overlay!;
        }

        // Vertidos del nivel cargado hasta uno que cumple wanted (el último del camino).
        private static Pour[] Trigger(GameBootstrap boot, MechanicTestKit.PourWanted wanted)
        {
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, wanted);
            Assert.That(path, Is.Not.Null, "vertidos que disparan el evento en el nivel " + boot.Flow!.LevelNumber);
            return path!;
        }

        // ¿La silueta del origen, en su pose de ahora, tapa algún punto del medallón de la llave (rejilla de 5 × 5 puntos)? El
        // medallón crece la mitad que la fruta y su borde de arriba es KeyTopAboveLipU; la silueta, la de CuiaGeometry (cuerpo,
        // hombro, cuello y borde de fibra, con 0,1 u más arriba para el cordón) en el espacio del contenedor del tablero.
        private static bool SourceCoversKey(CuiaView source, CuiaView hoja, HojaOverlay leaf)
        {
            float badgeScale = 1f + (0.5f * (leaf.FruitScale - 1f));
            float halfW = 0.5f * ProceduralArt.HojaBadgeWidthU * badgeScale;
            float top = leaf.KeyTopAboveLipU;
            float bottom = top - (ProceduralArt.HojaBadgeHeightU * badgeScale);
            float u = hoja.Unit;
            Vector2 lip = hoja.FootPosition + new Vector2(0f, hoja.HeightU * u);
            Vector2 origin = source.FootPosition + source.BodyOffset;
            float back = -source.BodyAngle * Mathf.Deg2Rad;
            for (int ix = 0; ix <= 4; ix++)
            {
                for (int iy = 0; iy <= 4; iy++)
                {
                    var p = new Vector2(Mathf.Lerp(-halfW, halfW, ix / 4f), Mathf.Lerp(bottom, top, iy / 4f));
                    Vector2 local = CuiaView.Rotate(lip + (p * u) - origin, back) / source.Unit;
                    float half = SilhouetteHalfWidthU(local.y, source.HeightU);
                    if (half >= 0f && Mathf.Abs(local.x) <= half)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Media anchura de la silueta de una cuia a la altura y (u desde el pie; CuiaGeometry), o −1 fuera de ella.
        private static float SilhouetteHalfWidthU(float y, float heightU)
        {
            if (y < 0f || y > heightU + 0.1f)
            {
                return -1f;
            }

            float rimBottom = heightU - CuiaGeometry.RimU;
            float neckBottom = rimBottom - CuiaGeometry.NeckU;
            float shoulderBottom = neckBottom - CuiaGeometry.ShoulderU;
            if (y >= rimBottom)
            {
                return CuiaGeometry.RimHalfWidthU;
            }

            if (y >= neckBottom)
            {
                return CuiaGeometry.NeckOuterHalfWidthU;
            }

            return y >= shoulderBottom
                ? Mathf.Lerp(CuiaGeometry.ShoulderOuterHalfWidthU, CuiaGeometry.NeckOuterHalfWidthU, (y - shoulderBottom) / CuiaGeometry.ShoulderU)
                : CuiaGeometry.BodyOuterHalfWidthU;
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

        // Estado visible de las piezas bajo un ancla: activación, pose relativa al ancla, Image.enabled (la hoja que cae y su
        // luz se muestran y se ocultan así; como en CuiaOverlayTests), color y sprite de cada una.
        private static string Signature(Transform anchor)
        {
            var sb = new StringBuilder(512);
            Transform[] nodes = anchor.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                Transform node = nodes[i];
                sb.Append(node.name).Append(node.gameObject.activeSelf ? '+' : '-');
                if (node is RectTransform rt)
                {
                    sb.Append(rt.anchoredPosition.ToString("F3")).Append(rt.sizeDelta.ToString("F3"));
                }

                sb.Append(node.localScale.ToString("F3")).Append(node.localEulerAngles.ToString("F3"));
                var image = node.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    sb.Append(image.enabled ? 'E' : 'D').Append(image.color.ToString("F3")).Append(image.sprite != null ? image.sprite.name : "-");
                }

                sb.Append('|');
            }

            return sb.ToString();
        }
    }
}
