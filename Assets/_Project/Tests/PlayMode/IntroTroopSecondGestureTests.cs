#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Segundo gesto de los miquitos en la escena real (F13; añadido de D-029 del 2026-11-08): en la mitad de cada mundo largo
    /// (251, 361, 491, 631, 761 y 871) toda la tropa hace el gesto de su especie en el reposo tras la última suelta, y se nota
    /// (el dúo sube la cabeza ≥ 20°, los brincos suben ≥ 0,25 S, colgarse de la cola se mece ≥ 14°, el desperezo inclina el torso
    /// ≥ 10° y el vuelo se aparta ≥ 0,5 S y vuelve); T, el toque y la salida no cambian (se va antes de T + 2,2 s y sale desde su
    /// sitio); el nivel siguiente vuelve al primer gesto; la cabeza nunca baja (D-002); 0 B por fotograma; y con Reducir
    /// movimiento, un solo miquito quieto en la pose del gesto. El extra <c>af_troop_gesture</c> lo fuerza o lo quita.
    /// </summary>
    public sealed class IntroTroopSecondGestureTests
    {
        private const int Short = 800;

        // Mitad de cada mundo largo: (mundo, id, nivel, gesto).
        internal static readonly (int World, string WorldId, int Level, FaunaSecondGesture Gesture)[] Mids =
        {
            (6, "w06_oriente", 251, FaunaSecondGesture.Duet),
            (7, "w07_madre_de_dios", 361, FaunaSecondGesture.Hops),
            (8, "w08_madidi", 491, FaunaSecondGesture.TailHang),
            (9, "w09_ucayali", 631, FaunaSecondGesture.TailHang),
            (10, "w10_alto_negro", 761, FaunaSecondGesture.Stretch),
            (11, "w11_escudo_guayana", 871, FaunaSecondGesture.Leap),
        };

        private static readonly int[] MidIndex = { 0, 1, 2, 3, 4, 5 };

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        /// <summary>Carga el tablero del nivel 7 en <paramref name="level"/> del mundo <paramref name="world"/> (pantalla de 360 × 780 dp).</summary>
        internal static IEnumerator Load(int world, string worldId, int level)
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            yield return PlayModeHelpers.LoadGameWithPack(PlayModeHelpers.Level7BoardPack(world, worldId, "white", level), level,
                new MemoryProgressStore(level, level - 1));
        }

        [UnityTest]
        public IEnumerator SecondGesture_PlaysInTheRest_Reads_AndKeepsTheTimes([ValueSource(nameof(MidIndex))] int index)
        {
            (int world, string worldId, int level, FaunaSecondGesture expected) = Mids[index];
            yield return Load(world, worldId, level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            view.DebugReplayIntro(Short, false);
            IntroChoreography plan = view.Intro.Plan;
            TroopLayer troop = view.Troop;
            string where = "nivel " + level;
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch), where);
            Assert.That(troop.SecondChosen, Is.True, where + ": la mitad del mundo lo estrena");
            Assert.That(troop.SecondShown, Is.EqualTo(expected), where);
            RestGesture g = TroopLayer.SecondRest(expected, plan.Model);
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                Assert.That(troop.RestGestureOf(m), Is.EqualTo(g), where + ": toda la tropa (miquito " + m + ")");
            }

            // Los tiempos de siempre: se va antes de T + 2,2 s.
            float gone = view.Intro.TroopGoneMs;
            Assert.That(gone - Short, Is.InRange(1280f, 2200f), where + ": se va hacia T + 2,2 s, como siempre");

            float from = Short;
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                from = Mathf.Min(from, troop.RestFromMs(m));
            }

            float headMax = 0f;
            float swingMin = 0f;
            float swingMax = 0f;
            float lift = 0f;
            float away = 0f;
            float torsoMin = float.MaxValue;
            float torsoMax = float.MinValue;
            var maxY = new float[plan.MonkeyCount];
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                maxY[m] = float.MinValue;
            }

            for (float t = from; t < plan.ExitStartMs; t += 10f)
            {
                view.Intro.Seek(t);
                for (int m = 0; m < plan.MonkeyCount; m++)
                {
                    if (t < troop.RestFromMs(m))
                    {
                        continue;
                    }

                    MonkeyRig rig = troop.Rig(m);
                    string at = where + ", miquito " + m + " a " + t + " ms";
                    Transform head = rig.Root.Find("Head");
                    float h = Mathf.DeltaAngle(0f, head.localEulerAngles.z) * (head.localScale.x < 0f ? -1f : 1f);
                    Assert.That(h, Is.InRange(-0.01f, MonkeyRig.LookUpDeg + 0.01f), "la cabeza nunca baja hacia el tablero: " + at);
                    headMax = Mathf.Max(headMax, h);
                    float swing = Mathf.DeltaAngle(0f, rig.Root.localEulerAngles.z);
                    swingMin = Mathf.Min(swingMin, swing);
                    swingMax = Mathf.Max(swingMax, swing);
                    Vector2 pos = rig.Root.anchoredPosition;
                    maxY[m] = Mathf.Max(maxY[m], pos.y);
                    away = Mathf.Max(away, Mathf.Abs(pos.x - plan.MonkeyX(m, t)));
                    float torso = Mathf.DeltaAngle(0f, rig.Root.Find("Torso").localEulerAngles.z);
                    torsoMin = Mathf.Min(torsoMin, torso);
                    torsoMax = Mathf.Max(torsoMax, torso);
                    Assert.That(pos.x, Is.InRange(0f, view.CanvasWidth), "en la rama, dentro de la pantalla: " + at);
                }
            }

            // La altura de cada uno en su sitio al final del reposo (la intro solo avanza): la referencia de los brincos y del vuelo.
            view.Intro.Seek(plan.ExitStartMs - 1f);
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                lift = Mathf.Max(lift, maxY[m] - troop.Rig(m).Root.anchoredPosition.y);
            }

            float s = plan.BodyDp;
            switch (expected)
            {
                case FaunaSecondGesture.Duet:
                    Assert.That(headMax, Is.GreaterThanOrEqualTo(MonkeyRig.DuetHeadDeg), where + ": el dúo sube la cabeza");
                    break;
                case FaunaSecondGesture.Hops:
                    Assert.That(lift, Is.GreaterThanOrEqualTo(0.25f * s), where + ": los brincos se ven");
                    break;
                case FaunaSecondGesture.TailHang:
                    Assert.That(swingMax, Is.GreaterThanOrEqualTo(14f), where + ": se mece hacia un lado");
                    Assert.That(swingMin, Is.LessThanOrEqualTo(-14f), where + ": y hacia el otro");
                    break;
                case FaunaSecondGesture.Stretch:
                    Assert.That(torsoMax - torsoMin, Is.GreaterThanOrEqualTo(10f), where + ": el desperezo inclina el torso");
                    break;
                case FaunaSecondGesture.Leap:
                    Assert.That(away, Is.GreaterThanOrEqualTo(0.5f * s), where + ": el vuelo se aparta de su sitio");
                    Assert.That(lift, Is.GreaterThanOrEqualTo(0.2f * s), where + ": y sube");
                    break;
            }

            // Sale desde su sitio: en el último instante del reposo, cada uno donde lo deja el plan y en la rama.
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                MonkeyRig rig = troop.Rig(m);
                Assert.That(rig.Root.anchoredPosition.x, Is.EqualTo(plan.MonkeyX(m, plan.ExitStartMs)).Within(0.5f), where + ": de vuelta en su sitio (" + m + ")");
                if (plan.Model != TroopModel.Hang)
                {
                    float branch = -plan.BranchCurveY(rig.Root.anchoredPosition.x);
                    Assert.That(rig.Root.anchoredPosition.y, Is.EqualTo(branch).Within(0.5f), where + ": sentado en la rama (" + m + ")");
                }
            }

            // El nivel siguiente vuelve al primer gesto (la rotación alterna).
            view.Intro.Clear();
            yield return Load(world, worldId, level + 1);
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            view = boot.View!;
            view.DebugReplayIntro(Short, false);
            Assert.That(view.Troop.SecondChosen, Is.False, "nivel " + (level + 1) + ": el primero");
            Assert.That(view.Troop.SecondShown, Is.EqualTo(FaunaSecondGesture.None));
            for (int m = 0; m < view.Intro.Plan.MonkeyCount; m++)
            {
                Assert.That(view.Troop.RestGestureOf(m), Is.Not.EqualTo(g), "nivel " + (level + 1) + ": sin el segundo gesto");
            }
        }

        /// <summary>
        /// Con la franja justa de 3 filas (D-046, punto 3: 56 dp, a 360 × 780 y 360 × 800 dp), el segundo gesto de cada especie
        /// (forzado con <c>af_troop_gesture</c> = 2 por si el nivel no le tocara) nunca sube sobre el HUD, no entra en el panel y
        /// queda a ≥ 8 dp de toda cuia en todos los fotogramas muestreados del reposo: brincos y vuelo se recortan a la
        /// franja; el dúo hincha el pecho, el desperezo baja la cabeza y colgado de la cola se mece ±16°, siempre dentro.
        /// </summary>
        [UnityTest]
        public IEnumerator SecondGesture_InTheTightThreeRowBand_StaysClearOfHudPanelAndCuias([ValueSource(nameof(MidIndex))] int index, [Values(1080, 720)] int widthPx)
        {
            (int world, string worldId, int level, FaunaSecondGesture expected) = Mids[index];
            LaunchOptions.TroopGestureOverride = 2;
            GameScreen.SimulatedScreen = widthPx == 1080 ? IntroSideTroopTests.Screen1080 : IntroSideTroopTests.Screen720;
            string[] ids = { "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo", "agua_tapajos", "lulo", "araza", "cacao", "agua_negro" };
            var sixteen = new int[16][];
            for (int i = 0; i < 16; i++)
            {
                sixteen[i] = i < 14 ? new[] { (i % 14) + 1, ((i + 3) % 14) + 1, ((i + 6) % 14) + 1, ((i + 9) % 14) + 1 } : new int[0];
            }

            yield return PlayModeHelpers.LoadGameWithPack(PlayModeHelpers.BoardPack(world, worldId, "white", level, ids, sixteen), level,
                new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            BoardLayoutResult l = view.Layout!;
            Assert.That(l.Rows, Is.EqualTo(3), "tablero de 3 filas");
            Assert.That(l.TroopBand, Is.True, "la franja de la tropa (D-046)");
            float hudBottom = view.HudBottomDp;
            var rects = new List<Rect>();
            int moved = 0;
            foreach (int duration in new[] { Short, 1400 })
            {
                view.DebugReplayIntro(duration, false);
                IntroChoreography plan = view.Intro.Plan;
                string what = "nivel " + level + ", " + widthPx + " px, T " + duration;
                Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch), what);
                FaunaSecondGesture shown = view.Troop.SecondShown;
                Assert.That(shown == expected || shown == FaunaSecondGesture.None, Is.True, what + ": su gesto o, si no cabe, el de siempre (" + shown + ")");
                moved += shown == expected ? 1 : 0;
                float panelTop = view.Intro.Input.PanelTop;
                // El reposo, donde va el gesto (la entrada y la salida no cambian con F13: las cubren IntroThreeRowBranchTests).
                float start = plan.ExitStartMs;
                for (int m = 0; m < plan.MonkeyCount; m++)
                {
                    start = Mathf.Min(start, view.Troop.RestFromMs(m));
                }

                for (int t = Mathf.CeilToInt(start); t <= plan.ExitStartMs; t += 10)
                {
                    view.Intro.Seek(t);
                    rects.Clear();
                    view.Troop.CollectRects(rects);
                    foreach (Rect r in rects)
                    {
                        if (r.xMax < 0f || r.xMin > view.CanvasWidth)
                        {
                            continue; // en el follaje, fuera de la pantalla
                        }

                        string where = what + " (" + shown + "), t " + t + ", " + r;
                        Assert.That(r.yMin, Is.GreaterThanOrEqualTo(hudBottom - 0.5f), "nada sobre el HUD: " + where);
                        Assert.That(r.yMax, Is.LessThanOrEqualTo(panelTop + 0.5f), "nada dentro del panel: " + where);
                        for (int i = 0; i < view.Views.Length; i++)
                        {
                            Assert.That(IntroSideTroopTests.Distance(r, IntroSideTroopTests.CuiaRect(view.Views[i])), Is.GreaterThanOrEqualTo(IntroChoreography.CuiaClearanceDp),
                                "≥ 8 dp de la cuia " + i + ": " + where);
                        }
                    }
                }

                Debug.Log("AF_F13_BAND " + what + ": " + plan.MonkeyCount + " miquitos, modelo " + plan.Model + ", gesto " + shown);
            }

            if (expected != FaunaSecondGesture.TailHang)
            {
                Assert.That(moved, Is.GreaterThan(0), "nivel " + level + ": el gesto sale también con la franja justa");
            }
        }

        /// <summary>
        /// El extra de desarrollo <c>af_troop_gesture</c>: 2 fuerza el segundo gesto antes de la mitad (el 202 del tocón) y 1 lo
        /// quita en la mitad (el 251); antes de la mitad, sin extra, el primero.
        /// </summary>
        [UnityTest]
        public IEnumerator DevExtra_ForcesOrRemovesTheSecondGesture()
        {
            yield return Load(6, "w06_oriente", 202);
            GameScreen view = GameBootstrap.Current!.View!;
            view.DebugReplayIntro(Short, false);
            Assert.That(view.Troop.SecondShown, Is.EqualTo(FaunaSecondGesture.None), "el 202, antes de la mitad: el primero");
            LaunchOptions.TroopGestureOverride = 2;
            view.DebugReplayIntro(Short, false);
            Assert.That(view.Troop.SecondShown, Is.EqualTo(FaunaSecondGesture.Duet), "forzado: el dúo en el 202");
            LaunchOptions.TroopGestureOverride = 1;
            view.DebugReplayIntro(Short, false);
            Assert.That(view.Troop.SecondShown, Is.EqualTo(FaunaSecondGesture.None), "forzado el primero");

            yield return Load(6, "w06_oriente", 251);
            view = GameBootstrap.Current!.View!;
            view.DebugReplayIntro(Short, false);
            Assert.That(view.Troop.SecondShown, Is.EqualTo(FaunaSecondGesture.None), "el 251 con el primero forzado");
            LaunchOptions.TroopGestureOverride = 0;
            view.DebugReplayIntro(Short, false);
            Assert.That(view.Troop.SecondShown, Is.EqualTo(FaunaSecondGesture.Duet), "0: la regla");
        }

        /// <summary>
        /// Reducir movimiento: un solo miquito, quieto (ninguna pieza cambia entre dos instantes) y, con dúo, desperezo o colgado de
        /// la cola, en la pose del gesto; brincos y vuelo se quedan sentados.
        /// </summary>
        [UnityTest]
        public IEnumerator ReduceMotion_OneStillMonkey_InTheGesturePose([ValueSource(nameof(MidIndex))] int index)
        {
            (int world, string worldId, int level, FaunaSecondGesture expected) = Mids[index];
            yield return Load(world, worldId, level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.Flow!.ReduceMotion = true;
            GameScreen view = boot.View!;
            view.DebugReplayIntro(Short, false);
            Assert.That(view.Troop.SecondChosen, Is.True);
            view.Intro.Seek(100f);
            int visible = -1;
            int count = 0;
            for (int m = 0; m < IntroChoreography.MaxMonkeys; m++)
            {
                if (view.Troop.Rig(m).Visible)
                {
                    visible = m;
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(1), "nivel " + level + ": un solo miquito");
            MonkeyRig rig = view.Troop.Rig(visible);
            Vector2 pos = rig.Root.anchoredPosition;
            Quaternion head = rig.Root.Find("Head").localRotation;
            Vector2 hand = rig.Root.Find("FrontArm") != null ? (Vector2)rig.Root.Find("FrontArm").localPosition : Vector2.zero;
            view.Intro.Seek(150f);
            Assert.That(rig.Root.anchoredPosition, Is.EqualTo(pos), "quieto");
            Assert.That(rig.Root.Find("Head").localRotation, Is.EqualTo(head), "quieto");
            if (rig.Root.Find("FrontArm") != null)
            {
                Assert.That((Vector2)rig.Root.Find("FrontArm").localPosition, Is.EqualTo(hand), "quieto");
            }

            if (expected == FaunaSecondGesture.Duet)
            {
                Assert.That(Mathf.DeltaAngle(0f, rig.Root.Find("Head").localEulerAngles.z), Is.GreaterThanOrEqualTo(MonkeyRig.DuetHeadDeg),
                    "la pose quieta del dúo: la cabeza arriba");
            }
        }

        /// <summary>A10 con el segundo gesto: cada fotograma del reposo y de la salida asigna 0 B, tras una pasada de calentamiento.</summary>
        [UnityTest]
        public IEnumerator SecondGesture_AllocatesNothingPerFrame([ValueSource(nameof(MidIndex))] int index)
        {
            (int world, string worldId, int level, FaunaSecondGesture expected) = Mids[index];
            yield return Load(world, worldId, level);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            flow.Load(level);
            PlayModeHelpers.TickExact(boot, flow.IntroDurationMs + 40);
            for (int i = 0; i < 400 && view.Intro.Active; i++)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 10 * PlayModeHelpers.FrameMs);
            flow.Load(level);
            Assert.That(view.Troop.SecondShown, Is.EqualTo(expected));
            PlayModeHelpers.TickExact(boot, flow.IntroDurationMs + 40);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "el toque en T, como siempre");
            IntroAnimation intro = view.Intro;
            TestDelegate frame = () => intro.Tick(PlayModeHelpers.FrameMs);
            int frames = 0;
            while (intro.TroopVisible && frames < 400)
            {
                Assert.That(frame, Is.Not.AllocatingGCMemory(), "nivel " + level + ": fotograma " + frames + " (t = " + intro.TimeMs + " ms)");
                flow.Tick(PlayModeHelpers.FrameMs, view.IsViewIdle);
                frames++;
            }

            Assert.That(frames * PlayModeHelpers.FrameMs, Is.GreaterThanOrEqualTo(1200), "el reposo y la salida, medidos");
            Assert.That(intro.TroopVisible, Is.False, "la tropa se fue");
        }
    }
}
