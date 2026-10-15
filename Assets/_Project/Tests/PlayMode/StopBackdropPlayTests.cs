#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
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
    /// Fondo de la parada en la escena (tarea M1.4; D-013, punto 9; 07 §2.1; propuesta del Mapa §10.5): cada nivel enseña el de su
    /// parada (1-10 las dos agujas, 11 la línea clara y blanca, 41 la diagonal, 51 el domo, 131 la baliza y 151 las balsas, estos
    /// dos en tableros de 3 filas), en la primera carga con corte; del 20 al 21, fundido de 250 ms sin retrasar la carga; con
    /// Reducir movimiento, corte; sin catálogo, el aspecto de hoy. Las capas van entre el panorama y la garza, no reciben toques y
    /// la sonda <c>AF_STATE stop</c> dice la parada y su <c>build_ms</c> (≤ 8 ms en el Editor).
    /// </summary>
    public sealed class StopBackdropPlayTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            StopBackdrop.HeroSpriteOverride = _ => null; // las siluetas de respaldo, aunque algún día haya PNG
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            StopBackdrop.HeroSpriteOverride = null;
            MapArtRule.ForceProvisional = null;
        }

        [UnityTest]
        public IEnumerator EachLevel_ShowsItsStop_WithACutOnTheFirstLoad()
        {
            var cases = new (int Level, int Stop, string? Shape, string? Code)[]
            {
                (1, 0, StopShapes.TwoSpires, null),
                (10, 0, StopShapes.TwoSpires, null),
                (11, 1, null, StopBackdrop.CodeClearWhite),
                (41, 4, null, StopBackdrop.CodeBlackWhite),
                (51, 5, StopShapes.Dome, null),
                (131, 13, StopShapes.Beacon, "kit:rio_ancho_con_baliza"),
                (151, 15, StopShapes.Rafts, null),
            };
            foreach ((int level, int n, string? shape, string? code) in cases)
            {
                yield return HudSetup.LoadAtIntro(level);
                GameScreen view = GameBootstrap.Current!.View!;
                StopBackdrop b = view.StopBackdrop;
                Assert.That(b.Stop, Is.Not.Null, "nivel " + level);
                Assert.That(b.Stop!.N, Is.EqualTo(n), "nivel " + level);
                Assert.That(b.Shape, Is.EqualTo(shape), "nivel " + level);
                Assert.That(b.Code, Is.EqualTo(code), "nivel " + level);
                Assert.That(b.HeroSource, Is.EqualTo(shape != null ? StopBackdrop.HeroShape : StopBackdrop.HeroNone), "nivel " + level);
                Assert.That(view.LastStopChange, Is.EqualTo(GameScreen.StopChangeCut), "nivel " + level + ": primera carga, corte");
                Assert.That(b.Fading, Is.False);
                Assert.That(b.Root.gameObject.activeSelf, Is.True);
                Assert.That(b.Water, Is.EqualTo(GameBootstrap.Current.Flow!.Level.WaterBase), "el agua base del mundo");
                if (shape != null)
                {
                    Assert.That(view.HeroRect.height, Is.EqualTo(80f * (view.CanvasWidth / 360f)).Within(0.01f), "HeroRect para TI6a");
                    Assert.That(view.HeroRect.yMax - (32f * view.CanvasWidth / 360f), Is.EqualTo(view.HorizonY).Within(0.01f), "pie en el horizonte");
                }
            }
        }

        [UnityTest]
        public IEnumerator From20To21_TheBackdropCrossfadesIn250Ms_WithoutDelayingTheLoad()
        {
            yield return HudSetup.LoadAtIntro(20);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.StopBackdrop.Stop!.N, Is.EqualTo(1), "el 20 aún es de Santarém");
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(21), "Siguiente carga el 21 en el acto");
            Assert.That(VictorySetup.InNewLevel(boot.Flow.Phase), Is.True, "la carga no espera al fundido: " + boot.Flow.Phase);

            StopBackdrop b = view.StopBackdrop;
            Assert.That(b.Stop!.N, Is.EqualTo(2), "Alter do Chão");
            Assert.That(view.LastStopChange, Is.EqualTo(GameScreen.StopChangeFade));
            Assert.That(b.Fading, Is.True);
            Assert.That(b.FrontAlpha, Is.EqualTo(0f));
            PlayModeHelpers.TickExact(boot, 125);
            Assert.That(b.FrontAlpha, Is.EqualTo(0.5f).Within(0.01f), "a mitad del fundido");
            Assert.That(b.BackAlpha, Is.EqualTo(0.5f).Within(0.01f));
            PlayModeHelpers.TickExact(boot, 120);
            Assert.That(b.Fading, Is.True, "a los 245 ms aún funde");
            PlayModeHelpers.TickExact(boot, 5);
            Assert.That(b.Fading, Is.False, "a los 250 ms ha terminado");
            Assert.That(b.FrontAlpha, Is.EqualTo(1f));
            Assert.That(b.BackAlpha, Is.EqualTo(0f));
            Assert.That(MapConfig.StopCrossfadeMs, Is.EqualTo(250));
        }

        [UnityTest]
        public IEnumerator WithReduceMotion_TheChangeIsACut()
        {
            yield return HudSetup.LoadAtIntro(20);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1500);
            boot.Flow!.ReduceMotion = true;
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(21));
            Assert.That(view.StopBackdrop.Stop!.N, Is.EqualTo(2));
            Assert.That(view.LastStopChange, Is.EqualTo(GameScreen.StopChangeCut));
            Assert.That(view.StopBackdrop.Fading, Is.False);
            Assert.That(view.StopBackdrop.FrontAlpha, Is.EqualTo(1f));
            boot.Flow.ReduceMotion = false;
        }

        [UnityTest]
        public IEnumerator WithinAStop_NothingChanges()
        {
            yield return HudSetup.LoadAtIntro(21);
            GameBootstrap boot = GameBootstrap.Current!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(22));
            Assert.That(boot.View!.LastStopChange, Is.EqualTo(GameScreen.StopChangeSame));
            Assert.That(boot.View.StopBackdrop.Fading, Is.False);
            Assert.That(boot.View.StopBackdrop.BuildMs, Is.EqualTo(0d), "la misma composición no se reconstruye");
        }

        [UnityTest]
        public IEnumerator WithoutTheCatalog_TheSceneIsAsToday()
        {
            yield return HudSetup.LoadAtIntro(9);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.StopBackdrop.Root.gameObject.activeSelf, Is.True);
            view.RiverStops = null; // como sin river_stops_v1.json
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(10));
            PlayModeHelpers.Step(boot, 400);
            StopBackdrop b = view.StopBackdrop;
            Assert.That(b.Stop, Is.Null);
            Assert.That(b.Root.gameObject.activeSelf, Is.False, "nada del fondo de la parada");
            Assert.That(b.HeroRect.width, Is.EqualTo(0f));
            Assert.That(view.PanoramaTint, Is.EqualTo(Color.white), "el panorama, sin tinte");
            Assert.That(boot.TapAnywhere(), Is.True, "el nivel se juega igual");
        }

        [UnityTest]
        public IEnumerator TheLayers_SitBetweenPanoramaAndHeron_AndTakeNoTouches()
        {
            yield return HudSetup.LoadAtIntro(51);
            GameScreen view = GameBootstrap.Current!.View!;
            RectTransform root = view.StopBackdrop.Root;
            Assert.That(root.parent, Is.SameAs(view.PanoramaRect.parent), "en la escena");
            Assert.That(root.GetSiblingIndex(), Is.GreaterThan(view.PanoramaRect.GetSiblingIndex()), "delante del panorama");
            Assert.That(root.GetSiblingIndex(), Is.LessThan(view.HeronRect.GetSiblingIndex()), "detrás de la garza");
            foreach (Graphic g in root.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(g.raycastTarget, Is.False, g.name);
            }

            Assert.That(root.GetComponent<GraphicRaycaster>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator TheProbe_SaysTheStop_AndBuildMsIsUnderEight()
        {
            var lines = new List<string>();
            Application.LogCallback capture = (message, stack, type) =>
            {
                if (message.Contains("\"event\":\"stop\""))
                {
                    lines.Add(message);
                }
            };
            Application.logMessageReceived += capture;
            try
            {
                yield return HudSetup.LoadAtIntro(91);
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }

            Assert.That(lines.Count, Is.EqualTo(1), string.Join("\n", lines));
            string line = lines[0];
            StringAssert.StartsWith("AF_STATE {\"event\":\"stop\"", line);
            StringAssert.Contains("\"level\":91", line);
            StringAssert.Contains("\"stop\":9,\"id\":\"leticia_atardecer\",\"world\":4", line);
            StringAssert.Contains("\"hero\":\"shape\",\"shape\":\"generic_city\"", line);
            StringAssert.Contains("\"lut\":\"dusk\",\"water\":\"white\",\"change\":\"cut\"", line);
            Match m = Regex.Match(line, "\"build_ms\":([0-9.]+)");
            Assert.That(m.Success, Is.True, line);
            double ms = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            TestContext.WriteLine("build_ms en el Editor: " + ms);
            Assert.That(ms, Is.LessThanOrEqualTo(8d), "08 §5.4");
            Assert.That(GameBootstrap.Current!.View!.PanoramaTint, Is.EqualTo(StopBackdrop.DuskLut), "el panorama con la luz del atardecer");
        }

        [UnityTest]
        public IEnumerator AsInRelease_NoSilhouette_InABoardOfThreeRows()
        {
            MapArtRule.ForceProvisional = false;
            yield return HudSetup.LoadAtIntro(151);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.StopBackdrop.Stop!.N, Is.EqualTo(15));
            Assert.That(view.StopBackdrop.HeroSource, Is.EqualTo(StopBackdrop.HeroNone), "sin PNG, nada en release");
            Assert.That(view.StopBackdrop.ShapeGraphic.IsEmpty, Is.True);
            Assert.That(view.StopBackdrop.BandGraphic.IsEmpty, Is.False, "el agua del mundo sí");
            HudSetup.SkipIntro(GameBootstrap.Current!); // los errores del log harían fallar el test: con 3 filas no hay ninguno
            Assert.That(GameBootstrap.Current!.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }
    }
}
