#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AF.GameFlow;
using AF.Levels;
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
    /// la sonda <c>AF_STATE stop</c> dice la parada y su <c>build_ms</c> (≤ 8 ms en el Editor). Lo que pasa por la victoria va con
    /// <c>ui_victory_view</c> apagada: con ella, el fondo de la llegada cambia ya en la victoria (M2.2, <see cref="VictoryViewTests"/>).
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
            GameScreen.VictoryViewOverride = null;
            GameScreen.SeasonOverride = null;
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
            GameScreen.VictoryViewOverride = false; // con ui_victory_view = true, la victoria lo cambia antes (VictoryViewTests)
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
            Assert.That(b.BackAlpha, Is.EqualTo(1f), "la vieja sigue entera debajo: la banda no deja ver el panorama");
            Assert.That(b.BackDetailAlpha, Is.EqualTo(0.5f).Within(0.01f), "lo que la nueva no tapa se va");
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
            GameScreen.VictoryViewOverride = false; // con ui_victory_view = true, la victoria lo cambia antes (VictoryViewTests)
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

            // Sin parada antes ni después nada cambia a la vista, aunque cambie el agua del mundo (10 → 11, blanca → clara).
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(11));
            Assert.That(b.Stop, Is.Null);
            Assert.That(view.LastStopChange, Is.EqualTo(GameScreen.StopChangeSame), "ni corte ni fundido");
            Assert.That(b.Fading, Is.False);
            if (view.LastStopProbe != null)
            {
                StringAssert.Contains("\"hero\":null", view.LastStopProbe);
                StringAssert.Contains("\"fade\":false", view.LastStopProbe);
                StringAssert.Contains("\"change\":\"same\"", view.LastStopProbe);
            }
        }

        [UnityTest]
        public IEnumerator WithoutTheCatalog_OnlyTheSeasonChanges_From20To21_AndNotFrom21To22()
        {
            // Sin parada antes ni después, el cambio de temporada sí se ve (SE2): de 20 (sin temporada) a 21 (ciclo B), la luz de
            // aguas bajas sobre el panorama; dentro del ciclo, de 21 a 22, nada. En el juego, sin catálogo o del 201 sin paradas.
            yield return HudSetup.LoadAtIntro(19);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.Seasons, Is.Not.Null, "premisa: la escena lleva la tabla de temporada");
            Assert.That(boot.Seasons!.SeasonOf(20), Is.EqualTo(Season.None), "premisa: el 20 no tiene temporada");
            Assert.That(boot.Seasons.SeasonOf(21), Is.EqualTo(Season.LowWater), "premisa: el ciclo del 21 es B");
            Assert.That(boot.Seasons.SeasonOf(22), Is.EqualTo(Season.LowWater));
            view.RiverStops = null; // como sin river_stops_v1.json
            StopBackdrop b = view.StopBackdrop;

            WinAndPressNext(boot, 20);
            Assert.That(b.Stop, Is.Null);
            Assert.That(b.Season, Is.EqualTo(Season.None));
            Assert.That(view.PanoramaTint, Is.EqualTo(Color.white), "el 20, sin parada ni temporada: sin tinte");

            WinAndPressNext(boot, 21);
            Assert.That(b.Stop, Is.Null, "sigue sin parada");
            Assert.That(view.LastStopChange, Is.Not.EqualTo(GameScreen.StopChangeSame), "solo cambia la temporada, y eso se ve");
            Assert.That(view.LastStopChange, Is.EqualTo(GameScreen.StopChangeCut), "sin parada a la vista no hay capas que fundir");
            Assert.That(b.Season, Is.EqualTo(Season.LowWater));
            Assert.That(b.LutColor, Is.EqualTo(StopBackdrop.LowWaterLut));
            Assert.That(view.PanoramaTint, Is.EqualTo(StopBackdrop.LowWaterLut), "el panorama con la luz de aguas bajas");
            Assert.That(b.Root.gameObject.activeSelf, Is.False, "nada del fondo de la parada");
            Assert.That(view.LastStopProbe, Is.Not.Null, "la sonda stop en el Editor (build de desarrollo)");
            StringAssert.Contains("\"level\":21", view.LastStopProbe);
            StringAssert.Contains("\"hero\":null", view.LastStopProbe);
            StringAssert.Contains("\"change\":\"cut\"", view.LastStopProbe);
            StringAssert.EndsWith(",\"season\":\"B\"", view.LastStopProbe);

            WinAndPressNext(boot, 22);
            Assert.That(view.LastStopChange, Is.EqualTo(GameScreen.StopChangeSame), "misma temporada y sin parada: nada cambia");
            Assert.That(b.BuildMs, Is.EqualTo(0d), "la misma composición no se reconstruye");
            Assert.That(b.Fading, Is.False);
            Assert.That(b.Season, Is.EqualTo(Season.LowWater));
            Assert.That(view.PanoramaTint, Is.EqualTo(StopBackdrop.LowWaterLut));
            StringAssert.EndsWith("\"change\":\"same\",\"season\":\"B\"", view.LastStopProbe);
        }

        private static void WinAndPressNext(GameBootstrap boot, int next)
        {
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(next));
        }

        [UnityTest]
        public IEnumerator TheBandHole_FollowsThePanel_WhenTheBoardRisesForTheVictory()
        {
            GameScreen.VictoryViewOverride = false; // con ui_victory_view = true, la victoria lo cambia antes (VictoryViewTests)
            yield return HudSetup.LoadAtIntro(21);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            StopBackdrop b = view.StopBackdrop;
            float inset = StopBackdrop.PanelHoleInsetDp;
            Rect panel = view.StopPanelRect;
            Assert.That(b.BandHole.width, Is.GreaterThan(0f), "con tablero, la banda deja el hueco del panel");
            Assert.That(b.BandHole.yMin, Is.EqualTo(panel.yMin + inset).Within(0.01f));
            Assert.That(b.BandHole.xMin, Is.EqualTo(panel.xMin + inset).Within(0.01f));
            Assert.That(b.BandHole.xMax, Is.EqualTo(panel.xMax - inset).Within(0.01f));
            Assert.That(b.BandHole.yMax, Is.EqualTo(panel.yMax - inset).Within(0.01f));

            float before = panel.yMin;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 1500);
            panel = view.StopPanelRect;
            TestContext.WriteLine("panel: de " + before + " a " + panel.yMin + " dp");
            Assert.That(b.BandHole.yMin, Is.EqualTo(panel.yMin + inset).Within(0.01f), "el hueco sigue al tablero desplazado");
            Assert.That(b.BandHole.yMax, Is.EqualTo(panel.yMax - inset).Within(0.01f));
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
            // Los campos de docs/dev/prototipo-v0.md, en su orden, y después los añadidos por M1.4.
            StringAssert.Contains("\"stop\":9,\"id\":\"leticia_atardecer\",\"world\":4,\"hero\":\"bg_hero_leticia\"", line);
            StringAssert.Contains("\"lut\":\"dusk\",\"water\":\"white\",\"fade\":false,\"build_ms\":", line);
            StringAssert.IsMatch("\"build_ms\":[0-9.]+,\"warm_ms\":[0-9.]+,\"hero_src\"", line); // FX4
            StringAssert.Contains("\"hero_src\":\"shape\",\"shape\":\"leticia\",\"code\":null,\"change\":\"cut\",\"season\":\"A\"}", line); // SE2
            Match m = Regex.Match(line, "\"build_ms\":([0-9.]+)");
            Assert.That(m.Success, Is.True, line);
            double ms = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            TestContext.WriteLine("build_ms en el Editor: " + ms);
            Assert.That(ms, Is.LessThanOrEqualTo(8d), "08 §5.4");
            Assert.That(GameBootstrap.Current!.View!.PanoramaTint, Is.EqualTo(StopBackdrop.DuskLut * StopBackdrop.TwilightHighWaterLut),
                "el panorama con la luz del atardecer y la de aguas altas del ciclo 81 (SE2)");
        }

        [UnityTest]
        public IEnumerator WithoutCodeArt_NoSilhouette_InABoardOfThreeRows()
        {
            MapArtRule.ForceProvisional = false; // forzado: hoy el arte por código sale en todos los builds, release incluido (D-052)
            yield return HudSetup.LoadAtIntro(151);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.StopBackdrop.Stop!.N, Is.EqualTo(15));
            Assert.That(view.StopBackdrop.HeroSource, Is.EqualTo(StopBackdrop.HeroNone), "sin arte por código ni PNG, nada");
            Assert.That(view.StopBackdrop.ShapeGraphic.IsEmpty, Is.True);
            Assert.That(view.StopBackdrop.BandGraphic.IsEmpty, Is.False, "el agua del mundo sí");
            HudSetup.SkipIntro(GameBootstrap.Current!); // los errores del log harían fallar el test: con 3 filas no hay ninguno
            Assert.That(GameBootstrap.Current!.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }
    }
}
