#nullable enable
using System.Collections;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Sacudida de rechazo (T-G, decisión del dueño del 2026-10-09; sustituye a los ±6 dp, 2 ciclos y 120 ms de 02 §4.1 y 07
    /// §4.3, que se alinean en el paso 2): al tocar como origen una cuia vacía, una Hoja cerrada (43), un Matapi (63) o una cuia
    /// sellada, la cuia se sacude ±8 dp en horizontal durante 240 ms, 3 ciclos sin amortiguar, y vuelve a su sitio exacto (x =
    /// 0). Se mide con el reloj del test a pasos de 1 ms: la duración, el pico, los 3 lóbulos a cada lado (5 cruces por cero
    /// dentro) y la vuelta. Sin rojo ni texto, y nada sube (D-002).
    /// </summary>
    public sealed class RejectShakeTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator EmptyCuia_L2()
        {
            yield return MechanicTestKit.LoadPlaying(2);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Session.Current[3].Count, Is.EqualTo(0), "la cuia 3 del nivel 2 está vacía");
            MeasureShake(boot, 3, "cuia vacía");
        }

        [UnityTest]
        public IEnumerator ClosedHoja_L43()
        {
            yield return MechanicTestKit.LoadPlaying(43);
            GameBootstrap boot = GameBootstrap.Current!;
            int hoja = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Hoja);
            MeasureShake(boot, hoja, "Hoja cerrada");
        }

        [UnityTest]
        public IEnumerator Matapi_L63()
        {
            yield return MechanicTestKit.LoadPlaying(63);
            GameBootstrap boot = GameBootstrap.Current!;
            int matapi = MechanicTestKit.IndexOf(boot.Flow!, MechanicType.Matapi);
            var overlay = (MatapiOverlay)boot.View!.Views[matapi].Overlay!;
            Assert.That(MatapiOverlay.PulseMs, Is.EqualTo(CuiaView.ShakeMs), "el embudo late lo que dura la sacudida");
            Assert.That(MatapiOverlay.PulseMs, Is.LessThanOrEqualTo(300f), "≤ 300 ms [P] (criterio de la ola 3)");
            MeasureShake(boot, matapi, "Matapi");
            Assert.That(overlay.FunnelPulse, Is.EqualTo(1f), "el latido terminó con la sacudida");
        }

        [UnityTest]
        public IEnumerator SealedCuia_L7()
        {
            yield return MechanicTestKit.LoadPlaying(7);
            GameBootstrap boot = GameBootstrap.Current!;
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.SealedMask != 0 && !a.IsWon());
            Assert.That(path, Is.Not.Null, "vertidos que sellan una cuia en el nivel 7");
            MechanicTestKit.PlayPrefix(boot, path!, skipLast: false);
            MechanicTestKit.StepToIdle(boot, 6000);
            BoardState state = boot.Flow!.Session.Current;
            int sealedCuia = -1;
            for (int i = 0; i < state.Count && sealedCuia < 0; i++)
            {
                if (state.IsSealed(i))
                {
                    sealedCuia = i;
                }
            }

            Assert.That(sealedCuia, Is.GreaterThanOrEqualTo(0));
            Assert.That(boot.View!.Views[sealedCuia].Sealed, Is.True, "con la tapa puesta");
            MeasureShake(boot, sealedCuia, "cuia sellada");
            Assert.That(boot.View.Views[sealedCuia].Sealed, Is.True, "sigue sellada");
        }

        // Toca la cuia como origen (rechazo) y avanza 1 ms cada vez hasta 280 ms: la sacudida es ShakeOffsetAt(t), con el pico
        // de 8 dp, 3 lóbulos positivos y 3 negativos (5 cruces por cero dentro), dura 240 ms y deja la cuia en x = 0 exacto.
        private static void MeasureShake(GameBootstrap boot, int index, string what)
        {
            CuiaView view = boot.View!.Views[index];
            Assert.That(view.Body.anchoredPosition.x, Is.EqualTo(0f), what + ": en su sitio antes");
            TapResult tap = boot.TapCuia(index);
            Assert.That(tap.Kind, Is.EqualTo(TapKind.Rejected), what + ": rechazo como origen");
            Assert.That(tap.Selection, Is.EqualTo(TapController.NoSelection), what + ": nada queda seleccionado");
            Assert.That(view.Shaking, Is.True, what + ": la sacudida empieza en el toque");

            float peak = 0f;
            float prev = 0f;
            int crossings = 0;
            int positiveLobes = 0;
            int negativeLobes = 0;
            int lastMoving = -1;
            for (int t = 1; t <= 280; t++)
            {
                PlayModeHelpers.TickExact(boot, 1);
                float x = view.Body.anchoredPosition.x;
                Assert.That(view.IsLifted, Is.False, what + ": no sube");
                if (t < CuiaView.ShakeMs)
                {
                    Assert.That(x, Is.EqualTo(CuiaView.ShakeOffsetAt(t)).Within(1e-3f), what + ": x a los " + t + " ms");
                }
                else
                {
                    Assert.That(x, Is.EqualTo(0f), what + ": a los " + t + " ms ya en su sitio, exacto");
                }

                if (Mathf.Abs(x) > 1e-4f)
                {
                    lastMoving = t;
                    if ((prev > 1e-4f && x < -1e-4f) || (prev < -1e-4f && x > 1e-4f))
                    {
                        crossings++;
                    }

                    if (x > 0f && prev <= 0f)
                    {
                        positiveLobes++;
                    }
                    else if (x < 0f && prev >= 0f)
                    {
                        negativeLobes++;
                    }

                    prev = x;
                }

                peak = Mathf.Max(peak, Mathf.Abs(x));
            }

            TestContext.WriteLine(what + ": pico " + peak.ToString("F3") + " dp, " + crossings + " cruces, " + positiveLobes + "+" + negativeLobes + " lóbulos, último movimiento a los " + lastMoving + " ms.");
            Assert.That(peak, Is.EqualTo(CuiaView.ShakeDp).Within(0.02f), what + ": pico de ±8 dp");
            Assert.That(positiveLobes, Is.EqualTo(CuiaView.ShakeCycles), what + ": 3 ciclos (lóbulos a la derecha)");
            Assert.That(negativeLobes, Is.EqualTo(CuiaView.ShakeCycles), what + ": 3 ciclos (lóbulos a la izquierda)");
            Assert.That(crossings, Is.EqualTo((2 * CuiaView.ShakeCycles) - 1), what + ": 5 cruces por cero dentro de la sacudida");
            Assert.That(lastMoving, Is.InRange(235, 239), what + ": dura 240 ms");
            Assert.That(view.Shaking, Is.False, what + ": terminó");
            Assert.That(view.BodyOffset.x, Is.EqualTo(0f), what + ": x = 0 exacto");
            Assert.That(view.GlassBoosted, Is.False, what + ": sin brillo de selección (D-002)");
            MechanicTestKit.AssertViewMatchesModel(boot);
        }
    }
}
