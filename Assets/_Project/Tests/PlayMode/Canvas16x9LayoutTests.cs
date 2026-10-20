#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Prueba de layout en 16:9 de la tarea DC3 (pregunta 19 (a) de <c>docs/GDD.md</c> §6.3; D-028; 02 §5 y §12): el lienzo del
    /// juego mide 360 dp de ancho y, si así quedaran menos de 640 dp de alto, manda el alto (<see cref="GameScreen.CanvasScale"/>),
    /// así que en 16:9 mide 360 × 640 dp y 1 sp de lienzo es 1 sp físico. En la escena:
    /// <list type="bullet">
    /// <item><description>a 1080 × 1920 y 720 × 1280 px, <c>scaleFactor</c> = ancho / 360 y el lienzo mide 360 × 640 dp;
    /// a 1080 × 2340 px, 360 × 780 como antes;</description></item>
    /// <item><description>ningún texto a la vista baja de 12 sp físicos (tamaño × escala del lienzo / (ancho / 360 px por dp))
    /// en el nivel, la pausa, Aguas estancadas y la victoria con TiTino (hito 40);</description></item>
    /// <item><description>el HUD, el tablero y la bandeja no se solapan ni salen de la zona segura en el nivel 1, uno de 3 filas
    /// (el 374, 13 cuias), uno de 14 cuias con la Cuia de rescate y la Cuia extra (14 + 2) y el 323 (Tinaja).</description></item>
    /// </list>
    /// Las capturas, en <see cref="Canvas16x9SnapshotTests"/>.
    /// </summary>
    public sealed class Canvas16x9LayoutTests
    {
        private const string NewLine = "\n";

        /// <summary>16:9 a 2 px/dp (720 × 1280 px) con la zona segura de <see cref="HudLabelTests.Screen640"/> a esa densidad.</summary>
        internal static readonly Vector4 Screen640At2x = new Vector4(720f, 1280f, 64f, 32f);

        /// <summary>18:9 a 3 px/dp (1080 × 2160 px; 360 × 720 dp) con la zona segura de <see cref="HudLabelTests.Screen640"/>.</summary>
        internal static readonly Vector4 Screen720 = new Vector4(1080f, 2160f, 96f, 48f);

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            SlothVictoryTests.NoSlot(false);
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        // ------------------------------------------------------------------------------------------- la escala

        [UnityTest]
        public IEnumerator ScaleFactor_IsWidthOver360_AndTheCanvasIs360x640_In16x9([Values(1920, 1280, 2340)] int heightPx)
        {
            Vector4 screen = heightPx == 1920 ? HudLabelTests.Screen640 : (heightPx == 1280 ? Screen640At2x : HudLabelTests.Screen780);
            GameScreen.SimulatedScreen = screen;
            yield return LoadPlaying(1);
            GameScreen view = GameBootstrap.Current!.View!;
            float expected = screen.x / GameScreen.ReferenceWidth;
            Assert.That(view.Canvas.GetComponent<CanvasScaler>().scaleFactor, Is.EqualTo(expected).Within(1e-4f), "ancho / 360");
            Assert.That(view.CanvasWidth, Is.EqualTo(360f).Within(0.01f));
            Assert.That(view.CardCanvasHeight, Is.EqualTo(heightPx == 2340 ? 780f : 640f).Within(0.01f), "alto del lienzo");
            Assert.That(view.SafeTopDp, Is.EqualTo(32f).Within(0.01f), "zona segura de arriba (96 px a 3 px/dp)");
            Assert.That(view.CardSafeBottom, Is.EqualTo(GameScreen.MinSafeBottom).Within(0.01f));
        }

        // ------------------------------------------------------------------------------------------- texto ≥ 12 sp físicos

        [UnityTest]
        public IEnumerator Texts_AtLeast12PhysicalSp_Level_Pause_Stuck([Values(1920, 1280)] int heightPx)
        {
            Vector4 screen = Use16x9(heightPx);
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0, 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            string at = " a " + screen.x + " × " + screen.y + " px";
            AssertTexts(view, (int)screen.x, "el nivel 31" + at, 5);
            AssertLayout(view, "el nivel 31" + at);

            // La pausa.
            view.PauseButton.onClick.Invoke();
            Assert.That(view.Pause.Visible, Is.True);
            PlayModeHelpers.Step(boot, 600);
            AssertTexts(view, (int)screen.x, "la pausa" + at, 4);
            AssertInsideSafeArea(view, (RectTransform)view.Pause.Root.Find("Panel"), "la pausa" + at);
            view.Pause.ResumeButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 600);
            Assert.That(view.Pause.Visible, Is.False);

            // Aguas estancadas (rama B: sin semillas, con la última canoa).
            FunnelSetup.GetStuck(boot);
            PlayModeHelpers.Step(boot, 800);
            Assert.That(view.StuckVisible, Is.True);
            AssertTexts(view, (int)screen.x, "Aguas estancadas" + at, 4);
            AssertInsideSafeArea(view, view.Stuck.Panel, "Aguas estancadas" + at);

            // Puñado y Balsa (revisión de DC3; 02 §12: las cajas crecen): botones de 56 dp o más, con 6 dp de aire sobre y bajo la
            // tinta de su etiqueta.
            StuckModal stuck = view.Stuck;
            Assert.That(stuck.HandfulButton.gameObject.activeInHierarchy && stuck.BalsaButton.gameObject.activeInHierarchy, Is.True, "Puñado y Balsa en la rama B");
            foreach ((string name, Button button, TextMeshProUGUI label) in new[] { ("Puñado", stuck.HandfulButton, stuck.HandfulLabel), ("Balsa", stuck.BalsaButton, stuck.BalsaLabel) })
            {
                float h = ((RectTransform)button.transform).rect.height;
                TestContext.WriteLine("AF_DC3 " + name + at + ": botón de " + h.ToString("0.0") + " dp, cara de " + stuck.OfferFaceDp.ToString("0.0") + " dp");
                Assert.That(h, Is.GreaterThanOrEqualTo(StuckModal.OfferMinHeightDp - 0.01f), name + at + ": el botón mide 56 dp o más");
                LongLabelSetup.AssertOfferAir(label, stuck.OfferFaceDp, 1f, name + at);
            }
        }

        [UnityTest]
        public IEnumerator Texts_AtLeast12PhysicalSp_VictoryWithTheSloth_Hito40([Values(1920, 1280)] int heightPx, [Values(true, false)] bool slot)
        {
            Vector4 screen = Use16x9(heightPx);
            SlothVictoryTests.NoSlot(!slot);
            yield return VictoryArrivalTests.Win(40);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 2500); // la cinta, la ranura, TiTino y Siguiente a la vista
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen));

            // Sin la ranura del Dato del río, TiTino sale en su hito con la vista (DC2). Con ella, en 16:9 la franja mide 44 dp,
            // menos que los 56 del gesto, y no sale (open issue de DC3; SlothVictoryBandTests).
            Assert.That(view.SlothView.Visible, Is.EqualTo(!slot), "TiTino en su hito, con la vista (DC2)");
            string at = "la victoria del 40 " + (slot ? "con" : "sin") + " ranura a " + screen.x + " × " + screen.y + " px";
            AssertTexts(view, (int)screen.x, at, 3);
            Rect panel = VictorySetup.CanvasRect(view.VictoryPanelRect, Root(view));
            AssertInside(view, panel, at + ": el panel de la victoria");
        }

        // ------------------------------------------------------------------------------------------- HUD, tablero y bandeja

        [UnityTest]
        public IEnumerator Layout_Level1_NoOverlaps_InsideTheSafeArea()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return LoadPlaying(1);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(1));
            AssertLayout(view, "el nivel 1 a 360 × 640");
            AssertTexts(view, 1080, "el nivel 1 a 360 × 640", 3);
        }

        [UnityTest]
        public IEnumerator Layout_ThreeRows_Level374_NoOverlaps_InsideTheSafeArea()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return LoadPlaying(374);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.Views.Length, Is.EqualTo(13));
            Assert.That(view.Layout!.Rows, Is.EqualTo(3), "13 generadas: 3 filas (P12 de D-006, c)");
            AssertLayout(view, "el 374 (13 cuias, 3 filas) a 360 × 640");
            AssertTexts(view, 1080, "el 374 a 360 × 640", 3);
        }

        /// <summary>
        /// Los altos intermedios (revisión de DC3): el cambio de escala toca toda pantalla más ancha que 360:780, no solo 16:9. En
        /// 18:9 (1080 × 2160 px) el lienzo pasa de 390 × 780 a 360 × 720 dp; el nivel 1 y el 374 (3 filas) sin solapes, dentro de
        /// la zona segura y con el texto a 12 sp físicos o más.
        /// </summary>
        [UnityTest]
        public IEnumerator Layout_18x9_360x720_Level1AndThreeRows([Values(1, 374)] int level)
        {
            GameScreen.SimulatedScreen = Screen720;
            yield return LoadPlaying(level);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.Canvas.GetComponent<CanvasScaler>().scaleFactor, Is.EqualTo(3f).Within(1e-4f), "ancho / 360");
            Assert.That(view.CanvasWidth, Is.EqualTo(360f).Within(0.01f));
            Assert.That(view.CardCanvasHeight, Is.EqualTo(720f).Within(0.01f), "18:9: 360 × 720 dp");
            Assert.That(view.Layout!.Rows, Is.EqualTo(level == 1 ? 1 : 3));
            string at = "el nivel " + level + " a 360 × 720 (18:9)";
            AssertLayout(view, at);
            AssertTexts(view, 1080, at, 3);
        }

        [UnityTest]
        public IEnumerator Layout_FourteenPlusTwo_TheBoardFits()
        {
            GameBootstrap? boot = null;
            Pour[]? path = null;
            foreach (int candidate in Dc18SnapshotTests.FourteenCandidates)
            {
                LaunchOptions.Reset();
                FunnelSetup.Hooks();
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(5000));
                GameScreen.SimulatedScreen = HudLabelTests.Screen640;
                yield return LoadPlaying(candidate);
                boot = GameBootstrap.Current!;
                path = Dc18SnapshotTests.FindRescuableStuck(boot.Flow!);
                if (path != null)
                {
                    break;
                }
            }

            Assert.That(path, Is.Not.Null, "algún nivel de 14 cuias llega a Aguas estancadas con el rescate a la venta");
            GameScreen view = boot!.View!;
            Assert.That(view.Views.Length, Is.EqualTo(14));
            AssertLayout(view, "14 cuias a 360 × 640");

            LevelFlow flow = boot.Flow!;
            MechanicTestKit.PlayPrefix(boot, path!, false);
            for (int ms = 0; ms < 8000 && flow.Phase != FlowPhase.StuckModal; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            PlayModeHelpers.Step(boot, 300);
            AssertTexts(view, 1080, "Aguas estancadas con 14 cuias a 360 × 640", 4);
            AssertInsideSafeArea(view, view.Stuck.Panel, "Aguas estancadas con 14 cuias a 360 × 640");
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "el rescate aplicado");
            PlayModeHelpers.Step(boot, 700);
            Assert.That(boot.PressExtraCuia(), Is.True, "Cuia extra con semillas");
            PlayModeHelpers.Step(boot, 700); // la recolocación (600 ms)
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(view.Views.Length, Is.EqualTo(16), "14 + 2");
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            AssertLayout(view, "14 + 2 cuias a 360 × 640");
        }

        [UnityTest]
        public IEnumerator Layout_Tinaja323_TheBoardFits()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return DebutFingerTests.Load323();
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(BoardLayout.TinajaPairStart(boot.Flow.Session.Current), Is.GreaterThanOrEqualTo(0), "condición del test: la Tinaja");
            AssertLayout(view, "el 323 (Tinaja) a 360 × 640");
            AssertTexts(view, 1080, "el 323 a 360 × 640", 3);
        }

        // ------------------------------------------------------------------------------------------- utilidades

        private static Vector4 Use16x9(int heightPx)
        {
            Vector4 screen = heightPx == 1280 ? Screen640At2x : HudLabelTests.Screen640;
            GameScreen.SimulatedScreen = screen;
            return screen;
        }

        // Un nivel de la escena (sin tarjeta), con la intro saltada y la vista quieta.
        internal static IEnumerator LoadPlaying(int level)
        {
            if (LaunchOptions.ProfileStoreOverride == null)
            {
                LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            }

            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 6000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), "nivel " + level);
            PlayModeHelpers.Step(boot, 400);
        }

        /// <summary>
        /// Todos los textos a la vista del lienzo del juego (activos, con texto, opacos y dentro de la pantalla): su tamaño en sp
        /// físicos (tamaño × su escala real en pantalla, <c>lossyScale</c>, / (ancho / 360), los px por dp de un teléfono de 360 dp)
        /// no baja de 12 (02 §12).
        /// </summary>
        internal static void AssertTexts(GameScreen view, int widthPx, string where, int atLeast)
        {
            Canvas.ForceUpdateCanvases();
            float pxPerDp = widthPx / GameScreen.ReferenceWidth;
            RectTransform root = Root(view);
            var issues = new List<string>();
            int seen = 0;
            foreach (TextMeshProUGUI t in root.GetComponentsInChildren<TextMeshProUGUI>(false))
            {
                if (!t.enabled || string.IsNullOrWhiteSpace(t.text) || t.color.a < 0.01f || GroupAlpha(t.transform) < 0.01f)
                {
                    continue;
                }

                Rect r = VictorySetup.CanvasRect(t.rectTransform, root);
                if (r.xMax <= 0f || r.xMin >= view.CanvasWidth || r.yMax <= 0f || r.yMin >= view.CardCanvasHeight)
                {
                    continue; // fuera de la pantalla
                }

                t.ForceMeshUpdate(true, true);
                if (t.textInfo.characterCount == 0)
                {
                    continue;
                }

                seen++;
                // Con la escala real del texto: lossyScale incluye el scaleFactor del lienzo y la de los padres (la columna
                // compactada de la cabecera, WalletBar.SetScale; el precio de la bandeja, BoosterTray.PlacePrice; los pops).
                float physical = t.fontSize * Mathf.Abs(t.rectTransform.lossyScale.y) / pxPerDp;
                if (physical < UiBuilder.MinTextSp - 0.01f)
                {
                    issues.Add(where + ": «" + t.text.Replace(NewLine, " / ") + "» (" + PathOf(t.transform) + ") a " + physical.ToString("0.00") + " sp físicos");
                }
            }

            Assert.That(seen, Is.GreaterThanOrEqualTo(atLeast), where + ": condición del test, textos a la vista");
            Assert.That(issues, Is.Empty, string.Join(NewLine, issues));
        }

        /// <summary>
        /// HUD (pausa, Reiniciar, insignia del nivel), tablero (el panel y cada cuia) y bandeja (sus botones a la vista): dentro de
        /// la pantalla y de su zona segura, el HUD encima del panel, la bandeja debajo y sin solapes entre ellos (02 §5).
        /// </summary>
        internal static void AssertLayout(GameScreen view, string where)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform root = Root(view);
            var hud = new List<(string, Rect)>
            {
                ("pausa", VictorySetup.CanvasRect((RectTransform)view.PauseButton.transform, root)),
                ("Reiniciar", VictorySetup.CanvasRect((RectTransform)view.RestartButton.transform, root)),
                ("Reiniciar (píldora)", VictorySetup.CanvasRect(view.RestartPillRect, root)),
            };
            if (view.LevelBadgeRect.gameObject.activeInHierarchy)
            {
                hud.Add(("insignia del nivel", VictorySetup.CanvasRect(view.LevelBadgeRect, root)));
            }

            var tray = new List<(string, Rect)>();
            AddIfShown(tray, "Deshacer", view.Tray.UndoButton, root);
            AddIfShown(tray, "Cuia extra", view.Tray.ExtraCuiaButton, root);
            AddIfShown(tray, "Revelar", view.Tray.RevealButton, root);
            AddIfShown(tray, "Pista", view.Tray.HintButton, root);
            Assert.That(tray.Count, Is.GreaterThan(0), where + ": condición del test, la bandeja");

            foreach ((string name, Rect r) in hud)
            {
                AssertInside(view, r, where + ": " + name);
            }

            foreach ((string name, Rect r) in tray)
            {
                AssertInside(view, r, where + ": " + name);
            }

            BoardLayoutResult layout = view.Layout!;
            var panel = new Rect(layout.PanelX, layout.PanelY, layout.PanelWidth, layout.PanelHeight);
            AssertInside(view, panel, where + ": el panel del tablero");
            float half = 0.5f * layout.CuiaWidth;
            for (int i = 0; i < view.Views.Length; i++)
            {
                var cuia = Rect.MinMaxRect(layout.CenterX[i] - half, layout.FootY[i] - layout.CuiaHeight, layout.CenterX[i] + half, layout.FootY[i]);
                Assert.That(cuia.xMin >= panel.xMin - 0.05f && cuia.xMax <= panel.xMax + 0.05f && cuia.yMin >= panel.yMin - 0.05f && cuia.yMax <= panel.yMax + 0.05f,
                    Is.True, where + ": la cuia " + i + " " + cuia + " dentro del panel " + panel);
            }

            // VA-15 (BoardLayout; BoardLayoutTests): el marco del panel puede pasar hasta 8 dp bajo la fila del HUD; las cuias, nunca.
            float blockTop = float.MaxValue;
            for (int i = 0; i < view.Views.Length; i++)
            {
                blockTop = Mathf.Min(blockTop, layout.FootY[i] - layout.CuiaHeight);
            }

            // La insignia «Nivel {n}» es una placa que monta sobre el canto de arriba del marco en los tableros de 3 filas (como a
            // 360 × 780; GameScreen.Labels.cs): con ella solo se exige que no toque las cuias.
            foreach ((string name, Rect r) in hud)
            {
                if (name == "insignia del nivel")
                {
                    Assert.That(r.yMax, Is.LessThanOrEqualTo(blockTop + 0.05f), where + ": " + name + " " + r + " encima de las cuias (" + blockTop + ")");
                    continue;
                }

                Assert.That(r.yMax, Is.LessThanOrEqualTo(panel.yMin + 8f + 0.05f), where + ": " + name + " " + r + " encima del panel " + panel + " (el marco, ≤ 8 dp debajo)");
                Assert.That(r.yMax, Is.LessThanOrEqualTo(blockTop + 0.05f), where + ": " + name + " " + r + " encima de las cuias (" + blockTop + ")");
            }

            foreach ((string name, Rect r) in tray)
            {
                Assert.That(r.yMin, Is.GreaterThanOrEqualTo(panel.yMax - 0.05f), where + ": " + name + " " + r + " debajo del panel " + panel);
                foreach ((string other, Rect h) in hud)
                {
                    Assert.That(r.Overlaps(h), Is.False, where + ": " + name + " no se cruza con " + other);
                }
            }
        }

        internal static void AssertInsideSafeArea(GameScreen view, RectTransform panel, string where)
        {
            Canvas.ForceUpdateCanvases();
            AssertInside(view, VictorySetup.CanvasRect(panel, Root(view)), where);
        }

        private static void AssertInside(GameScreen view, Rect r, string where)
        {
            Assert.That(r.xMin, Is.GreaterThanOrEqualTo(-0.05f), where + " " + r + ": dentro de la pantalla");
            Assert.That(r.xMax, Is.LessThanOrEqualTo(view.CanvasWidth + 0.05f), where + " " + r + ": dentro de la pantalla");
            Assert.That(r.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp - 0.05f), where + " " + r + ": bajo la zona segura de arriba");
            Assert.That(r.yMax, Is.LessThanOrEqualTo(view.CardCanvasHeight - view.CardSafeBottom + 0.05f), where + " " + r + ": sobre la zona segura de abajo");
        }

        private static void AddIfShown(List<(string, Rect)> list, string name, Button button, RectTransform root)
        {
            if (button.gameObject.activeInHierarchy)
            {
                list.Add((name, VictorySetup.CanvasRect((RectTransform)button.transform, root)));
            }
        }

        private static float GroupAlpha(Transform t)
        {
            float alpha = 1f;
            for (Transform? p = t; p != null; p = p.parent)
            {
                CanvasGroup g = p.GetComponent<CanvasGroup>();
                if (g != null && g.enabled)
                {
                    alpha *= g.alpha;
                    if (g.ignoreParentGroups)
                    {
                        break;
                    }
                }
            }

            return alpha;
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (Transform? p = t.parent; p != null && p.GetComponent<Canvas>() == null; p = p.parent)
            {
                path = p.name + "/" + path;
            }

            return path;
        }

        private static RectTransform Root(GameScreen view)
        {
            return (RectTransform)view.Canvas.transform;
        }
    }
}
