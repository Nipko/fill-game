#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AF.Core.Board;
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
    /// Pose y lectura del vertido (revisiones visuales del 2026-10-07 sobre <c>04_pouring_l7</c>): inclinación de 38°
    /// (02 §4.1); el labio queda entre 0,6 y 1,6 u sobre el borde del destino (07 §5.2 más la elevación máxima de 1 u), con el
    /// canto por el que sale el chorro sobre su boca; la silueta del origen no entra en el cuerpo ni en el hombro del
    /// destino; el chorro nace en el labio y termina en la superficie del destino (o en el fondo de su cavidad), dentro de
    /// su boca; las marcas de dosis del origen se apagan mientras está inclinado y el destino no empieza a llenarse hasta
    /// que la cabeza del chorro llega a su superficie. El origen inclinado no sube por encima del techo del vertido
    /// (<see cref="GameScreen.PourCeilingFor"/>): 4 dp de aire bajo el canto interior del marco en los tableros de una
    /// fila (<see cref="GameScreen.PourCeilingAirDp"/>) y el borde exterior del panel en los de dos filas, siempre bajo el
    /// HUD. Es un techo [P] derivado de D-004 (C18: con u = 32 la
    /// pose elevada salía del panel), no una respuesta del dueño. Una dosis por vertido (D-001). El reloj lo lleva el
    /// test, en el lienzo del teléfono (1080 × 2340 px, 360 × 780 dp): en batchmode la pantalla puede salir más ancha de
    /// 360 dp y el tope de tableta cambiaría u.
    /// </summary>
    /// <remarks>
    /// <para>Cobertura (revisión de D-004): el ajuste del techo cambia la pose con T = 3, 5, 6 y 7, así que para los
    /// niveles 1, 2, 4 y 7 se comprueban (a) las poses de todas las parejas (origen, destino), 6 + 20 + 30 + 42 = 98, con
    /// la misma búsqueda que usa la cola (<see cref="BoardAnimator.ComputePourPose"/>: depende solo de la geometría, no del
    /// contenido); (b) todos los vertidos legales del estado inicial, jugados con toques, con su chorro, y que su pose sea
    /// la de (a); y (c) vertidos concretos en los que el techo recorta la elevación (hacia la fila 1 en dos filas y el
    /// vertido cruzado del nivel 1 en los dos sentidos), con los vertidos previos que los hacen legales.</para>
    /// <para>Con 38° y dos filas no existe ninguna pose que deje libres a las vecinas (el eje cruza la ranura vecina a
    /// 3,5 / tan 38° ≈ 4,5 u bajo el labio). El dueño aceptó en D-004 (C17) que, en tableros de dos filas, el origen
    /// tape la parte alta de la vecina durante el vertido (≤ 400 ms). En una fila, el vertido entre cuias no vecinas
    /// tapa la parte alta de la del medio con cualquier elevación (captura <c>04b_pouring_l1</c>): el dueño también lo
    /// aceptó (D-004, Consecuencias). Estos tests no comprueban el tapado en ningún caso.</para>
    /// </remarks>
    public sealed class PourPoseTests
    {
        // Tolerancia de las comprobaciones de posición (dp).
        private const float ToleranceDp = 0.5f;

        // Paso de elevación de la búsqueda de pose (u): si el techo queda a menos de un paso sobre la silueta, es el techo
        // el que limita la elevación (el paso siguiente lo pasaría y se recortaría).
        private const float RaiseStepU = 0.25f;

        // Fin de la fila del HUD en el lienzo de prueba (32 dp de zona segura + 56 de HUD, guía 3.2).
        private const float HudBottomDp = 88f;

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level1_TheLipStaysOverTheTargetMouth_AndTheStreamReachesItsSurface()
        {
            // Primer vertido de la solución: 0 → 2, cruzado de izquierda a derecha sobre la del medio.
            yield return PourIsReadable(1, -1, -1, false);
        }

        [UnityTest]
        public IEnumerator Level1_CrossPourRightToLeft_StaysUnderTheFrame_AndTheCeilingLimitsTheRaise()
        {
            // 2 → 0, el sentido contrario del primer vertido de la solución: legal tras 1 → 2 y 0 → 1. Con u = 32 el techo,
            // 4 dp bajo el canto interior del marco, deja 4,5 dp (0,14 u) de elevación: la búsqueda la recorta a ≈ 0,14 u
            // (con 5,5 dp de aire, 0,09 u, el origen entraría en el hombro del destino).
            yield return PourIsReadable(1, 2, 0, true);
        }

        [UnityTest]
        public IEnumerator Level2_CrossPour_StaysUnderTheFrame()
        {
            // T = 5 (u ≈ 23,5 dp), 0 → 3 sobre dos cuias: la búsqueda querría subir el máximo, 1 u. Con el margen de 36 dp
            // de una fila el canto interior del marco deja ≈ 1,01 u de elevación y el techo, 4 dp más abajo, la recorta a
            // ≈ 0,84 u (con el borde exterior y 22 dp de margen la bajaba a 0,5 u).
            yield return PourIsReadable(2, 0, 3, true);
        }

        [UnityTest]
        public IEnumerator Level4_PourIntoRow1_StaysUnderTheCeiling_AndTheCeilingClipsTheRaise()
        {
            // T = 6 (dos filas de 3, u = 24): de la fila 2 a la fila 1, en diagonal; legal tras 0 → 4 y 0 → 5. El techo (borde
            // exterior del panel) recorta la elevación a ≈ 0,64 u.
            yield return PourIsReadable(4, 4, 0, true);
        }

        [UnityTest]
        public IEnumerator Level7_TwoRows_TheLipStaysOverTheTargetMouth_AndTheStreamReachesItsSurface()
        {
            yield return PourIsReadable(7, -1, -1, false);
        }

        [UnityTest]
        public IEnumerator Level7_PourIntoRow1_StaysUnderTheCeiling_AndTheCeilingClipsTheRaise()
        {
            // T = 7 (filas de 4 y 3, u = 24): de la fila 2 a la fila 1. El techo (borde exterior del panel) recorta la
            // elevación a ≈ 0,64 u.
            yield return PourIsReadable(7, 5, 1, true);
        }

        [UnityTest]
        public IEnumerator Level1_EveryPairAndEveryInitialPour_HaveAReadablePose()
        {
            yield return EveryPoseIsReadable(1);
        }

        [UnityTest]
        public IEnumerator Level2_EveryPairAndEveryInitialPour_HaveAReadablePose()
        {
            yield return EveryPoseIsReadable(2);
        }

        [UnityTest]
        public IEnumerator Level4_EveryPairAndEveryInitialPour_HaveAReadablePose()
        {
            yield return EveryPoseIsReadable(4);
        }

        [UnityTest]
        public IEnumerator Level7_EveryPairAndEveryInitialPour_HaveAReadablePose()
        {
            yield return EveryPoseIsReadable(7);
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

        // Vertido de source a target (el primero de la solución si source < 0), precedido de los vertidos más cortos que
        // lo hacen legal; comprueba la pose y el chorro con el origen ya inclinado y, si ceilingLimits, que es el techo el
        // que limita la elevación (queda a menos de un paso de elevación sobre la silueta).
        private static IEnumerator PourIsReadable(int level, int source, int target, bool ceilingLimits)
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
            Pour[]? setup = FindSetup(session.Current, source, target, session.PourMode, session.FutileRuleEnabled);
            Assert.That(setup, Is.Not.Null, label + ": ningún estado cercano hace legal el vertido");
            for (int i = 0; i < setup!.Length; i++)
            {
                TapPour(boot, setup[i].Source, setup[i].Target, label + " (vertido previo " + i + ")");
                PlayModeHelpers.StepUntilIdle(boot, 3000);
            }

            if (level == 1)
            {
                // D-004 (C18): tres cuias de 64 dp a 360 dp de ancho, u = 32.
                Assert.That(boot.View!.Views[source].Unit, Is.EqualTo(32f).Within(0.01f), "u del origen en el nivel 1");
                Assert.That(boot.View.Views[target].Unit, Is.EqualTo(32f).Within(0.01f), "u del destino en el nivel 1");
            }

            float headroom = PlayedPourIsReadable(boot, source, target, label);
            if (ceilingLimits)
            {
                float step = RaiseStepU * boot.View!.Views[source].Unit;
                Assert.That(headroom, Is.LessThan(step), label + ": el techo limita la elevación (holgura en dp, menos de un paso)");
            }

            PlayModeHelpers.StepUntilIdle(boot, 3000);
        }

        // (a) Todas las parejas con ComputePourPose y (b) todos los vertidos legales del estado inicial con toques, cada
        // uno deshecho después con Marea (ilimitada hasta el nivel 10, 04 §2.1).
        private static IEnumerator EveryPoseIsReadable(int level)
        {
            yield return LoadLevel(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelSession session = boot.Flow!.Session;
            BoardState initial = session.Current;
            int n = initial.Count;
            var report = new StringBuilder("AF_POSES nivel " + level + " (s→t: elevación u / holgura del techo dp):");
            int pairs = 0;
            int limited = 0;
            for (int s = 0; s < n; s++)
            {
                for (int t = 0; t < n; t++)
                {
                    if (s == t)
                    {
                        continue;
                    }

                    string label = "nivel " + level + ", pareja " + s + " → " + t;
                    view.Animator.ComputePourPose(s, t, out Vector2 offset, out float angle);
                    CuiaView sv = view.Views[s];
                    sv.Lock();
                    sv.SetPose(offset, angle);
                    float raise = LipAboveRimU(sv, view.Views[t]) - BoardAnimator.LipClearanceU;
                    float headroom = PoseIsReadable(view, s, t, label);
                    sv.Unlock();
                    pairs++;
                    limited += headroom < RaiseStepU * sv.Unit ? 1 : 0;
                    report.Append(' ').Append(s).Append('→').Append(t).Append(':')
                        .Append(raise.ToString("0.00", CultureInfo.InvariantCulture)).Append('/')
                        .Append(headroom.ToString("0.0", CultureInfo.InvariantCulture));
                }
            }

            Assert.That(pairs, Is.EqualTo(n * (n - 1)));
            int played = 0;
            for (int s = 0; s < n; s++)
            {
                for (int t = 0; t < n; t++)
                {
                    if (s == t || !PourRules.IsLegal(initial, s, t))
                    {
                        continue;
                    }

                    string label = "nivel " + level + ", vertido inicial " + s + " → " + t;
                    PlayedPourIsReadable(boot, s, t, label);
                    PlayModeHelpers.StepUntilIdle(boot, 3000);
                    PlayModeHelpers.Step(boot, 2 * PlayModeHelpers.FrameMs);
                    Assert.That(boot.PressUndo(), Is.True, label + ": Marea");
                    PlayModeHelpers.StepUntilIdle(boot, 3000);
                    Assert.That(session.Current, Is.EqualTo(initial), label + ": Marea vuelve al estado inicial");
                    played++;
                }
            }

            Assert.That(played, Is.GreaterThan(0), "el estado inicial tiene vertidos legales");
            report.Append(" | parejas ").Append(pairs).Append(", techo a menos de un paso de elevación ").Append(limited)
                .Append(", vertidos iniciales jugados ").Append(played);
            Debug.Log(report.ToString());
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

        private static void TapPour(GameBootstrap boot, int source, int target, string label)
        {
            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected), label + ": selección");
            Assert.That(boot.TapCuia(target).Kind, Is.EqualTo(TapKind.Poured), label + ": vertido");
        }

        // Juega el vertido con toques y, con el origen ya inclinado, comprueba la pose (la misma que ComputePourPose) y el
        // chorro. Devuelve la holgura (dp) entre el punto más alto de la silueta y el techo del vertido.
        private static float PlayedPourIsReadable(GameBootstrap boot, int source, int target, string label)
        {
            TapPour(boot, source, target, label);
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, (int)view.Timings.TravelMs + PlayModeHelpers.FrameMs); // ya en la pose de vertido
            CuiaView s = view.Views[source];
            CuiaView t = view.Views[target];
            view.Animator.ComputePourPose(source, target, out Vector2 offset, out float angle);
            Assert.That(Vector2.Distance(s.BodyOffset, offset), Is.LessThan(0.01f), label + ": la pose jugada es la de ComputePourPose");
            Assert.That(s.BodyAngle, Is.EqualTo(angle).Within(0.01f), label + ": ángulo de ComputePourPose");
            float headroom = PoseIsReadable(view, source, target, label);

            // El chorro nace junto al labio y termina en la superficie del destino, dentro de su boca.
            float u = s.Unit;
            float tu = t.Unit;
            float mouth = CuiaGeometry.InnerHalfWidth(CuiaGeometry.LipU(t.Capacity), t.Capacity) * tu;
            StreamGraphic? stream = EnabledStream();
            Assert.That(stream, Is.Not.Null, label + ": chorro visible");
            Assert.That(stream!.Range.y, Is.EqualTo(1f).Within(0.001f), label + ": la cabeza del chorro ya llegó");
            Assert.That(Vector2.Distance(stream.CurveStart, s.PourPointInContainer()), Is.LessThan(0.25f * u), label + ": nace en el labio");
            // La curva se calcula antes de que el destino suba en ese fotograma: un fotograma de llenado es 0,16 u.
            Assert.That(stream.CurveEnd.y, Is.EqualTo(t.SurfaceInContainer().y).Within(0.25f * tu), label + ": termina en la superficie del destino");
            Assert.That(Mathf.Abs(stream.CurveEnd.x - t.FootPosition.x), Is.LessThan(mouth), label + ": dentro de la boca del destino");
            return headroom;
        }

        // Comprueba la pose aplicada al cuerpo del origen: 38°, labio a 0,6-1,6 u sobre el borde del destino, canto de
        // vertido sobre su boca, la silueta no entra en el cuerpo ni en el hombro del destino y no pasa del techo del
        // vertido. Devuelve la holgura (dp) entre el punto más alto de la silueta y el techo.
        private static float PoseIsReadable(GameScreen view, int source, int target, string label)
        {
            CuiaView s = view.Views[source];
            CuiaView t = view.Views[target];
            Assert.That(Mathf.Abs(s.BodyAngle), Is.EqualTo(BoardAnimator.TiltDeg).Within(0.5f), label + ": inclinación de 02 §4.1");

            // Labio entre 0,6 y 1,6 u sobre el borde del destino (antes la búsqueda lo subía hasta 2,1 u o más).
            Assert.That(LipAboveRimU(s, t), Is.InRange(BoardAnimator.LipClearanceU - 0.05f, BoardAnimator.LipClearanceU + BoardAnimator.MaxRaiseU + 0.05f), label + ": altura del labio sobre el borde del destino (u)");

            // El canto por el que sale el chorro queda sobre la boca del destino.
            float tu = t.Unit;
            float mouth = CuiaGeometry.InnerHalfWidth(CuiaGeometry.LipU(t.Capacity), t.Capacity) * tu;
            Assert.That(Mathf.Abs(s.PourPointInContainer().x - t.FootPosition.x), Is.LessThan(mouth), label + ": canto de vertido sobre la boca");

            // La silueta del origen no entra en el cuerpo ni en el hombro del destino.
            float u = s.Unit;
            float angle = s.BodyAngle * Mathf.Deg2Rad;
            float shoulderTop = t.FootPosition.y + ((CuiaGeometry.CavityTopU(t.Capacity) + CuiaGeometry.ShoulderU) * tu);
            float maxY = float.MinValue;
            for (int r = 0; r <= 24; r++)
            {
                float y = r / 24f * s.HeightU;
                float hw = CuiaGeometry.OuterHalfWidth(Mathf.Min(y, s.HeightU - 0.001f), s.Capacity);
                for (int c = 0; c <= 6; c++)
                {
                    float x = -hw + (c / 6f * 2f * hw);
                    Vector2 p = s.FootPosition + s.BodyOffset + CuiaView.Rotate(new Vector2(x * u, y * u), angle);
                    maxY = Mathf.Max(maxY, p.y);
                    if (p.y <= t.FootPosition.y || p.y >= shoulderTop)
                    {
                        continue;
                    }

                    float local = (p.y - t.FootPosition.y) / tu;
                    bool inside = Mathf.Abs(p.x - t.FootPosition.x) < CuiaGeometry.OuterHalfWidth(local, t.Capacity) * tu;
                    Assert.That(inside, Is.False, label + ": el origen entra en el destino en (" + p.x + ", " + p.y + ")");
                }
            }

            // Techo del vertido [P] (y hacia arriba en el contenedor; el panel empieza en −PanelY): nunca sobre el borde
            // exterior del panel; en una fila, 4 dp de aire bajo el canto interior del marco (revisión de D-004 sobre
            // 04b_pouring_l1: con < 1 dp la cuia parecía apoyarse en el marco) y sin tolerancia; y el panel empieza bajo el
            // HUD.
            BoardLayoutResult layout = view.Layout!;
            float ceiling = view.Animator.TopLimitY;
            Assert.That(layout.PanelY, Is.GreaterThanOrEqualTo(HudBottomDp - 0.01f), label + ": el panel empieza bajo el HUD");
            Assert.That(ceiling, Is.LessThanOrEqualTo(-layout.PanelY + 0.01f), label + ": techo dentro del panel");
            float headroom = ceiling - maxY;
            if (layout.Rows == 1)
            {
                Assert.That(ceiling, Is.EqualTo(-(layout.PanelY + view.PanelFrameDp + GameScreen.PourCeilingAirDp)).Within(0.01f), label + ": en una fila, el techo queda 4 dp bajo el canto interior del marco");
                // ≥ 0 salvo el redondeo de float (la elevación recortada deja el canto alto del borde justo en el techo).
                Assert.That(headroom, Is.GreaterThanOrEqualTo(-0.01f), label + ": en una fila, holgura bajo el techo (dp)");
            }

            Assert.That(maxY, Is.LessThanOrEqualTo(ceiling + ToleranceDp), label + ": el origen inclinado pasa del techo del vertido (dp)");
            return headroom;
        }

        private static float LipAboveRimU(CuiaView s, CuiaView t)
        {
            float rim = t.FootPosition.y + (t.HeightU * t.Unit);
            return (s.LipInContainer().y - rim) / t.Unit;
        }

        // Vertidos más cortos desde state tras los que source → target es legal (BFS sobre el modelo con las reglas del
        // núcleo y el modo de vertido de la partida, D-001); vacío si ya lo es y null si no aparece en los primeros
        // estados. Los estados ganados o bloqueados no sirven de paso intermedio: en ellos el flujo no acepta toques.
        private static Pour[]? FindSetup(BoardState state, int source, int target, PourMode mode, bool futileRule)
        {
            const int maxStates = 20000;
            var parents = new Dictionary<BoardState, (BoardState? Parent, Pour Move)>();
            var queue = new Queue<BoardState>();
            parents.Add(state, (null, default));
            queue.Enqueue(state);
            int n = state.Count;
            while (queue.Count > 0 && parents.Count < maxStates)
            {
                BoardState current = queue.Dequeue();
                if (PourRules.IsLegal(current, source, target))
                {
                    var path = new List<Pour>();
                    for (BoardState? at = current; at != null && parents[at].Parent != null; at = parents[at].Parent)
                    {
                        path.Add(parents[at].Move);
                    }

                    path.Reverse();
                    return path.ToArray();
                }

                for (int a = 0; a < n; a++)
                {
                    for (int b = 0; b < n; b++)
                    {
                        if (a == b || !PourRules.IsLegal(current, a, b))
                        {
                            continue;
                        }

                        BoardState next = PourRules.Apply(current, a, b, mode);
                        if (parents.ContainsKey(next) || next.IsWon() || PourRules.IsStuck(next, futileRule))
                        {
                            continue;
                        }

                        parents.Add(next, (current, new Pour(a, b, PourRules.Amount(current, a, b, mode))));
                        queue.Enqueue(next);
                    }
                }
            }

            return null;
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
