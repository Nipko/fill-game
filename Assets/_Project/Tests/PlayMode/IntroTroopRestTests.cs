#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Core.Board;
using AF.GameFlow;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Miquitos con más presencia sin frenar el juego (D-011, punto 3), en la escena real con el flujo: la tropa entra trepando
    /// desde el follaje mientras el río llena, el toque se habilita en T como siempre, reposa en la rama ya en juego (se ve
    /// ≥ 1.200 ms después de T) con un gesto de animal según la especie y el nivel, y se va sola hacia T + 2,2 s; saltar la
    /// intro la funde en 100 ms, como antes; cargar otro nivel, volver a la orilla o volver de segundo plano la retiran en el
    /// mismo fotograma; 0 B por fotograma en el reposo con
    /// el juego en marcha; y la cuia que vierte pasa por delante de la tropa (D-006).
    /// </summary>
    public sealed class IntroTroopRestTests
    {
        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        /// <summary>Nivel 2 (larga, mono ardilla) y nivel 21 (corta, sagui plateado): entrada, toque en T, reposo y salida.</summary>
        [UnityTest]
        public IEnumerator Troop_EntersClimbing_TouchAtT_RestsInPlay_AndLeavesOnItsOwn([Values(2, 21)] int level)
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            flow.Load(level); // desde t = 0
            int total = flow.IntroDurationMs;
            IntroChoreography plan = view.Intro.Plan;
            Assert.That(total, Is.EqualTo(level == 2 ? 1400 : 800), "T no cambia");
            Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch), "primer intento: la tropa");
            Assert.That(plan.IsLong, Is.EqualTo(level == 2));
            Assert.That(view.Troop.Active, Is.True);

            // En t = 0, toda la tropa espera en el follaje de su esquina, fuera de la pantalla.
            float w = view.CanvasWidth;
            float enterEnd = 0f;
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                float x = view.Troop.Rig(m).Root.anchoredPosition.x;
                Assert.That(x < -0.5f * plan.BodyDp || x > w + (0.5f * plan.BodyDp), Is.True, "miquito " + m + " en el follaje en t = 0 (x " + x + ")");
                Assert.That(plan.MonkeyEnterEndMs[m] - plan.MonkeyEnterStartMs[m], Is.GreaterThanOrEqualTo(level == 2 ? 450f : IntroChoreography.MinShortEnterMs), "entra trepando");
                enterEnd = Mathf.Max(enterEnd, plan.MonkeyEnterEndMs[m]);
            }

            // Ya en su sitio, en la rama y a la vista, antes de soltar.
            PlayModeHelpers.TickExact(boot, Mathf.CeilToInt(enterEnd) + 1);
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                float x = view.Troop.Rig(m).Root.anchoredPosition.x;
                Assert.That(x, Is.InRange(0f, w), "miquito " + m + " en la rama a " + view.Intro.TimeMs + " ms");
                Assert.That(x, Is.EqualTo(plan.MonkeySeatX(m)).Within(0.5f));
            }

            // El toque se habilita en T, como antes.
            PlayModeHelpers.TickExact(boot, total - 1 - Mathf.RoundToInt(view.Intro.TimeMs));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro), "aún en la intro a T − 1");
            PlayModeHelpers.TickExact(boot, 1);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "Playing en T");
            Assert.That(boot.TapCuia(FirstNonEmpty(flow.Session.Current)).Kind, Is.EqualTo(TapKind.Selected), "el toque de juego funciona en T");
            boot.TapBackground();

            // Reposa en la rama, ya en juego: a la vista 1.200 ms después de T, opaca y quieta.
            Assert.That(view.Intro.TroopVisible, Is.True);
            float gone = view.Intro.TroopGoneMs;
            Assert.That(gone - total, Is.InRange(1280f, 2200f), "se va hacia T + 2,2 s");
            PlayModeHelpers.TickExact(boot, 1200);
            Assert.That(view.Troop.Active, Is.True, "a la vista 1.200 ms después de T");
            Assert.That(view.Intro.TroopVisible, Is.True);
            for (int m = 0; m < plan.MonkeyCount; m++)
            {
                MonkeyRig rig = view.Troop.Rig(m);
                Assert.That(rig.Visible, Is.True);
                Assert.That(rig.Alpha, Is.EqualTo(1f).Within(0.001f), "opaco en el reposo");
                Assert.That(rig.Root.anchoredPosition.x, Is.EqualTo(plan.MonkeyX(m, plan.ExitStartMs)).Within(0.5f), "quieto en su sitio");
            }

            // Y se va sola, a su hora.
            PlayModeHelpers.TickExact(boot, Mathf.CeilToInt(gone - view.Intro.TimeMs) + 1);
            Assert.That(view.Troop.Active, Is.False, "se ha ido");
            Assert.That(view.Intro.TroopVisible, Is.False);
            Assert.That(view.Intro.Active, Is.False, "no queda nada de la intro");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        /// <summary>
        /// Saltar la intro con un toque mientras la tropa entra trepando (02 §4.2) la retira como antes de D-011: se funde en su
        /// sitio en 100 ms, sin moverse, sin llegar a su asiento ni reposar, y el toque de juego funciona en ese mismo fotograma.
        /// </summary>
        [UnityTest]
        public IEnumerator SkippingDuringTheEntry_RemovesTheTroopAsBefore_WithoutTheRest([Values(2, 21)] int level)
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            flow.Load(level);
            IntroChoreography plan = view.Intro.Plan;
            int mid = Mathf.RoundToInt((plan.MonkeyEnterStartMs[0] + plan.MonkeyEnterEndMs[0]) * 0.5f);
            PlayModeHelpers.TickExact(boot, mid);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(Mathf.Abs(plan.MonkeyVelocity(0, view.Intro.TimeMs)), Is.GreaterThan(0f), "el miquito 0 está entrando");
            Vector2[] before = RigPositions(view, plan.MonkeyCount);

            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "el toque se habilita en ese fotograma");
            Assert.That(view.Intro.TroopVisible, Is.True, "se funde en su sitio");
            PlayModeHelpers.TickExact(boot, 50);
            Assert.That(RigPositions(view, plan.MonkeyCount), Is.EqualTo(before), "sin moverse al fundirse");
            Assert.That(view.Intro.TroopVisible, Is.True);
            PlayModeHelpers.TickExact(boot, 50);
            Assert.That(view.Troop.Active, Is.False, "se ha ido a los 100 ms, sin reposo");
            Assert.That(view.Intro.TroopVisible, Is.False);
            Assert.That(view.Intro.Active, Is.False);
            Assert.That(boot.TapCuia(FirstNonEmpty(flow.Session.Current)).Kind, Is.EqualTo(TapKind.Selected));
        }

        private static Vector2[] RigPositions(GameScreen view, int count)
        {
            var p = new Vector2[count];
            for (int m = 0; m < count; m++)
            {
                p[m] = view.Troop.Rig(m).Root.anchoredPosition;
            }

            return p;
        }

        /// <summary>
        /// Cargar otro nivel, volver a la orilla (reintento: dosel) o volver de segundo plano mientras la tropa reposa la retiran
        /// en ese mismo fotograma (<c>IntroAnimation.Clear</c>; con un reposo de ≈ 2 s es lo habitual).
        /// </summary>
        [UnityTest]
        public IEnumerator LoadingAnotherLevel_Restarting_OrResuming_DuringTheRest_RemovesTheTroopAtOnce()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            yield return PlayModeHelpers.LoadGameWithPack(TwoLevelPack(), 3, new MemoryProgressStore(3, 2));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;

            // Nivel N + 1 sin tropa (16 cuias en 3 filas: dosel).
            RestAt(boot, 3, 600);
            flow.Load(4);
            Assert.That(view.Troop.Active, Is.False, "cargar el nivel siguiente la retira en el mismo fotograma");
            Assert.That(view.Intro.TroopVisible, Is.False);
            Assert.That(view.Intro.Plan.Perch, Is.EqualTo(IntroPerch.Canopy), "el nivel siguiente no tiene tropa");

            // Nivel N + 1 con su tropa: en ese fotograma no queda ningún miquito de la anterior en la rama (la nueva espera fuera).
            LaunchOptions.Reset();
            yield return PlayModeHelpers.LoadGame(2, new MemoryProgressStore(2, 1));
            boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            flow = boot.Flow!;
            view = boot.View!;
            RestAt(boot, 2, 600);
            flow.Load(3);
            Assert.That(view.Intro.TimeMs, Is.EqualTo(0f), "la intro nueva, en t = 0");
            for (int m = 0; m < IntroChoreography.MaxMonkeys; m++)
            {
                MonkeyRig rig = view.Troop.Rig(m);
                if (rig.Visible)
                {
                    float x = rig.Root.anchoredPosition.x;
                    Assert.That(x < 0f || x > view.CanvasWidth, Is.True, "miquito " + m + ": ninguno de la tropa anterior sigue en la rama");
                }
            }

            // Volver a la orilla (reintento, dosel).
            RestAt(boot, 2, 600);
            Assert.That(boot.PressRestart(), Is.True);
            Assert.That(view.Troop.Active, Is.False, "volver a la orilla la retira en el mismo fotograma");

            // Vuelta de segundo plano (02 §14): la vista salta al modelo.
            RestAt(boot, 2, 600);
            view.SnapToModel();
            Assert.That(view.Troop.Active, Is.False, "volver de segundo plano la retira en el mismo fotograma");
            Assert.That(view.Intro.Active, Is.False);
        }

        // Rutas del reposo para el presupuesto de 0 B por fotograma: especie y modelo, larga y corta.
        private static readonly string[] RestCases = { "fila_l2_larga", "cuelga_l21_corta", "abraza_82", "sacude_142", "pareja_202" };

        /// <summary>
        /// A10 en el reposo (D-011, punto 3): ya en juego, con el flujo en marcha, cada fotograma de la tropa en su reposo y en
        /// su salida (<c>IntroAnimation.Tick</c>, que lleva la cola de la intro) asigna 0 B, tras una pasada de calentamiento por
        /// el mismo nivel. Solo se mide la tropa: el resto de <c>GameScreen.Tick</c> no es de esta tarea.
        /// </summary>
        [UnityTest]
        public IEnumerator Rest_AllocatesNothingPerFrame_InPlay([ValueSource(nameof(RestCases))] string route)
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            int level;
            string pack = string.Empty;
            switch (route)
            {
                case "fila_l2_larga":
                    level = 2;
                    break;
                case "cuelga_l21_corta":
                    level = 21;
                    break;
                case "abraza_82":
                    level = 82;
                    pack = PlayModeHelpers.Level7BoardPack(4, "w04_trapecio", "white", 82);
                    break;
                case "sacude_142":
                    level = 142;
                    pack = PlayModeHelpers.Level7BoardPack(5, "w05_loreto", "white", 142);
                    break;
                default:
                    level = 202;
                    pack = PlayModeHelpers.Level7BoardPack(6, "w06_oriente", "white", 202);
                    break;
            }

            if (pack.Length > 0)
            {
                yield return PlayModeHelpers.LoadGameWithPack(pack, level, new MemoryProgressStore(level, level - 1));
            }
            else
            {
                yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            }

            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;

            // Calentamiento: la misma intro entera, con su reposo, su salida y unos fotogramas más (el primer uso de cada pieza, de
            // cada gesto y de lo que oculta la tropa al irse).
            flow.Load(level);
            PlayModeHelpers.TickExact(boot, flow.IntroDurationMs + 40);
            for (int i = 0; i < 400 && view.Intro.Active; i++)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 10 * PlayModeHelpers.FrameMs);
            flow.Load(level);
            Assert.That(view.Intro.Plan.Perch, Is.EqualTo(IntroPerch.Branch), route);
            PlayModeHelpers.TickExact(boot, flow.IntroDurationMs + 40);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), route);
            Assert.That(view.Intro.TroopVisible, Is.True, route);
            bool eats = false;
            for (int m = 0; m < view.Intro.Plan.MonkeyCount; m++)
            {
                eats |= view.Troop.RestGestureOf(m) == RestGesture.EatFruit;
            }

            if (route == "abraza_82")
            {
                Assert.That(eats, Is.True, "el churuco come la fruta en el reposo");
            }

            IntroAnimation intro = view.Intro;
            TestDelegate frame = () => intro.Tick(PlayModeHelpers.FrameMs);
            int frames = 0;
            while (intro.TroopVisible && frames < 400)
            {
                Assert.That(frame, Is.Not.AllocatingGCMemory(), route + ": fotograma " + frames + " del reposo (t = " + intro.TimeMs + " ms)");
                flow.Tick(PlayModeHelpers.FrameMs, view.IsViewIdle); // el juego sigue: la tropa no depende de él
                Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), route);
                frames++;
            }

            Assert.That(frames * PlayModeHelpers.FrameMs, Is.GreaterThanOrEqualTo(1200), route + ": el reposo y la salida, medidos");
            Assert.That(view.Intro.TroopVisible, Is.False, route + ": la tropa se fue");
        }

        /// <summary>
        /// Gestos de reposo en la escena (D-011, punto 3): cada miquito queda quieto en su sitio mientras hace su gesto; el de
        /// <c>cola_balancin</c> (mono ardilla) balancea la cola, el churuco come la fruta (en su mano, cerca de la cabeza), la
        /// pareja sigue con las colas trenzadas y nunca balancea la cola; ninguna cabeza baja hacia el tablero (D-002). Mismo
        /// nivel, mismos gestos.
        /// </summary>
        [UnityTest]
        public IEnumerator RestGestures_InTheScene_FollowTheSpecies_AndTheBodyStaysPut()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            var cases = new (string Pack, int Level, int Duration, RestGesture Signature)[]
            {
                (string.Empty, 2, 1400, RestGesture.SwingTail),
                (PlayModeHelpers.Level7BoardPack(4, "w04_trapecio", "white", 82), 82, 1400, RestGesture.EatFruit),
                (PlayModeHelpers.Level7BoardPack(6, "w06_oriente", "white", 202), 202, 800, RestGesture.None),
                (string.Empty, 21, 800, RestGesture.None),
            };
            foreach ((string pack, int level, int duration, RestGesture signature) in cases)
            {
                LaunchOptions.Reset();
                if (pack.Length > 0)
                {
                    yield return PlayModeHelpers.LoadGameWithPack(pack, level, new MemoryProgressStore(level, level - 1));
                }
                else
                {
                    yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
                }

                GameBootstrap boot = GameBootstrap.Current!;
                PlayModeHelpers.TakeClock(boot);
                GameScreen view = boot.View!;
                view.DebugReplayIntro(duration, false);
                IntroChoreography plan = view.Intro.Plan;
                TroopLayer troop = view.Troop;
                Assert.That(plan.Perch, Is.EqualTo(IntroPerch.Branch), "nivel " + level);
                var gestures = new RestGesture[plan.MonkeyCount];
                bool hasSignature = signature == RestGesture.None;
                for (int m = 0; m < plan.MonkeyCount; m++)
                {
                    gestures[m] = troop.RestGestureOf(m);
                    Assert.That(gestures[m], Is.Not.EqualTo(RestGesture.None), "nivel " + level + ": miquito " + m + " con gesto");
                    hasSignature |= gestures[m] == signature;
                    if (plan.Pair || plan.Model == TroopModel.Hang)
                    {
                        Assert.That(gestures[m], Is.Not.EqualTo(RestGesture.SwingTail), "nivel " + level + ": sin balancear la cola");
                    }
                }

                Assert.That(hasSignature, Is.True, "nivel " + level + ": el gesto de la especie (" + signature + ")");

                // Desde que todos reposan hasta antes de la salida: cuerpo quieto, cabeza nunca hacia abajo, y el gesto se nota.
                float from = duration;
                for (int m = 0; m < plan.MonkeyCount; m++)
                {
                    from = Mathf.Max(from, troop.RestFromMs(m) + TroopLayer.RestBlendMs);
                }

                var root = new Vector2[plan.MonkeyCount];
                var tipMin = new float[plan.MonkeyCount];
                var tipMax = new float[plan.MonkeyCount];
                bool held = false;
                for (float t = from; t < plan.ExitStartMs - TroopLayer.RestBlendMs; t += 50f)
                {
                    view.Intro.Seek(t);
                    for (int m = 0; m < plan.MonkeyCount; m++)
                    {
                        MonkeyRig rig = troop.Rig(m);
                        string where = "nivel " + level + ", miquito " + m + " (" + gestures[m] + ") a " + t + " ms";
                        if (t == from)
                        {
                            root[m] = rig.Root.anchoredPosition;
                            tipMin[m] = float.MaxValue;
                            tipMax[m] = float.MinValue;
                        }

                        if (gestures[m] != RestGesture.SwingBody)
                        {
                            Assert.That(rig.Root.anchoredPosition, Is.EqualTo(root[m]), "quieto en su sitio: " + where);
                        }

                        float head = Mathf.DeltaAngle(0f, rig.Root.Find("Head").localEulerAngles.z);
                        Assert.That(head, Is.InRange(-0.01f, 15f), "la cabeza nunca baja hacia el tablero: " + where);
                        float tip = rig.Tail.PointAt(1f).x + rig.Tail.PointAt(1f).y;
                        tipMin[m] = Mathf.Min(tipMin[m], tip);
                        tipMax[m] = Mathf.Max(tipMax[m], tip);
                        Image fruit = rig.Root.Find("HeldFruit").GetComponent<Image>();
                        if (gestures[m] == RestGesture.EatFruit)
                        {
                            held |= fruit.enabled && fruit.rectTransform.sizeDelta.x > 1f;
                        }
                        else
                        {
                            Assert.That(fruit.enabled, Is.False, "solo el que come lleva fruta en el reposo: " + where);
                        }

                        if (plan.Pair && m < 2)
                        {
                            Assert.That(rig.Tail.Waving, Is.True, "la pareja sigue con las colas trenzadas: " + where);
                        }
                    }
                }

                for (int m = 0; m < plan.MonkeyCount; m++)
                {
                    if (gestures[m] == RestGesture.SwingTail)
                    {
                        Assert.That(tipMax[m] - tipMin[m], Is.GreaterThan(4f), "nivel " + level + ": el miquito " + m + " balancea la cola");
                    }
                }

                bool anyEats = System.Array.IndexOf(gestures, RestGesture.EatFruit) >= 0;
                Assert.That(held, Is.EqualTo(anyEats), "nivel " + level + ": quien come lleva la fruta en la mano");
            }
        }

        /// <summary>
        /// D-006 en el reposo: la tropa se dibuja por detrás del tablero, así que la cuia elevada o que vierte (también la de la
        /// fila 1, en la capa LiftFront) pasa por delante de la tropa y de la rama; y nada de la tropa recibe toques.
        /// </summary>
        [UnityTest]
        public IEnumerator PouringCuia_PassesInFrontOfTheResting_Troop()
        {
            yield return PlayModeHelpers.LoadGame(2, new MemoryProgressStore(2, 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            RestAt(boot, 2, 300);
            Dictionary<Transform, int> order = RenderOrder(view.Canvas.transform);
            int troop = order[view.Troop.Layer];
            Assert.That(troop, Is.LessThan(order[view.Views[0].Root]), "la cuia, por delante de la tropa");
            Assert.That(troop, Is.LessThan(order[view.LiftFront]), "la cuia elevada, por delante de la tropa");
            var graphics = new List<Graphic>();
            view.Troop.CollectGraphics(graphics);
            foreach (Graphic g in graphics)
            {
                Assert.That(g.raycastTarget, Is.False, g.name);
            }
        }

        // Carga el nivel desde t = 0 y avanza hasta T + afterMs (la tropa, en el reposo).
        private static void RestAt(GameBootstrap boot, int level, int afterMs)
        {
            boot.Flow!.Load(level);
            int total = boot.Flow.IntroDurationMs;
            PlayModeHelpers.TickExact(boot, total + afterMs);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.View!.Troop.Active, Is.True, "la tropa reposa a T + " + afterMs + " ms");
            Assert.That(boot.View.Intro.TimeMs, Is.LessThan(boot.View.Intro.Plan.ExitStartMs), "aún no ha empezado a salir");
        }

        // Paquete del mundo 1 con dos niveles: el 3 con el tablero del nivel 7 de PlayModeHelpers.Level7BoardPack (tropa del mono
        // ardilla) y el 4 con el de 16 cuias de SixteenCuiaPack, en 3 filas (sin franja para la rama: dosel).
        private static string TwoLevelPack()
        {
            string[] seven = { "acai", "aguaje", "tapereba", "guanabana", "bacaba" };
            int[][] sevenBoard = { new[] { 8, 8, 7, 7 }, new[] { 3, 4, 4, 7 }, new[] { 1, 1, 8, 3 }, new[] { 3, 7, 8, 4 }, new[] { 4, 1, 1, 3 }, new int[0], new int[0] };
            string[] ids = { "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo", "agua_tapajos", "lulo", "araza", "cacao", "agua_negro" };
            var sixteen = new int[16][];
            for (int i = 0; i < 16; i++)
            {
                sixteen[i] = i < 14 ? new[] { (i % 14) + 1, ((i + 3) % 14) + 1, ((i + 6) % 14) + 1, ((i + 9) % 14) + 1 } : new int[0];
            }

            return "{ \"subset_of\": \"af-level/1\", \"pack_id\": \"test_two_levels\", \"kind\": \"campaign\", \"world\": 1, \"world_id\": \"w01_estuario\", "
                + "\"lap\": 1, \"water_base\": \"white\", \"levels\": [ " + LevelJson(3, seven, sevenBoard) + ", " + LevelJson(4, ids, sixteen) + " ] }";
        }

        private static string LevelJson(int level, string[] palette, int[][] board)
        {
            var cuias = new System.Text.StringBuilder();
            var caps = new System.Text.StringBuilder();
            var attrs = new System.Text.StringBuilder();
            for (int i = 0; i < board.Length; i++)
            {
                string sep = i == 0 ? string.Empty : ", ";
                caps.Append(sep).Append('4');
                attrs.Append(sep).Append("null");
                cuias.Append(sep).Append('[');
                for (int k = 0; k < board[i].Length; k++)
                {
                    cuias.Append(k == 0 ? string.Empty : ", ").Append(board[i][k]);
                }

                cuias.Append(']');
            }

            string id = "m01_v1_n" + level.ToString("0000", System.Globalization.CultureInfo.InvariantCulture);
            return "{ \"id\": \"" + id + "\", \"level\": " + level + ", \"world\": 1, \"lap\": 1, \"water_base\": \"white\", \"palette\": [\""
                + string.Join("\", \"", palette) + "\"], \"units_per_color\": 4, \"caps\": [" + caps + "], \"cuias\": [" + cuias + "], \"attrs\": ["
                + attrs + "], \"murky\": false, \"hidden\": [], \"difficulty\": { \"par\": 1 }, \"relief\": [] }";
        }

        private static int FirstNonEmpty(BoardState board)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (!board[i].IsEmpty)
                {
                    return i;
                }
            }

            return 0;
        }

        // Orden de dibujo de cada Transform del lienzo (recorrido en profundidad de la jerarquía).
        private static Dictionary<Transform, int> RenderOrder(Transform root)
        {
            var order = new Dictionary<Transform, int>();
            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Transform t = stack.Pop();
                order[t] = order.Count;
                for (int i = t.childCount - 1; i >= 0; i--)
                {
                    stack.Push(t.GetChild(i));
                }
            }

            return order;
        }
    }
}
