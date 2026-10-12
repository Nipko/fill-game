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
    /// El rechazo de un toque ilegal conserva su sacudida (02 §4.1; ±8 dp, 3 ciclos y 240 ms desde T-G), que no bloquea la
    /// cola de toques: un toque válido durante la sacudida se atiende igual. El reloj lo lleva el test.
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
            // C2 (02 §3.1): toque en una cuia vacía sin selección → rechazo con sacudida de ±8 dp, 3 ciclos y 240 ms (T-G,
            // decisión del dueño del 2026-10-09, que sustituye a los ±6 dp en 120 ms de 02 §4.1).
            yield return PlayModeHelpers.LoadGame(2, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 200);
            CuiaView empty = boot.View!.Views[3];
            Assert.That(boot.Flow!.Session.Current[3].Count, Is.EqualTo(0));

            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Rejected));
            float maxShake = 0f;
            for (int t = 0; t < CuiaView.ShakeMs + (3 * PlayModeHelpers.FrameMs); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                maxShake = Mathf.Max(maxShake, Mathf.Abs(empty.Body.anchoredPosition.x));
            }

            Assert.That(CuiaView.ShakeDp, Is.EqualTo(8f), "±8 dp (T-G)");
            Assert.That(CuiaView.ShakeMs, Is.EqualTo(240f), "240 ms (T-G)");
            Assert.That(maxShake, Is.InRange(CuiaView.ShakeDp * 0.9f, CuiaView.ShakeDp + 1e-3f), "sacudida de ≈ 8 dp, a pasos de 16 ms");
            Assert.That(empty.Shaking, Is.False, "terminó a los 240 ms");
            Assert.That(empty.Body.anchoredPosition.x, Is.EqualTo(0f).Within(0.001f), "vuelve a su sitio");
            AssertNoTargetRing();
        }

        [UnityTest]
        public IEnumerator ValidTapDuringShake_IsServed([Values(false, true)] bool shakenAsTarget) // T-G: la sacudida no bloquea la cola
        {
            // Nivel 2: la cuia 3 está vacía. Se rechaza (C2) y, a los 64 ms de los 240 de la sacudida, llega un toque válido: un
            // origen que se elige (Selected) y un destino (Poured), que es la propia cuia que se sacude o una tercera. Se atiende
            // igual que sin sacudida, el vertido se encola y, al terminar, todas las cuias están en x = 0.
            yield return PlayModeHelpers.LoadGame(2, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 200);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            BoardState state = boot.Flow.Session.Current;
            const int empty = 3;
            Assert.That(state[empty].Count, Is.EqualTo(0), "la cuia 3 del nivel 2 está vacía");

            int source = -1;
            int target = -1;
            for (int a = 0; a < state.Count && source < 0; a++)
            {
                for (int b = 0; b < state.Count; b++)
                {
                    bool wantedTarget = shakenAsTarget ? b == empty : b != empty;
                    if (a != empty && a != b && wantedTarget && PourRules.IsLegal(state, a, b))
                    {
                        source = a;
                        target = b;
                        break;
                    }
                }
            }

            Assert.That(source, Is.GreaterThanOrEqualTo(0), "hay un vertido legal " + (shakenAsTarget ? "hacia la cuia vacía" : "entre otras dos cuias"));
            CuiaView[] views = boot.View!.Views;
            Assert.That(boot.TapCuia(empty).Kind, Is.EqualTo(TapKind.Rejected), "C2: la vacía no es origen");
            PlayModeHelpers.TickExact(boot, 64);
            Assert.That(views[empty].Shaking, Is.True, "a los 64 ms la sacudida sigue");
            Assert.That(Mathf.Abs(views[empty].Body.anchoredPosition.x), Is.GreaterThan(1f), "y la cuia está fuera de su sitio");

            int poursBefore = boot.Flow.Session.AppliedPours;
            TapResult selected = boot.TapCuia(source);
            Assert.That(selected.Kind, Is.EqualTo(TapKind.Selected), "el toque válido se atiende durante la sacudida");
            Assert.That(views[source].IsLifted, Is.True, "el origen sube");
            PlayModeHelpers.TickExact(boot, 32);
            Assert.That(views[empty].Shaking, Is.True, "la sacudida sigue mientras");
            TapResult poured = boot.TapCuia(target);
            Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured), "y el vertido también (a los 96 ms de 240)");
            Assert.That(boot.Flow.Session.AppliedPours, Is.EqualTo(poursBefore + 1), "el modelo aplica el vertido en el toque");
            Assert.That(boot.View.IsViewIdle, Is.False, "el vertido se encola en la vista");

            int ms = PlayModeHelpers.StepUntilIdle(boot, 4000);
            TestContext.WriteLine("Vertido " + source + " → " + target + " durante la sacudida: la vista quieta a los " + ms + " ms.");
            Assert.That(views[empty].Shaking, Is.False);
            for (int i = 0; i < views.Length; i++)
            {
                Assert.That(views[i].Body.anchoredPosition.x, Is.EqualTo(0f).Within(0.001f), "cuia " + i + ": en su sitio (x = 0)");
                Assert.That(views[i].IsLifted, Is.False, "cuia " + i + ": abajo");
            }

            MechanicTestKit.AssertViewMatchesModel(boot);
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
