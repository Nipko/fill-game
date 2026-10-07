#nullable enable
using System.Collections;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Pose y lectura del vertido (revisiones visuales del 2026-10-07 sobre <c>04_pouring_l7</c>): inclinación de 38°
    /// (02 §4.1); el labio queda entre 0,6 y 1,6 u sobre el borde del destino (07 §5.2 más la elevación máxima de 1 u), con el
    /// canto por el que sale el chorro sobre su boca; la silueta del origen no entra en el cuerpo ni en el hombro del
    /// destino; el chorro nace en el labio y termina en la superficie del destino (o en el fondo de su cavidad), dentro de
    /// su boca; las marcas de dosis del origen se apagan mientras está inclinado y el destino no empieza a llenarse hasta
    /// que la cabeza del chorro llega a su superficie. Una dosis por vertido (D-001). El reloj lo lleva el test.
    /// </summary>
    /// <remarks>
    /// Con 38° y dos filas no existe ninguna pose que deje libres a las vecinas (el eje cruza la ranura vecina a
    /// 3,5 / tan 38° ≈ 4,5 u bajo el labio): el tapado de una vecina durante el vertido (≤ 400 ms) es una pregunta para el
    /// dueño (inclinación de 75° o dinámica), así que estos tests no lo comprueban.
    /// </remarks>
    public sealed class PourPoseTests
    {
        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level1_TheLipStaysOverTheTargetMouth_AndTheStreamReachesItsSurface()
        {
            yield return PourPoseIsReadable(1);
        }

        [UnityTest]
        public IEnumerator Level7_TwoRows_TheLipStaysOverTheTargetMouth_AndTheStreamReachesItsSurface()
        {
            yield return PourPoseIsReadable(7);
        }

        // Primer vertido de la solución del nivel: comprueba la pose y el chorro con el origen ya inclinado.
        private static IEnumerator PourPoseIsReadable(int level)
        {
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 2000);
            SolveResult solution = new AStarSolver().Solve(boot.Flow!.Session.Current, SolverOptions.ForPipeline(boot.Flow.Session.PourMode));
            Pour first = solution.GetMove(0);
            Assert.That(boot.TapCuia(first.Source).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(first.Target).Kind, Is.EqualTo(TapKind.Poured));

            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, (int)view.Timings.TravelMs + PlayModeHelpers.FrameMs); // ya en la pose de vertido
            CuiaView s = view.Views[first.Source];
            CuiaView t = view.Views[first.Target];
            Assert.That(Mathf.Abs(s.BodyAngle), Is.EqualTo(BoardAnimator.TiltDeg).Within(0.5f), "inclinación de 02 §4.1");

            // Labio entre 0,6 y 1,6 u sobre el borde del destino (antes la búsqueda lo subía hasta 2,1 u o más).
            float tu = t.Unit;
            float rim = t.FootPosition.y + (t.HeightU * tu);
            float lipAbove = (s.LipInContainer().y - rim) / tu;
            Assert.That(lipAbove, Is.InRange(BoardAnimator.LipClearanceU - 0.05f, BoardAnimator.LipClearanceU + BoardAnimator.MaxRaiseU + 0.05f), "altura del labio sobre el borde del destino (u)");

            // El canto por el que sale el chorro queda sobre la boca del destino.
            float mouth = CuiaGeometry.InnerHalfWidth(CuiaGeometry.LipU(t.Capacity), t.Capacity) * tu;
            Assert.That(Mathf.Abs(s.PourPointInContainer().x - t.FootPosition.x), Is.LessThan(mouth), "canto de vertido sobre la boca");

            // La silueta del origen no entra en el cuerpo ni en el hombro del destino.
            float u = s.Unit;
            float angle = s.BodyAngle * Mathf.Deg2Rad;
            float shoulderTop = t.FootPosition.y + ((CuiaGeometry.CavityTopU(t.Capacity) + CuiaGeometry.ShoulderU) * tu);
            for (int r = 0; r <= 24; r++)
            {
                float y = r / 24f * s.HeightU;
                float hw = CuiaGeometry.OuterHalfWidth(Mathf.Min(y, s.HeightU - 0.001f), s.Capacity);
                for (int c = 0; c <= 6; c++)
                {
                    float x = -hw + (c / 6f * 2f * hw);
                    Vector2 p = s.FootPosition + s.BodyOffset + CuiaView.Rotate(new Vector2(x * u, y * u), angle);
                    if (p.y <= t.FootPosition.y || p.y >= shoulderTop)
                    {
                        continue;
                    }

                    float local = (p.y - t.FootPosition.y) / tu;
                    bool inside = Mathf.Abs(p.x - t.FootPosition.x) < CuiaGeometry.OuterHalfWidth(local, t.Capacity) * tu;
                    Assert.That(inside, Is.False, "el origen entra en el destino en (" + p.x + ", " + p.y + ")");
                }
            }

            // El chorro nace junto al labio y termina en la superficie del destino, dentro de su boca.
            StreamGraphic? stream = EnabledStream();
            Assert.That(stream, Is.Not.Null, "chorro visible");
            Assert.That(stream!.Range.y, Is.EqualTo(1f).Within(0.001f), "la cabeza del chorro ya llegó");
            Assert.That(Vector2.Distance(stream.CurveStart, s.PourPointInContainer()), Is.LessThan(0.25f * u), "nace en el labio");
            // La curva se calcula antes de que el destino suba en ese fotograma: un fotograma de llenado es 0,16 u.
            Assert.That(stream.CurveEnd.y, Is.EqualTo(t.SurfaceInContainer().y).Within(0.25f * tu), "termina en la superficie del destino");
            Assert.That(Mathf.Abs(stream.CurveEnd.x - t.FootPosition.x), Is.LessThan(mouth), "dentro de la boca del destino");

            PlayModeHelpers.StepUntilIdle(boot, 3000);
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

        [UnityTest]
        public IEnumerator TiltedSource_HidesItsDoseMarks_AndTheTargetWaitsForTheStreamHead()
        {
            // Nivel 2 (02 §13): la cuia 2 tiene una racha de 3 açaí y la 3 está vacía.
            yield return PlayModeHelpers.LoadGame(2, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 2000);
            GameScreen view = boot.View!;
            CuiaView source = view.Views[2];
            CuiaView target = view.Views[3];
            Assert.That(source.MarksVisible, Is.True);
            float before = target.LiquidLevel;

            Assert.That(boot.TapCuia(2).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Poured));

            // Chorro recién salido (< StreamGrowMs): la cabeza aún no ha llegado y el destino no ha subido.
            int streamStart = (int)(view.Timings.TravelMs * BoardAnimator.StreamStartFraction);
            PlayModeHelpers.Step(boot, streamStart + 24);
            Assert.That(source.MarksVisible, Is.False, "marcas del origen apagadas con la cuia inclinada");
            Assert.That(target.LiquidLevel, Is.EqualTo(before).Within(0.001f), "el destino espera a la cabeza del chorro");
            Assert.That(source.Liquid.Tongue, Is.GreaterThan(0f), "lengua por la pared inferior hasta el labio");

            PlayModeHelpers.StepUntilIdle(boot, 3000);
            Assert.That(source.MarksVisible, Is.True, "marcas de vuelta en la ranura");
            Assert.That(source.Liquid.Tongue, Is.EqualTo(0f));
            Assert.That(target.LiquidLevel, Is.EqualTo(before + 1f).Within(0.001f), "una dosis por vertido (D-001)");
        }
    }
}
