#nullable enable
using System.Collections;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Capa de vertido sin techo (D-006, punto 1; Magic Sort en t = 250: el origen tapa el rótulo del nivel): mientras
    /// vierte, el cuerpo del origen y su chorro se dibujan en <see cref="GameScreen.PourFront"/>, hermana posterior al HUD
    /// y al ambiente cercano, sin que la ranura (con su área táctil) cambie de padre; al terminar, y tras
    /// <see cref="BoardAnimator.SnapAll"/>, el cuerpo vuelve a su ranura con su índice y la capa queda vacía. En todos los
    /// fotogramas, también en los del cambio de capa, el cuerpo está donde dicen su pie y su desplazamiento (≤ 0,5 dp). El
    /// reloj lo lleva el test, en el lienzo del teléfono (1080 × 2340 px, 360 × 780 dp).
    /// </summary>
    public sealed class PourLayerTests
    {
        private const float ToleranceDp = 0.5f;

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Pouring_DrawsTheSourceAndTheStreamAboveTheHud_AndRestoresThemAfter()
        {
            // Nivel 1: vertido 0 → 2 (una fila). Nivel 7: vertido 5 → 1, de la fila 2 a la fila 1, el que más sube.
            yield return PourGoesThroughTheFrontLayer(1, 0, 2);
            yield return PourGoesThroughTheFrontLayer(7, 5, 1);
        }

        private static IEnumerator PourGoesThroughTheFrontLayer(int level, int source, int target)
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp, como VisualSnapshotTests
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 2000);
            string label = "nivel " + level + ", " + source + " → " + target;

            LevelSession session = boot.Flow!.Session;
            Pour[]? setup = PlayModeHelpers.FindSetup(session.Current, source, target, session.PourMode, session.FutileRuleEnabled);
            Assert.That(setup, Is.Not.Null, label + ": ningún estado cercano hace legal el vertido");
            for (int i = 0; i < setup!.Length; i++)
            {
                TapPour(boot, setup[i].Source, setup[i].Target, label + " (vertido previo " + i + ")");
                PlayModeHelpers.StepUntilIdle(boot, 3000);
            }

            GameScreen view = boot.View!;
            RectTransform front = view.PourFront;
            CuiaView s = view.Views[source];
            RectTransform body = s.Body;
            Transform slot = s.Root;
            Transform slotParent = slot.parent;
            Transform bodyParent = body.parent;
            int bodyIndex = body.GetSiblingIndex();

            // Orden de capas: la de vertido va detrás del HUD y del ambiente cercano en la jerarquía, así que se dibuja
            // delante de ellos; no es una superficie táctil.
            Assert.That(front.parent, Is.SameAs(view.Hud.parent), label + ": la capa de vertido es hermana del HUD");
            Assert.That(front.GetSiblingIndex(), Is.GreaterThan(view.Hud.GetSiblingIndex()), label + ": delante del HUD");
            Assert.That(front.GetSiblingIndex(), Is.GreaterThan(view.AmbientNear.GetSiblingIndex()), label + ": delante del ambiente cercano");
            Assert.That(front.GetComponent<Graphic>(), Is.Null, label + ": la capa no tiene Graphic");
            Assert.That(front.childCount, Is.EqualTo(0), label + ": la capa está vacía en reposo");
            Assert.That(bodyParent, Is.SameAs(slot), label + ": en reposo el cuerpo está en su ranura");

            TapPour(boot, source, target, label);
            bool sawFront = false;
            bool sawStream = false;
            int elapsed = 0;
            while (!view.IsViewIdle)
            {
                Assert.That(elapsed, Is.LessThan(4000), label + ": la cola visual no se vació");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                elapsed += PlayModeHelpers.FrameMs;
                Assert.That(slot.parent, Is.SameAs(slotParent), label + ": la ranura (HitArea) no cambia de padre");
                AssertBodyWhereThePoseSays(view, s, label + " a los " + elapsed + " ms");
                if (body.parent == front)
                {
                    sawFront = true;
                    StreamGraphic? stream = EnabledStream();
                    if (stream != null)
                    {
                        sawStream = true;
                        Assert.That(stream.transform.parent, Is.SameAs(front), label + ": el chorro está en la capa de vertido");
                        Assert.That(stream.transform.GetSiblingIndex(), Is.GreaterThan(body.GetSiblingIndex()), label + ": el chorro sobre el origen");
                    }
                }
            }

            Assert.That(sawFront, Is.True, label + ": el origen pasó a la capa de vertido");
            Assert.That(sawStream, Is.True, label + ": el chorro pasó a la capa de vertido");
            AssertRestored(view, s, bodyParent, bodyIndex, label + " al terminar");

            view.Animator.SnapAll();
            AssertRestored(view, s, bodyParent, bodyIndex, label + " tras SnapAll");

            // Un vertido cortado a medias por SnapAll también devuelve el cuerpo y el chorro: Marea (ilimitada en 1-10,
            // 04 §2.1) lo deshace y se vuelve a verter.
            Assert.That(boot.PressUndo(), Is.True, label + ": Marea");
            PlayModeHelpers.StepUntilIdle(boot, 3000);
            TapPour(boot, source, target, label + " (cortado)");
            PlayModeHelpers.Step(boot, (int)view.Timings.TravelMs + PlayModeHelpers.FrameMs);
            Assert.That(body.parent, Is.SameAs(front), label + ": en la capa durante el vertido cortado");
            view.Animator.SnapAll();
            AssertRestored(view, s, bodyParent, bodyIndex, label + " tras SnapAll a mitad");
        }

        private static void AssertRestored(GameScreen view, CuiaView s, Transform bodyParent, int bodyIndex, string label)
        {
            Assert.That(s.Body.parent, Is.SameAs(bodyParent), label + ": el cuerpo vuelve a su ranura");
            Assert.That(s.Body.GetSiblingIndex(), Is.EqualTo(bodyIndex), label + ": con su índice de hermano");
            Assert.That(s.BodyInFront, Is.False, label);
            Assert.That(view.PourFront.childCount, Is.EqualTo(0), label + ": la capa de vertido queda vacía");
            AssertBodyWhereThePoseSays(view, s, label);
        }

        // El pivote del cuerpo (centro del pie) está, en dp del lienzo, en el pie de la ranura más el desplazamiento de la
        // pose, medidos en el contenedor del tablero (origen arriba a la izquierda), y con el ángulo de la pose.
        private static void AssertBodyWhereThePoseSays(GameScreen view, CuiaView s, string label)
        {
            var canvas = (RectTransform)view.Canvas.transform;
            RectTransform board = view.BoardContainer;
            Vector2 p = s.FootPosition + s.BodyOffset;
            Rect r = board.rect;
            Vector3 expectedWorld = board.TransformPoint(new Vector3(r.xMin + p.x, r.yMax + p.y, 0f));
            Vector2 expected = canvas.InverseTransformPoint(expectedWorld);
            Vector2 actual = canvas.InverseTransformPoint(s.Body.position);
            Assert.That(Vector2.Distance(actual, expected), Is.LessThanOrEqualTo(ToleranceDp), label + ": posición del cuerpo (dp)");
            float angle = Quaternion.Angle(s.Body.rotation, canvas.rotation * Quaternion.Euler(0f, 0f, s.BodyAngle));
            Assert.That(angle, Is.LessThan(0.1f), label + ": ángulo del cuerpo");
        }

        private static void TapPour(GameBootstrap boot, int source, int target, string label)
        {
            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected), label + ": selección");
            Assert.That(boot.TapCuia(target).Kind, Is.EqualTo(TapKind.Poured), label + ": vertido");
        }

        private static StreamGraphic? EnabledStream()
        {
            StreamGraphic[] streams = Object.FindObjectsByType<StreamGraphic>(FindObjectsSortMode.None);
            for (int i = 0; i < streams.Length; i++)
            {
                if (streams[i].enabled)
                {
                    return streams[i];
                }
            }

            return null;
        }
    }
}
