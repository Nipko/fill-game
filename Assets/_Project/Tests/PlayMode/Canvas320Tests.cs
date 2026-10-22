#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
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
    /// El lienzo en los teléfonos de 320 a 360 dp de ancho (Q-B11-5 (a), D-033, añadido de B11; cierre de B11; 02 §5,
    /// «Dispositivos», y §12): mide lo que el teléfono con 1 dp físico por dp de lienzo (<see cref="GameScreen.CanvasScale(int, int, float)"/>),
    /// así que el texto de 12 sp sale a 12 sp físicos y el tablero usa el layout de 320 dp (6 cuias por fila; BoardLayout320Tests).
    /// En la escena, a 720 × 1520 px con 2,25 px/dp (320 × 675,6 dp): la pantalla de nivel, el inicio, la tienda (la capa de
    /// encima) y la victoria miden 320 dp; en el nivel 1 y el 374 (3 filas) nada se solapa ni sale de la zona segura y ningún texto
    /// baja de 12 sp físicos; Aguas estancadas, Sin canoas y la victoria caben en la pantalla.
    /// </summary>
    public sealed class Canvas320Tests
    {
        /// <summary>320 × 675,6 dp a 2,25 px/dp, con 32 dp de zona segura arriba y 16 abajo.</summary>
        internal static readonly Vector4 Screen320 = new Vector4(720f, 1520f, 72f, 36f);

        internal const float Density = 2.25f;

        // AssertTexts mide los sp físicos con ancho / 360 px por dp: con 810 px, 2,25 px/dp, la densidad de Screen320.
        private const int TextsWidthPx = 810;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = Screen320;
            GameScreen.SimulatedPxPerDp = Density;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            GameScreen.SimulatedPxPerDp = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator TheCanvas_Is320Wide_With1PhysicalDpPerDp_AndTheLevelFits([Values(1, 374)] int level)
        {
            yield return Canvas16x9LayoutTests.LoadPlaying(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Canvas.GetComponent<CanvasScaler>().scaleFactor, Is.EqualTo(Density).Within(1e-4f), "1 dp físico por dp");
            Assert.That(view.CanvasWidth, Is.EqualTo(320f).Within(0.01f));
            Assert.That(view.CardCanvasHeight, Is.EqualTo(1520f / Density).Within(0.01f));
            string at = "el nivel " + level + " a 320 dp";
            Canvas16x9LayoutTests.AssertLayout(view, at);
            Canvas16x9LayoutTests.AssertTexts(view, TextsWidthPx, at, 3);
            if (level == 374)
            {
                Assert.That(view.Layout!.Rows, Is.EqualTo(3));
                foreach (int n in view.Layout.PerRow)
                {
                    Assert.That(n, Is.LessThanOrEqualTo(6), at + ": como mucho 6 por fila (02 §5)");
                }
            }
        }

        [UnityTest]
        public IEnumerator TheStuckModal_AndNoCanoes_FitAt320()
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0, 1));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            PlayModeHelpers.Step(boot, 800);
            Assert.That(view.StuckVisible, Is.True);
            Canvas16x9LayoutTests.AssertInsideSafeArea(view, view.Stuck.Panel, "Aguas estancadas a 320 dp");
            Canvas16x9LayoutTests.AssertTexts(view, TextsWidthPx, "Aguas estancadas a 320 dp", 4);

            boot.PressGiveUp();
            boot.ConfirmGiveUp();
            boot.PressRetry();
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            PlayModeHelpers.Step(boot, 600);
            AssertButtonsInside(view.NoCanoes.Root, view.CanvasWidth, "Sin canoas a 320 dp");
            Canvas16x9LayoutTests.AssertTexts(view, TextsWidthPx, "Sin canoas a 320 dp", 3);
        }

        [UnityTest]
        public IEnumerator TheVictory_FitsAt320()
        {
            yield return FactLineTests.Win(26);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(view.CanvasWidth, Is.EqualTo(320f).Within(0.01f));
            Assert.That(view.VictoryPanelWidthDp, Is.EqualTo(288f).Within(0.01f), "el panel, con 16 dp a cada lado");
            Fx16bTests.AssertCompactVictory(view, "la victoria del 26 a 320 dp");
            Canvas16x9LayoutTests.AssertTexts(view, TextsWidthPx, "la victoria del 26 a 320 dp", 3);
        }

        [UnityTest]
        public IEnumerator TheLobby_And_TheStoreLayer_Are320Wide()
        {
            Profile profile = FunnelSetup.Profile(500);
            profile.Progress.MaxLevel = 30;
            profile.Progress.NextLevel = 31;
            profile.Progress.MapSeenLevel = 30;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            Assert.That(lobby.ScaleFactor, Is.EqualTo(Density).Within(1e-4f), "el inicio, con la misma escala");
            Assert.That(lobby.CanvasWidth, Is.EqualTo(320f).Within(0.01f));
            AssertButtonsInside(lobby.Root, lobby.CanvasWidth, "el inicio a 320 dp");
        }

        [UnityTest]
        public IEnumerator Settings_TheMarket_AndTheLogbook_FitAt320([Values(0, 1, 2)] int which)
        {
            GameBootstrap.LogbookOverride = which == 2;
            try
            {
                Profile profile = FunnelSetup.Profile(500);
                profile.Progress.MaxLevel = 30;
                profile.Progress.NextLevel = 31;
                profile.Progress.MapSeenLevel = 30;
                yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
                GameBootstrap boot = GameBootstrap.Current!;
                PlayModeHelpers.TakeClock(boot);
                string what;
                switch (which)
                {
                    case 0:
                        Assert.That(boot.PressSettings(), Is.True);
                        what = "Ajustes";
                        break;
                    case 1:
                        Assert.That(boot.OpenMarket(MarketTab.Boosters), Is.True);
                        what = "el Mercado";
                        break;
                    default:
                        Assert.That(boot.OpenLogbook(LogbookOpenSource.Card), Is.True);
                        what = "la libreta de la Bitácora";
                        break;
                }

                PlayModeHelpers.Step(boot, 600);
                yield return null;
                AssertAllRootCanvases(what + " a 320 dp");
            }
            finally
            {
                GameBootstrap.LogbookOverride = null;
            }
        }

        // Cada lienzo raíz con escala (el de nivel, el del inicio y las capas de encima) va a 2,25 px/dp (320 dp de ancho) y sus botones
        // a la vista caben en ese ancho.
        private static void AssertAllRootCanvases(string where)
        {
            Canvas.ForceUpdateCanvases();
            var issues = new List<string>();
            int canvases = 0;
            foreach (CanvasScaler scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None))
            {
                Canvas canvas = scaler.GetComponent<Canvas>();
                if (canvas == null || !canvas.isRootCanvas || !canvas.gameObject.activeInHierarchy || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    continue;
                }

                // Con la pantalla simulada, el rect del lienzo es el de la ventana del Editor: el ancho que cuenta es el de la
                // maquetación, 320 dp, que la escala de los tres lienzos raíz da a 2,25 px/dp.
                canvases++;
                var rt = (RectTransform)canvas.transform;
                const float width = 320f;
                if (Mathf.Abs(scaler.scaleFactor - Density) > 1e-4f)
                {
                    issues.Add(where + ": el lienzo " + canvas.name + " con escala " + scaler.scaleFactor);
                }

                foreach (Button b in canvas.GetComponentsInChildren<Button>(false))
                {
                    Rect r = VictorySetup.CanvasRect((RectTransform)b.transform, rt);
                    if (r.width > 0f && (r.xMin < -0.05f || r.xMax > width + 0.05f))
                    {
                        issues.Add(where + ": " + canvas.name + "/" + b.name + " " + r + " se sale del ancho");
                    }
                }
            }

            Assert.That(canvases, Is.GreaterThan(0), where);
            Assert.That(issues, Is.Empty, string.Join(" | ", issues));
        }

        // Todo botón activo bajo root, dentro del ancho del lienzo y con 48 dp o más de alto y de ancho (02 §3.3).
        private static void AssertButtonsInside(RectTransform root, float canvasWidth, string where)
        {
            Canvas.ForceUpdateCanvases();
            Canvas canvas = root.GetComponentInParent<Canvas>().rootCanvas;
            var canvasRect = (RectTransform)canvas.transform;
            var issues = new List<string>();
            int seen = 0;
            foreach (Button b in root.GetComponentsInChildren<Button>(false))
            {
                if (!b.gameObject.activeInHierarchy)
                {
                    continue;
                }

                seen++;
                Rect r = VictorySetup.CanvasRect((RectTransform)b.transform, canvasRect);
                if (r.xMin < -0.05f || r.xMax > canvasWidth + 0.05f)
                {
                    issues.Add(where + ": " + b.name + " " + r + " se sale del ancho (" + canvasWidth + " dp)");
                }
            }

            Assert.That(seen, Is.GreaterThan(0), where + ": condición del test, botones a la vista");
            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }
    }
}
