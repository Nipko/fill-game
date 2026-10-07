#nullable enable
using System.Collections;
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Core.Rules;
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
    /// D-002 (decisión del dueño del 2026-10-06; 02 §3.1 C1, §4.1; criterio B1 de <c>docs/dev/decisiones.md</c>): al
    /// seleccionar un origen solo él se eleva 16 dp; ninguna otra cuia cambia y no se resalta ni se sugiere ningún destino.
    /// El rechazo de un toque ilegal conserva su sacudida (02 §4.1). El reloj lo lleva el test.
    /// </summary>
    public sealed class SelectionShowsNoTargetsTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level2_SelectingASourceWithTwoLegalTargets_ChangesNoOtherCuia()
        {
            yield return CheckSelection(2);
        }

        [UnityTest]
        public IEnumerator Level7_SelectingASourceWithTwoLegalTargets_ChangesNoOtherCuia()
        {
            yield return CheckSelection(7);
        }

        [UnityTest]
        public IEnumerator IllegalTap_StillShakes_AndHighlightsNothing()
        {
            // C2 (02 §3.1): toque en una cuia vacía sin selección → rechazo con sacudida de ±6 dp en 120 ms (02 §4.1).
            yield return PlayModeHelpers.LoadGame(2, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 200);
            CuiaView empty = boot.View!.Views[3];
            Assert.That(boot.Flow!.Session.Current[3].Count, Is.EqualTo(0));

            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Rejected));
            float maxShake = 0f;
            for (int t = 0; t < 160; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                maxShake = Mathf.Max(maxShake, Mathf.Abs(empty.Body.anchoredPosition.x));
            }

            Assert.That(maxShake, Is.GreaterThan(CuiaView.ShakeDp * 0.5f), "sacudida");
            Assert.That(empty.Body.anchoredPosition.x, Is.EqualTo(0f).Within(0.001f), "vuelve a su sitio");
            AssertNoTargetRing();
        }

        private static IEnumerator CheckSelection(int level)
        {
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            Assert.That(boot.TapAnywhere(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            PlayModeHelpers.Step(boot, 300);

            BoardState state = flow.Session.Current;
            int source = -1;
            int legalTargets = 0;
            for (int a = 0; a < state.Count && source < 0; a++)
            {
                if (!PourRules.CanBeSource(state, a))
                {
                    continue;
                }

                int legal = 0;
                for (int b = 0; b < state.Count; b++)
                {
                    if (b != a && PourRules.IsLegal(state, a, b))
                    {
                        legal++;
                    }
                }

                if (legal >= 2)
                {
                    source = a;
                    legalTargets = legal;
                }
            }

            Assert.That(source, Is.GreaterThanOrEqualTo(0), "el nivel " + level + " tiene un origen con al menos dos destinos legales");
            CuiaView[] views = boot.View!.Views;
            var before = new string[views.Length];
            for (int i = 0; i < views.Length; i++)
            {
                before[i] = Signature(views[i]);
            }

            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.Step(boot, 300); // la elevación (120 ms) ya terminó

            Assert.That(views[source].IsLifted, Is.True, "el origen se eleva");
            Assert.That(views[source].Body.anchoredPosition.y, Is.EqualTo(CuiaView.LiftDp).Within(0.01f), "16 dp (02 §4.1)");
            for (int i = 0; i < views.Length; i++)
            {
                if (i == source)
                {
                    continue;
                }

                Assert.That(views[i].IsLifted, Is.False, "cuia " + i + " sin elevar");
                Assert.That(Signature(views[i]), Is.EqualTo(before[i]), "la cuia " + i + " no cambia al seleccionar la " + source + " (" + legalTargets + " destinos legales)");
            }

            AssertNoTargetRing();
        }

        // No existe ningún aro de destino en la jerarquía, ni activo ni inactivo (B1).
        private static void AssertNoTargetRing()
        {
            Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                Assert.That(all[i].name, Is.Not.EqualTo("TargetRing"), "objeto de resaltado de destino");
            }
        }

        // Estado visible de una cuia: pose y activación de cada nodo, y color y visibilidad de cada gráfico.
        private static string Signature(CuiaView view)
        {
            var sb = new StringBuilder(512);
            Transform[] nodes = view.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                Transform node = nodes[i];
                sb.Append(node.name).Append(node.gameObject.activeSelf ? '+' : '-');
                if (node is RectTransform rt)
                {
                    Append(sb, rt.anchoredPosition);
                    Append(sb, rt.sizeDelta);
                }

                Append(sb, node.localEulerAngles);
                Append(sb, node.localScale);
                Graphic? graphic = node.GetComponent<Graphic>();
                if (graphic != null)
                {
                    sb.Append(graphic.enabled ? 'E' : 'D');
                    Color c = graphic.color;
                    Append(sb, new Vector4(c.r, c.g, c.b, c.a));
                }

                sb.Append('|');
            }

            return sb.ToString();
        }

        private static void Append(StringBuilder sb, Vector4 v)
        {
            sb.Append('(').Append(v.x.ToString("F3", CultureInfo.InvariantCulture))
                .Append(',').Append(v.y.ToString("F3", CultureInfo.InvariantCulture))
                .Append(',').Append(v.z.ToString("F3", CultureInfo.InvariantCulture))
                .Append(',').Append(v.w.ToString("F3", CultureInfo.InvariantCulture)).Append(')');
        }
    }
}
