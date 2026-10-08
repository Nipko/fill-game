#nullable enable
using System.Collections;
using System.Globalization;
using System.Text;
using AF.Core.Rules;
using AF.Core.Session;
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
    /// Pose y tiempos del vertido como Magic Sort (D-006, puntos 1 y 2; ficha §2.6; propuesta §3.2 y §3.5):
    /// (a) del fin del viaje al inicio del retorno, el centro de la boca del origen está sobre el eje del destino (±0,10 u)
    /// y el punto más bajo de la boca, a 0,20-0,60 u de su borde; (b) la inclinación sigue <see cref="BoardAnimator.TiltAt"/>
    /// en cada fotograma, sin bajar ni pararse en 38° (≈ 40° al terminar el viaje), y vale 92° − 9° × dosis que quedan (±1°)
    /// al terminar cada dosis y, la última, al cortarse el chorro; (c) el cuerpo queda del lado del que
    /// viene el origen (en la misma columna, hacia el centro del tablero), salvo que a su ángulo final saliera del lienzo,
    /// y ninguna silueta sale del lienzo para ninguna pareja de los niveles 1, 2, 4 y 7 con 0-3 dosis restantes; (d) escala
    /// 1,00; (f) el chorro sale junto al punto más bajo de la boca y acaba en la superficie del destino, dentro de su boca;
    /// (g) el nivel 2 jugado en run con la solución del informe llega a la celebración sin que salte el drenaje y sin saltos
    /// de la cola. El reloj lo lleva el test, en el lienzo del teléfono (1080 × 2340 px, 360 × 780 dp).
    /// </summary>
    /// <remarks>
    /// Sin techo (D-006): el origen puede pasar sobre el marco del panel y el HUD, porque se dibuja delante de todo (lo
    /// prueba <see cref="PourLayerTests"/>). A 38° el origen tapa parte de la cuia vecina y del propio destino, como Magic
    /// Sort en 3.100 ms: estos tests no comprueban el tapado.
    /// </remarks>
    public sealed class PourPoseTests
    {
        private const float AngleToleranceDeg = 1f;

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level1_FirstPour_FollowsTheMagicSortPose()
        {
            // 0 → 2: el origen (4 dosis) suelta 1 y le quedan 3: ≈ 40° al llegar (sin pararse) y 65° al cortarse el chorro.
            yield return LoadLevel(1);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.View!.Views[0].Unit, Is.EqualTo(32f).Within(0.01f), "u del nivel 1 (D-004)");
            PlayedPourFollowsThePose(boot, 0, 2, new[] { 65f }, "nivel 1, 0 → 2");
        }

        [UnityTest]
        public IEnumerator Level1_CrossPourRightToLeft_FollowsTheMagicSortPose()
        {
            // 2 → 0, el sentido contrario: legal tras 1 → 2 y 0 → 1.
            yield return PourWithSetup(1, 2, 0);
        }

        [UnityTest]
        public IEnumerator Level2_RunOfThreeOntoAnEmptyCuia_TiltsTo65_74And83()
        {
            // D-005 y D-006: la racha de 3 açaí de la cuia 2 (4 dosis) pasa a la 3 vacía en un vertido: 65°, 74° y 83°.
            yield return LoadLevel(2);
            PlayedPourFollowsThePose(GameBootstrap.Current!, 2, 3, new[] { 65f, 74f, 83f }, "nivel 2, 2 → 3");
        }

        [UnityTest]
        public IEnumerator Level4_PoursIntoRow1_FollowTheMagicSortPose()
        {
            // T = 6 (dos filas de 3): de la fila 2 a la fila 1, en la misma columna (el cuerpo va hacia el centro del
            // tablero) y en diagonal.
            yield return PourWithSetup(4, 3, 0);
            yield return PourWithSetup(4, 4, 0);
        }

        [UnityTest]
        public IEnumerator Level7_FirstPourAndAPourIntoRow1_FollowTheMagicSortPose()
        {
            yield return PourWithSetup(7, -1, -1);
            yield return PourWithSetup(7, 5, 1);
        }

        [UnityTest]
        public IEnumerator Level1_EveryPair_StaysOnTheCanvas_OnTheSourceSide()
        {
            yield return EveryPairStaysOnTheCanvas(1);
        }

        [UnityTest]
        public IEnumerator Level2_EveryPair_StaysOnTheCanvas_OnTheSourceSide()
        {
            yield return EveryPairStaysOnTheCanvas(2);
        }

        [UnityTest]
        public IEnumerator Level4_EveryPair_StaysOnTheCanvas_OnTheSourceSide()
        {
            yield return EveryPairStaysOnTheCanvas(4);
        }

        [UnityTest]
        public IEnumerator Level7_EveryPair_StaysOnTheCanvas_OnTheSourceSide()
        {
            yield return EveryPairStaysOnTheCanvas(7);
        }

        [UnityTest]
        public IEnumerator Level2_TheReportSolutionInRun_ReachesTheCelebration_WithoutADrainSnap()
        {
            // (g) Solución óptima en run de tools/out/tutorial_v0_report.md (nivel 2, 6 vertidos):
            // 0→3 ×2 · 0→4 ×1 · 0→3 ×1 · 1→3 ×1 · 2→4 ×3 · 1→2 ×3, tocada cada 1.000 ms (Magic Sort: 0,99 s entre vertidos como
            // mínimo y 3,1 s de mediana; ficha §2.6).
            int[,] moves = { { 0, 3, 2 }, { 0, 4, 1 }, { 0, 3, 1 }, { 1, 3, 1 }, { 2, 4, 3 }, { 1, 2, 3 } };
            yield return LoadLevel(2);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Session.PourMode, Is.EqualTo(PourMode.Run));
            for (int i = 0; i < moves.GetLength(0); i++)
            {
                if (i > 0)
                {
                    PlayModeHelpers.TickExact(boot, 1000);
                }

                Assert.That(boot.TapCuia(moves[i, 0]).Kind, Is.EqualTo(TapKind.Selected), "origen " + i);
                TapResult r = boot.TapCuia(moves[i, 1]);
                Assert.That(r.Kind, Is.EqualTo(TapKind.Poured), "destino " + i);
                Assert.That(r.Pour.Pour.Amount, Is.EqualTo(moves[i, 2]), "cantidad " + i);
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(flow.Level.Par));
            int drain = PlayModeHelpers.StepUntilIdle(boot, GameConfig.MaxViewDrainWaitMs);
            Assert.That(drain, Is.LessThan(GameConfig.MaxViewDrainWaitMs - PlayModeHelpers.FrameMs), "la cola se vacía antes del tope de drenaje");
            PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Celebration), "celebración al vaciarse la cola");
            Assert.That(boot.View!.Animator.LagSnapCount, Is.EqualTo(0), "sin saltos de la cola visual");
            Debug.Log("AF_DRAIN nivel 2: la cola se vació " + drain + " ms después del último toque");
        }

        [UnityTest]
        public IEnumerator TiltedSource_HidesItsDoseMarks_AndTheTargetWaitsForTheStreamHead()
        {
            // Nivel 2 (02 §13): la cuia 2 tiene una racha de 3 açaí y la 3 está vacía.
            yield return LoadLevel(2);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            CuiaView source = view.Views[2];
            CuiaView target = view.Views[3];
            Assert.That(source.MarksVisible, Is.True);
            float before = target.LiquidLevel;

            Assert.That(boot.TapCuia(2).Kind, Is.EqualTo(TapKind.Selected));
            TapResult poured = boot.TapCuia(3);
            Assert.That(poured.Kind, Is.EqualTo(TapKind.Poured));
            int amount = poured.Pour.Pour.Amount;

            // Chorro recién salido (< StreamGrowMs): la cabeza aún no ha llegado y el destino no ha subido.
            int streamStart = (int)(view.Timings.TravelMs * BoardAnimator.StreamStartFraction);
            PlayModeHelpers.TickExact(boot, streamStart + 24);
            Assert.That(source.MarksVisible, Is.False, "marcas del origen apagadas con la cuia inclinada");
            Assert.That(target.LiquidLevel, Is.EqualTo(before).Within(0.001f), "el destino espera a la cabeza del chorro");
            Assert.That(source.Liquid.Tongue, Is.GreaterThan(0f), "lengua por la pared inferior hasta el labio");

            PlayModeHelpers.StepUntilIdle(boot, 3000);
            Assert.That(source.MarksVisible, Is.True, "marcas de vuelta en la ranura");
            Assert.That(source.Liquid.Tongue, Is.EqualTo(0f));
            Assert.That(target.LiquidLevel, Is.EqualTo(before + amount).Within(0.001f), "el destino recibe la cantidad del vertido");
        }

        [Test]
        public void TiltCurve_OfOneDoseFromAFullCuia_FollowsMagicSort_Every100ms()
        {
            // D-006 y ficha §2.6: Magic Sort gira 12°, 24°, 38°, 52°, 62° cada 100 ms desde la salida y llega a 66° con el
            // final del chorro; sin parada en 38°. Con los defaults de 08 §9: chorro a los 210 ms, dosis de 280 y cola de 80.
            float[] magicSort = { 0f, 12f, 24f, 38f, 52f, 62f };
            float previous = -1f;
            for (int i = 0; i < magicSort.Length; i++)
            {
                float tilt = BoardAnimator.TiltAt(i * 100f, 210f, 280f, 80f, 4, 1);
                Assert.That(tilt, Is.EqualTo(magicSort[i]).Within(3f), "a los " + (i * 100) + " ms");
                Assert.That(tilt, Is.GreaterThan(previous), "sube sin pararse");
                previous = tilt;
            }

            float before38 = BoardAnimator.TiltAt(290f, 210f, 280f, 80f, 4, 1);
            float after38 = BoardAnimator.TiltAt(310f, 210f, 280f, 80f, 4, 1);
            Assert.That((after38 - before38) * 5f, Is.GreaterThan(10f), "≈ 13° cada 100 ms al pasar por 38° (sin velocidad 0)");
            Assert.That(BoardAnimator.TiltAt(570f, 210f, 280f, 80f, 4, 1), Is.EqualTo(65f).Within(1e-3f), "65° al cortarse el chorro");
            Assert.That(BoardAnimator.TiltAt(900f, 210f, 280f, 80f, 4, 1), Is.EqualTo(65f).Within(1e-3f));

            // Racha de 3 de una cuia llena: 65° y 74° al terminar las dosis 1 y 2 y 83° al cortarse el chorro.
            Assert.That(BoardAnimator.TiltAt(490f, 210f, 280f, 80f, 4, 3), Is.EqualTo(65f).Within(1e-3f));
            Assert.That(BoardAnimator.TiltAt(770f, 210f, 280f, 80f, 4, 3), Is.EqualTo(74f).Within(1e-3f));
            Assert.That(BoardAnimator.TiltAt(1130f, 210f, 280f, 80f, 4, 3), Is.EqualTo(83f).Within(1e-3f));
            previous = -1f;
            for (int ms = 0; ms <= 1200; ms += 5)
            {
                float tilt = BoardAnimator.TiltAt(ms, 210f, 280f, 80f, 4, 3);
                Assert.That(tilt, Is.GreaterThanOrEqualTo(previous - 1e-4f), "no decreciente a los " + ms + " ms");
                previous = tilt;
            }
        }

        [Test]
        public void TiltAfterDose_Is92Minus9PerRemainingDose()
        {
            Assert.That(BoardAnimator.TiltAfterDose(3), Is.EqualTo(65f).Within(1e-4f));
            Assert.That(BoardAnimator.TiltAfterDose(2), Is.EqualTo(74f).Within(1e-4f));
            Assert.That(BoardAnimator.TiltAfterDose(1), Is.EqualTo(83f).Within(1e-4f));
            Assert.That(BoardAnimator.TiltAfterDose(0), Is.EqualTo(92f).Within(1e-4f));
            Assert.That(BoardAnimator.TiltAfterDose(9), Is.EqualTo(BoardAnimator.ArrivalTiltDeg).Within(1e-4f), "nunca menos que la llegada");
        }

        [Test]
        public void PourDuration_Is870_1150And1430ms_WithTheTimingsOfD006()
        {
            // (e) PourTimings(anim_lift_ms, anim_travel_ms, anim_pour_dose_ms, anim_pour_tail_ms, anim_return_ms).
            var timings = new PourTimings(100f, 300f, 280f, 80f, 300f);
            Assert.That(BoardAnimator.PourDurationMs(timings, 1), Is.EqualTo(870f).Within(0.01f));
            Assert.That(BoardAnimator.PourDurationMs(timings, 2), Is.EqualTo(1150f).Within(0.01f));
            Assert.That(BoardAnimator.PourDurationMs(timings, 3), Is.EqualTo(1430f).Within(0.01f));
            Assert.That(GameConfig.AnimLiftMs, Is.EqualTo(100));
            Assert.That(GameConfig.AnimTravelMs, Is.EqualTo(300));
            Assert.That(GameConfig.AnimPourDoseMs, Is.EqualTo(280));
            Assert.That(GameConfig.AnimPourTailMs, Is.EqualTo(80));
            Assert.That(GameConfig.AnimReturnMs, Is.EqualTo(300));
        }

        private static IEnumerator LoadLevel(int level)
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // 360 × 780 dp, como VisualSnapshotTests
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore());
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.StepUntilIdle(boot, 2000);
        }

        // Vertido de source a target (el primero de la solución si source < 0), precedido de los vertidos más cortos que
        // lo hacen legal; comprueba la pose en cada fotograma.
        private static IEnumerator PourWithSetup(int level, int source, int target)
        {
            yield return LoadLevel(level);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelSession session = boot.Flow!.Session;
            if (source < 0)
            {
                SolveResult solution = new AStarSolver().Solve(session.Current, SolverOptions.ForPipeline(session.PourMode));
                Pour first = solution.GetMove(0);
                source = first.Source;
                target = first.Target;
            }

            string label = "nivel " + level + ", " + source + " → " + target;
            Pour[]? setup = PlayModeHelpers.FindSetup(session.Current, source, target, session.PourMode, session.FutileRuleEnabled);
            Assert.That(setup, Is.Not.Null, label + ": ningún estado cercano hace legal el vertido");
            for (int i = 0; i < setup!.Length; i++)
            {
                TapPour(boot, setup[i].Source, setup[i].Target, label + " (vertido previo " + i + ")");
                PlayModeHelpers.StepUntilIdle(boot, 3000);
            }

            PlayedPourFollowsThePose(boot, source, target, null, label);
        }

        // Juega source → target con el reloj exacto y comprueba (a), (b), (d) y (f) en cada fotograma del viaje al retorno,
        // y (b) en los instantes clave: fin del viaje (la inclinación de TiltAt, ≥ 38°) y fin de cada dosis (92° − 9° ×
        // dosis que quedan; la última, al cortarse el chorro). Si expectedTilts no es null, también los ángulos esperados
        // por dosis.
        private static void PlayedPourFollowsThePose(GameBootstrap boot, int source, int target, float[]? expectedTilts, string label)
        {
            GameScreen view = boot.View!;
            CuiaView s = view.Views[source];
            CuiaView t = view.Views[target];
            int before = boot.Flow!.Session.Current[source].Count;
            TapResult r = TapPour(boot, source, target, label);
            int amount = r.Pour.Pour.Amount;
            int remainingAfter = before - amount;
            if (expectedTilts != null)
            {
                Assert.That(amount, Is.EqualTo(expectedTilts.Length), label + ": dosis del vertido");
            }

            PourTimings timings = view.Timings;
            int travel = Mathf.RoundToInt(timings.TravelMs);
            int streamStart = Mathf.RoundToInt(timings.TravelMs * BoardAnimator.StreamStartFraction);
            int dose = Mathf.RoundToInt(timings.DoseMs);
            int flowEnd = streamStart + (amount * dose);
            int returnStart = Mathf.Max(travel, flowEnd + Mathf.RoundToInt(timings.TailMs));
            float arrivalTilt = BoardAnimator.TiltAt(travel, streamStart, dose, timings.TailMs, before, amount);
            Assert.That(arrivalTilt, Is.GreaterThanOrEqualTo(BoardAnimator.ArrivalTiltDeg), label + ": al terminar el viaje ya pasó de 38° (b)");
            view.Animator.ComputePourPose(source, target, remainingAfter, arrivalTilt, out Vector2 arrivalOffset, out float arrivalAngle);
            float side = Mathf.Sign(arrivalAngle);

            int elapsed = 0;
            float previousTilt = 0f;
            int nextDose = 1;
            bool sawStream = false;
            while (elapsed < returnStart)
            {
                // Puntos clave: fin del viaje, fin de cada dosis y, la última, fin del chorro (cola incluida).
                int doseKey = nextDose < amount ? streamStart + (nextDose * dose) : returnStart;
                int key = elapsed < travel ? travel : (nextDose <= amount ? doseKey : returnStart);
                int next = Mathf.Min(elapsed + PlayModeHelpers.FrameMs, Mathf.Max(key, elapsed + 1));
                PlayModeHelpers.TickExact(boot, next - elapsed);
                elapsed = next;
                string at = label + " a los " + elapsed + " ms";
                Assert.That(s.Body.localScale, Is.EqualTo(Vector3.one), at + ": escala 1,00 (d)");
                float tilt = Mathf.Abs(s.BodyAngle);
                Assert.That(tilt, Is.GreaterThanOrEqualTo(previousTilt - 0.01f), at + ": la inclinación no baja hasta la vuelta (b)");
                Assert.That(tilt, Is.EqualTo(BoardAnimator.TiltAt(elapsed, streamStart, dose, timings.TailMs, before, amount)).Within(0.05f), at + ": sigue la curva de TiltAt (b)");
                previousTilt = tilt;
                if (elapsed < travel)
                {
                    continue;
                }

                Assert.That(Mathf.Sign(s.BodyAngle), Is.EqualTo(side), at + ": el lado no cambia");
                if (elapsed == travel)
                {
                    Assert.That(tilt, Is.EqualTo(arrivalTilt).Within(0.05f), at + ": inclinación al terminar el viaje (b)");
                    Assert.That(Vector2.Distance(s.BodyOffset, arrivalOffset), Is.LessThan(0.01f), at + ": la pose de llegada es la de ComputePourPose");
                }

                if (nextDose <= amount && elapsed == doseKey)
                {
                    float expected = BoardAnimator.TiltAfterDose(before - nextDose);
                    Assert.That(tilt, Is.EqualTo(expected).Within(AngleToleranceDeg), at + ": 92° − 9° × dosis que quedan al terminar la dosis " + nextDose + " (b)");
                    if (expectedTilts != null)
                    {
                        Assert.That(tilt, Is.EqualTo(expectedTilts[nextDose - 1]).Within(AngleToleranceDeg), at + ": ángulo esperado de la dosis " + nextDose);
                    }

                    nextDose++;
                }

                if (elapsed < returnStart)
                {
                    MouthIsOverTheTarget(s, t, at);
                    sawStream |= StreamLeavesTheMouthAndReachesTheSurface(s, t, at);
                }
            }

            Assert.That(nextDose, Is.EqualTo(amount + 1), label + ": se comprobaron todas las dosis");
            Assert.That(sawStream, Is.True, label + ": chorro con la cabeza en la superficie (f)");

            // (c) El cuerpo (su pie), del lado del que viene el origen, salvo la excepción del lienzo.
            if (Mathf.Abs(t.FootPosition.x - s.FootPosition.x) > 1f)
            {
                bool sourceSide = Mathf.Sign(s.FootPosition.x - t.FootPosition.x) == Mathf.Sign(s.FootPosition.x + s.BodyOffset.x - t.FootPosition.x);
                if (!sourceSide)
                {
                    Assert.That(NaturalSideLeavesTheCanvas(view, source, target, remainingAfter), Is.True, label + ": cambia de lado solo si el suyo sale del lienzo (c)");
                }
            }

            PlayModeHelpers.StepUntilIdle(boot, 3000);
            Assert.That(s.BodyAngle, Is.EqualTo(0f).Within(0.01f), label + ": vuelve recta a su ranura");
            Assert.That(s.BodyOffset.magnitude, Is.LessThan(0.01f), label + ": vuelve a su ranura");
        }

        // (a) Centro de la boca sobre el eje del destino (±0,10 u) y su punto más bajo a 0,20-0,60 u del borde del destino.
        private static void MouthIsOverTheTarget(CuiaView s, CuiaView t, string label)
        {
            float tu = t.Unit;
            float rim = t.FootPosition.y + (t.HeightU * tu);
            Vector2 mouth = s.MouthCenterInContainer();
            Assert.That(Mathf.Abs(mouth.x - t.FootPosition.x) / tu, Is.LessThanOrEqualTo(0.10f), label + ": centro de la boca sobre el eje del destino (u) (a)");
            Assert.That((s.MouthLowestInContainer().y - rim) / tu, Is.InRange(0.20f, 0.60f), label + ": punto más bajo de la boca sobre el borde del destino (u) (a)");
        }

        // (f) Con la cabeza del chorro en la superficie: nace en el canto interior bajo de la boca, a ≤ 0,25 u de su punto más
        // bajo, y acaba en la superficie del destino, dentro de su boca. Devuelve si había chorro que comprobar.
        private static bool StreamLeavesTheMouthAndReachesTheSurface(CuiaView s, CuiaView t, string label)
        {
            StreamGraphic? stream = EnabledStream();
            if (stream == null || stream.Range.y < 0.999f || stream.Range.x > 0.001f)
            {
                return false;
            }

            float u = s.Unit;
            float tu = t.Unit;
            float mouth = CuiaGeometry.InnerHalfWidth(CuiaGeometry.LipU(t.Capacity), t.Capacity) * tu;
            Assert.That(Vector2.Distance(s.PourPointInContainer(), s.MouthLowestInContainer()), Is.LessThanOrEqualTo(0.25f * u), label + ": el canto de vertido está junto al punto más bajo de la boca (f)");
            Assert.That(Vector2.Distance(stream.CurveStart, s.PourPointInContainer()), Is.LessThan(0.25f * u), label + ": el chorro nace en la boca (f)");
            // La curva se calcula antes de que el destino suba en ese fotograma: un fotograma de llenado es < 0,1 u.
            Assert.That(stream.CurveEnd.y, Is.EqualTo(t.SurfaceInContainer().y).Within(0.25f * tu), label + ": termina en la superficie del destino (f)");
            Assert.That(Mathf.Abs(stream.CurveEnd.x - t.FootPosition.x), Is.LessThan(mouth), label + ": dentro de la boca del destino (f)");
            return true;
        }

        // (c) Para todas las parejas (origen, destino) y 0-3 dosis restantes: la silueta del origen no sale del lienzo en la
        // pose de llegada (38°) ni en la de cada dosis hasta la final; y el cuerpo queda del lado del origen (en la misma
        // columna, hacia el centro), salvo que ese lado saliera del lienzo a su ángulo final.
        private static IEnumerator EveryPairStaysOnTheCanvas(int level)
        {
            yield return LoadLevel(level);
            GameScreen view = GameBootstrap.Current!.View!;
            int n = view.Views.Length;
            float width = view.CanvasWidth;
            var report = new StringBuilder("AF_POSES nivel " + level + " (cambios de lado por el lienzo, s→t/dosis):");
            int pairs = 0;
            int flips = 0;
            for (int src = 0; src < n; src++)
            {
                for (int dst = 0; dst < n; dst++)
                {
                    if (src == dst)
                    {
                        continue;
                    }

                    pairs++;
                    CuiaView s = view.Views[src];
                    for (int remaining = 0; remaining <= 3; remaining++)
                    {
                        string label = "nivel " + level + ", " + src + " → " + dst + " con " + remaining + " dosis";
                        float side = PoseStaysOnTheCanvas(view, s, src, dst, remaining, BoardAnimator.ArrivalTiltDeg, width, label);
                        for (int m = 3; m >= remaining; m--)
                        {
                            Assert.That(PoseStaysOnTheCanvas(view, s, src, dst, remaining, BoardAnimator.TiltAfterDose(m), width, label), Is.EqualTo(side), label + ": un solo lado");
                        }

                        CuiaView t = view.Views[dst];
                        float dx = t.FootPosition.x - s.FootPosition.x;
                        float natural = Mathf.Abs(dx) > 1f ? (dx > 0f ? -1f : 1f) : (t.FootPosition.x > width * 0.5f ? -1f : 1f);
                        if (side != natural)
                        {
                            flips++;
                            Assert.That(NaturalSideLeavesTheCanvas(view, src, dst, remaining), Is.True, label + ": cambia de lado solo si el suyo sale del lienzo (c)");
                            report.Append(' ').Append(src).Append('→').Append(dst).Append('/').Append(remaining);
                        }
                    }
                }
            }

            Assert.That(pairs, Is.EqualTo(n * (n - 1)));
            report.Append(" | parejas ").Append(pairs).Append(", cambios de lado ").Append(flips);
            Debug.Log(report.ToString());
        }

        // Pose de ComputePourPose con la inclinación tilt: la silueta queda dentro del lienzo. Devuelve el signo del ángulo.
        private static float PoseStaysOnTheCanvas(GameScreen view, CuiaView s, int src, int dst, int remaining, float tilt, float width, string label)
        {
            view.Animator.ComputePourPose(src, dst, remaining, tilt, out Vector2 offset, out float angle);
            Assert.That(Mathf.Abs(angle), Is.EqualTo(tilt).Within(1e-3f), label);
            Assert.That(SilhouetteInside(s, offset, angle, width), Is.True, label + ", " + tilt.ToString("0", CultureInfo.InvariantCulture) + "°: la silueta no sale del lienzo (c)");
            Assert.That(view.Animator.SilhouetteInsideBoard(src, offset, angle), Is.True, label + ": la comprobación del animador coincide");
            return Mathf.Sign(angle);
        }

        // El lado natural del cuerpo (el del origen; en la misma columna, hacia el centro) sale del lienzo en alguna
        // inclinación del vertido, de 38° a su ángulo final (cada 3°, la regla [P] de D-006 que aplica la cola).
        private static bool NaturalSideLeavesTheCanvas(GameScreen view, int source, int target, int remaining)
        {
            CuiaView s = view.Views[source];
            CuiaView t = view.Views[target];
            float width = view.CanvasWidth;
            float dx = t.FootPosition.x - s.FootPosition.x;
            float natural = Mathf.Abs(dx) > 1f ? (dx > 0f ? -1f : 1f) : (t.FootPosition.x > width * 0.5f ? -1f : 1f);
            float final = BoardAnimator.TiltAfterDose(remaining);
            Vector2 mouth = t.FootPosition + new Vector2(0f, (t.HeightU + BoardAnimator.MouthCenterAboveRimU) * t.Unit);
            for (float tilt = BoardAnimator.ArrivalTiltDeg; ; tilt += 3f)
            {
                float angle = natural * Mathf.Min(tilt, final);
                Vector2 offset = mouth - s.FootPosition - CuiaView.Rotate(new Vector2(0f, s.HeightU * s.Unit), angle * Mathf.Deg2Rad);
                if (!SilhouetteInside(s, offset, angle, width))
                {
                    return true;
                }

                if (tilt >= final)
                {
                    return false;
                }
            }
        }

        // Muestreo propio de la silueta exterior (25 × 7 puntos con los bordes): dentro del lienzo con 0,5 dp de tolerancia.
        private static bool SilhouetteInside(CuiaView s, Vector2 offset, float angleDeg, float width)
        {
            float u = s.Unit;
            float rad = angleDeg * Mathf.Deg2Rad;
            for (int r = 0; r <= 24; r++)
            {
                float y = r / 24f * s.HeightU;
                float hw = CuiaGeometry.OuterHalfWidth(Mathf.Min(y, s.HeightU - 0.001f), s.Capacity);
                for (int c = 0; c <= 6; c++)
                {
                    float x = -hw + (c / 6f * 2f * hw);
                    Vector2 p = s.FootPosition + offset + CuiaView.Rotate(new Vector2(x * u, y * u), rad);
                    if (p.x < -0.5f || p.x > width + 0.5f || p.y > 0.5f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static TapResult TapPour(GameBootstrap boot, int source, int target, string label)
        {
            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected), label + ": selección");
            TapResult r = boot.TapCuia(target);
            Assert.That(r.Kind, Is.EqualTo(TapKind.Poured), label + ": vertido");
            return r;
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
