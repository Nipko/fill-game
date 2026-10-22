#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// C10, el arrastre, en el juego (02 §3.1; 08 §9 <c>drag_pour_enabled</c>; tarea B11, parche B11-drag): el dedo baja en X y
    /// sube sobre Y tras ≥ 24 dp → X→Y; soltar sobre el fondo cancela; con <c>drag_pour_enabled</c> = false, soltar fuera cancela
    /// como antes. Por los mismos eventos de puntero que un toque (<see cref="CuiaView"/>, el touch-down eleva y el touch-up decide).
    /// </summary>
    public sealed class DragPourHooksTests
    {
        [TearDown]
        public void TearDown()
        {
            GameScreen.DragPourOverride = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator C10_DownOnX_UpOnY_PoursXToY()
        {
            GameBootstrap boot = null!;
            yield return Load(b => boot = b);
            LevelFlow flow = boot.Flow!;
            CuiaView from = boot.View!.Views[0];
            CuiaView to = boot.View.Views[2]; // nivel 1: la cuia 2 está vacía, 0 → 2 es legal
            Press(from, ScreenCenter(from));
            Assert.That(from.IsLifted, Is.True, "sube en el touch-down");
            Release(from, ScreenCenter(to));
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(1), "02 §3.1, C10: X→Y");
            Assert.That(flow.Session.GetAppliedPour(0).Source, Is.EqualTo(0));
            Assert.That(flow.Session.GetAppliedPour(0).Target, Is.EqualTo(2));
            Assert.That(flow.Selected, Is.EqualTo(TapController.NoSelection), "C4: tras el vertido, sin selección");
            Assert.That(boot.View.LastDragSource, Is.EqualTo(0));
            Assert.That(boot.View.LastDragTarget, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator C10_OntoAnIllegalY_ThatCanBeASource_Cancels()
        {
            // Nivel 1: la cuia 1 está llena y la 0 lleva otra fruta arriba: 0 → 1 es ilegal y la 1 puede ser origen. Un toque haría
            // C5 (la selección pasa a la 1); el arrastre no selecciona nada y la 1 se sacude [P] (Q-B11-11).
            GameBootstrap boot = null!;
            yield return Load(b => boot = b);
            LevelFlow flow = boot.Flow!;
            CuiaView from = boot.View!.Views[0];
            Press(from, ScreenCenter(from));
            Release(from, ScreenCenter(boot.View.Views[1]));
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(0));
            Assert.That(flow.Selected, Is.EqualTo(TapController.NoSelection), "ni la 0 ni la 1 quedan seleccionadas");
            Assert.That(boot.View.Views[1].IsLifted, Is.False, "la 1 no llega a subir");
            Assert.That(boot.View.LastDragTarget, Is.EqualTo(1));
            PlayModeHelpers.Step(boot, 400);
            Assert.That(from.IsLifted, Is.False);
            Assert.That(boot.View.Views[1].IsLifted, Is.False);
        }

        [UnityTest]
        public IEnumerator C10_ASelectionMadeWithAnotherFingerMeanwhile_Cancels()
        {
            // C11 (varios dedos): el dedo A baja en la 0 (se eleva); el B selecciona la 1; el A suelta sobre la 2. El arrastre no
            // vierte (sería 1 → 0) y la 1 sigue seleccionada [P].
            GameBootstrap boot = null!;
            yield return Load(b => boot = b);
            LevelFlow flow = boot.Flow!;
            CuiaView from = boot.View!.Views[0];
            Press(from, ScreenCenter(from));
            Assert.That(boot.TapCuia(1).Kind, Is.EqualTo(TapKind.Selected), "el otro dedo");
            Release(from, ScreenCenter(boot.View.Views[2]));
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(0));
            Assert.That(flow.Selected, Is.EqualTo(1));
            Assert.That(boot.View.LastDragSource, Is.EqualTo(-1), "no se tomó como arrastre");
        }

        [UnityTest]
        public IEnumerator C10_ReleaseOverTheBackground_Cancels()
        {
            GameBootstrap boot = null!;
            yield return Load(b => boot = b);
            LevelFlow flow = boot.Flow!;
            CuiaView from = boot.View!.Views[0];
            Vector2 start = ScreenCenter(from);
            Press(from, start);
            Release(from, new Vector2(-80f, -80f));
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(0), "soltar sobre el fondo cancela");
            Assert.That(flow.Selected, Is.EqualTo(TapController.NoSelection));
            Assert.That(from.IsLifted, Is.False, "la cuia vuelve a bajar");
        }

        [UnityTest]
        public IEnumerator C10_DragPourDisabled_ReleasingOnYCancels()
        {
            GameScreen.DragPourOverride = false;
            GameBootstrap boot = null!;
            yield return Load(b => boot = b);
            LevelFlow flow = boot.Flow!;
            CuiaView from = boot.View!.Views[0];
            Press(from, ScreenCenter(from));
            Release(from, ScreenCenter(boot.View.Views[2]));
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(0), "08 §9: drag_pour_enabled = false");
            Assert.That(flow.Selected, Is.EqualTo(TapController.NoSelection));
        }

        [UnityTest]
        public IEnumerator C10_WithASelection_ReleasingElsewhereCancels()
        {
            // [P]: con otra cuia ya seleccionada, el touch-down no eleva X y soltar fuera cancela como antes del arrastre.
            GameBootstrap boot = null!;
            yield return Load(b => boot = b);
            LevelFlow flow = boot.Flow!;
            Assert.That(boot.TapCuia(1).Kind, Is.EqualTo(TapKind.Selected));
            CuiaView from = boot.View!.Views[0];
            Press(from, ScreenCenter(from));
            Release(from, ScreenCenter(boot.View.Views[2]));
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(0));
            Assert.That(flow.Selected, Is.EqualTo(1), "la selección de antes sigue");
        }

        private static IEnumerator Load(System.Action<GameBootstrap> done)
        {
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            boot.TapAnywhere();
            yield return PlayModeHelpers.WaitFor(() => boot.Flow!.Phase == FlowPhase.Playing && boot.View!.IsViewIdle, "nivel 1 jugable");
            done(boot);
        }

        private static void Press(CuiaView view, Vector2 at)
        {
            ExecuteEvents.Execute(view.gameObject, new PointerEventData(EventSystem.current) { position = at }, ExecuteEvents.pointerDownHandler);
        }

        private static void Release(CuiaView view, Vector2 at)
        {
            ExecuteEvents.Execute(view.gameObject, new PointerEventData(EventSystem.current) { position = at }, ExecuteEvents.pointerUpHandler);
        }

        private static Vector2 ScreenCenter(CuiaView view)
        {
            RectTransform body = view.Body;
            Vector3 world = body.TransformPoint(body.rect.center);
            return RectTransformUtility.WorldToScreenPoint(null, world);
        }
    }
}
