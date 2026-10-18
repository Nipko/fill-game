#nullable enable
using System.Collections;
using AF.GameFlow;
using AF.Levels;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Victoria con vista (D-013, punto 8, Q6-A; 02 §6; 07 §5.4; <c>ui_victory_view</c>, 08 §9; tarea M2.2) en la escena, con el
    /// catálogo real de paradas:
    /// <list type="bullet">
    /// <item>al abrirse la pantalla de victoria, tras la ola de saltos, el tablero (el panel y las cuias) pasa del alfa 1 al 0 en
    /// 250 ms y la banda del agua deja de tener el hueco del panel;</item>
    /// <item>con más de 10 cuias, la ola sigue al abrirse la victoria y el fundido empieza cuando acaba (con 16 cuias, 240 ms
    /// después; paquete de prueba de 16 cuias en el 131), sin que ninguna cuia salte mientras el tablero se va;</item>
    /// <item>al ganar el 20, el fondo pasa a Alter do Chão en el mismo fundido, y el 21 carga ya con él (sin otro fundido); en una
    /// victoria sin llegada, el sitio no cambia;</item>
    /// <item>con Reducir movimiento, corte; con la clave apagada, como antes (el tablero a la vista; el fondo cambia al cargar);</item>
    /// <item>Siguiente se habilita, empieza a verse y se ve entero en el mismo milisegundo que sin la vista;</item>
    /// <item>con 3 filas (el 131 del Trapecio, 11 cuias, a 360 × 780 dp), el tablero se va al acabar la ola y el sitio queda a
    /// la vista sobre la cinta de la victoria.</item>
    /// </list>
    /// </summary>
    public sealed class VictoryViewTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.VictoryViewOverride = true;
            GameScreen.FactLineDisabledForTests = true; // la vista no depende de la línea de la ranura
            MapArtRule.ForceProvisional = true;
            StopBackdrop.HeroSpriteOverride = _ => null; // las siluetas de respaldo, aunque algún día haya PNG
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.VictoryViewOverride = null;
            GameScreen.FactLineDisabledForTests = false;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            StopBackdrop.HeroSpriteOverride = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level20_TheBoardFadesTo0In250Ms_AndTheBackdropBecomesAlterDoChao_InTheSameFade()
        {
            yield return VictoryArrivalTests.Win(20);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            StopBackdrop b = view.StopBackdrop;
            Assert.That(view.VictoryViewOn, Is.True);
            Assert.That(view.VictoryViewFading, Is.True);
            Assert.That(view.BoardAlpha, Is.EqualTo(1f), "empieza con el tablero entero");
            Assert.That(b.Stop!.Id, Is.EqualTo("alter_do_chao"), "la llegada: la parada del 21");
            Assert.That(b.LeavingStop!.Id, Is.EqualTo("santarem"), "Santarém se va");
            Assert.That(b.Fading, Is.True);
            Assert.That(b.FrontAlpha, Is.EqualTo(0f));
            Assert.That(b.BandHole.width, Is.EqualTo(0f), "sin el hueco del panel: bajo el panel que se va, el agua");
            Assert.That(b.Water, Is.EqualTo(boot.Flow!.Catalog.Find(21)!.WaterBase), "el agua del mundo del 21");

            PlayModeHelpers.TickExact(boot, 125);
            Assert.That(view.BoardAlpha, Is.EqualTo(0.5f).Within(0.01f), "a mitad del fundido");
            Assert.That(b.FrontAlpha, Is.EqualTo(0.5f).Within(0.01f), "el fondo, en el mismo fundido");
            PlayModeHelpers.TickExact(boot, 124);
            Assert.That(view.BoardAlpha, Is.GreaterThan(0f), "a los 249 ms aún se ve");
            Assert.That(b.Fading, Is.True);
            PlayModeHelpers.TickExact(boot, 1);
            Assert.That(view.BoardAlpha, Is.EqualTo(0f), "alfa 0 a los 250 ms");
            Assert.That(view.VictoryViewFading, Is.False);
            Assert.That(b.Fading, Is.False, "el fondo termina a la vez");
            Assert.That(b.FrontAlpha, Is.EqualTo(1f));
            Assert.That(view.PanoramaTint, Is.EqualTo(StopBackdrop.LutOf(b.Stop, Season.LowWater)),
                "el panorama con la luz del sitio nuevo y la de aguas bajas del ciclo del 21 (SE2)");
            Assert.That(b.Season, Is.EqualTo(Season.LowWater), "la temporada del nivel siguiente");
            Assert.That(GameScreen.VictoryViewFadeMs, Is.EqualTo(250f));

            PlayModeHelpers.Step(boot, 1500);
            Assert.That(view.BoardAlpha, Is.EqualTo(0f), "se queda así hasta Siguiente");
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(21));
            Assert.That(view.BoardAlpha, Is.EqualTo(1f), "la carga devuelve el alfa a 1");
            Assert.That(view.VictoryViewOn, Is.False);
            Assert.That(view.LastStopChange, Is.EqualTo(GameScreen.StopChangeSame), "el 21 ya carga con su fondo");
            Assert.That(b.Fading, Is.False);
            Assert.That(b.BuildMs, Is.EqualTo(0d), "sin reconstruir en la carga");
            Assert.That(b.Stop.Id, Is.EqualTo("alter_do_chao"));
            Rect panel = view.StopPanelRect;
            Assert.That(b.BandHole.yMin, Is.EqualTo(panel.yMin + StopBackdrop.PanelHoleInsetDp).Within(0.01f), "vuelve el hueco del panel");
        }

        [UnityTest]
        public IEnumerator AWinWithoutArrival_FadesTheBoard_AndKeepsTheSite()
        {
            yield return VictoryArrivalTests.Win(21);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            StopBackdrop b = view.StopBackdrop;
            Assert.That(b.Stop!.Id, Is.EqualTo("alter_do_chao"));
            Assert.That(b.Fading, Is.False, "sin llegada, el fondo no cambia");
            PlayModeHelpers.TickExact(boot, 250);
            Assert.That(view.BoardAlpha, Is.EqualTo(0f));
            Assert.That(b.Stop.Id, Is.EqualTo("alter_do_chao"));
            Assert.That(b.BandHole.width, Is.EqualTo(0f));
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(22));
            Assert.That(view.BoardAlpha, Is.EqualTo(1f));
            Assert.That(view.LastStopChange, Is.EqualTo(GameScreen.StopChangeSame));
        }

        [UnityTest]
        public IEnumerator Level10_IsNotAnArrival_TheBoardFadesOverTheEstuary_AndLevel11ChangesOnLoad()
        {
            // En el 10, la primera llegada la cuenta el Mapa (02 §6, fila Llegada; 06 §2.3, paso 3; D-013, punto 7), con la
            // misma definición que MapMoments.ForVictory y VictoryLineArbiter: el fondo no cambia en la victoria.
            yield return VictoryArrivalTests.Win(10);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            StopBackdrop b = view.StopBackdrop;
            Assert.That(view.VictoryViewOn, Is.True);
            Assert.That(b.Stop!.Id, Is.EqualTo("belem"), "la parada del 1-10 sigue");
            Assert.That(b.Fading, Is.False, "el 10 no es una llegada: el fondo no funde");
            PlayModeHelpers.TickExact(boot, 250);
            Assert.That(view.BoardAlpha, Is.EqualTo(0f), "el tablero se va igual");
            Assert.That(view.VictoryViewFading, Is.False);
            Assert.That(b.Fading, Is.False);
            Assert.That(b.Stop.Id, Is.EqualTo("belem"), "el estuario, sin adelantar Santarém");
            Assert.That(b.BandHole.width, Is.EqualTo(0f));
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(b.Stop.Id, Is.EqualTo("belem"));
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(11));
            Assert.That(view.BoardAlpha, Is.EqualTo(1f), "la carga devuelve el alfa a 1");
            Assert.That(view.LastStopChange, Is.EqualTo(GameScreen.StopChangeFade), "el 11 cambia de fondo al cargar, con fundido");
            Assert.That(b.Stop.Id, Is.EqualTo("santarem"));
        }

        [UnityTest]
        public IEnumerator WithReduceMotion_ItIsACut()
        {
            yield return VictoryArrivalTests.Win(20, v => GameBootstrap.Current!.Flow!.ReduceMotion = true);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            StopBackdrop b = view.StopBackdrop;
            Assert.That(view.BoardAlpha, Is.EqualTo(0f), "corte directo");
            Assert.That(view.VictoryViewFading, Is.False);
            Assert.That(b.Stop!.Id, Is.EqualTo("alter_do_chao"));
            Assert.That(b.Fading, Is.False, "el fondo, también con corte");
            Assert.That(b.FrontAlpha, Is.EqualTo(1f));
            boot.Flow!.ReduceMotion = false;
        }

        [UnityTest]
        public IEnumerator WithTheKeyOff_ItIsAsBefore()
        {
            GameScreen.VictoryViewOverride = false;
            yield return VictoryArrivalTests.Win(20);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            StopBackdrop b = view.StopBackdrop;
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(view.VictoryViewOn, Is.False);
            Assert.That(view.BoardAlpha, Is.EqualTo(1f), "el tablero, a la vista");
            Assert.That(b.Stop!.Id, Is.EqualTo("santarem"), "el fondo no cambia en la victoria");
            Assert.That(b.BandHole.width, Is.GreaterThan(0f), "con el hueco del panel");
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(view.LastStopChange, Is.EqualTo(GameScreen.StopChangeFade), "cambia al cargar el 21, con fundido");
            Assert.That(b.Stop.Id, Is.EqualTo("alter_do_chao"));
        }

        [UnityTest]
        public IEnumerator Next_EnablesInTheSameMillisecond_WithAndWithoutTheView()
        {
            int[] without = new int[3];
            int[] with = new int[3];
            GameScreen.VictoryViewOverride = false;
            yield return MeasureNext(20, without);
            GameScreen.VictoryViewOverride = true;
            yield return MeasureNext(20, with);
            TestContext.WriteLine("Siguiente (ms desde la victoria) sin la vista: " + without[0] + "/" + without[1] + "/" + without[2]
                + "; con ella: " + with[0] + "/" + with[1] + "/" + with[2]);
            Assert.That(without[0], Is.GreaterThanOrEqualTo(0));
            Assert.That(with[0], Is.EqualTo(without[0]), "se habilita en el mismo milisegundo");
            Assert.That(with[1], Is.EqualTo(without[1]), "empieza a verse en el mismo milisegundo");
            Assert.That(with[2], Is.EqualTo(without[2]), "entero en el mismo milisegundo");
        }

        [UnityTest]
        public IEnumerator WithThreeRows_TheSiteIsSeen()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen780; // 360 × 780 dp: las filas dependen de la pantalla
            yield return VictoryArrivalTests.Win(131);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(3), "el 131 del Trapecio, en 3 filas");
            Assert.That(view.Views.Length, Is.EqualTo(11));
            StopBackdrop b = view.StopBackdrop;

            // Con 11 cuias la ola acaba a los 940 ms de la celebración, después de abrirse la victoria (≥ 900 ms).
            float wait = view.VictoryViewWaitMs;
            Assert.That(GameScreen.CelebrationWaveEndMs(11), Is.EqualTo(940f));
            Assert.That(wait, Is.GreaterThan(0f).And.LessThanOrEqualTo(40f), "la ola de 11 cuias aún no ha acabado");
            Assert.That(view.VictoryViewFading, Is.False);
            int waitMs = Mathf.CeilToInt(wait);
            PlayModeHelpers.TickExact(boot, waitMs - 1);
            Assert.That(view.BoardAlpha, Is.EqualTo(1f), "el tablero, entero mientras salta la ola");
            Assert.That(b.BandHole.width, Is.GreaterThan(0f), "con el hueco del panel mientras no se va");
            PlayModeHelpers.TickExact(boot, 1);
            Assert.That(view.VictoryViewFading, Is.True, "el fundido empieza al acabar la ola");
            PlayModeHelpers.TickExact(boot, 249);
            Assert.That(view.BoardAlpha, Is.GreaterThan(0f), "a los 249 ms del fin de la ola aún se ve");
            PlayModeHelpers.TickExact(boot, 1);
            Assert.That(view.BoardAlpha, Is.EqualTo(0f), "alfa 0 a los 250 ms del fin de la ola: el panel ya no tapa el horizonte");
            Assert.That(b.Stop!.Id, Is.EqualTo("rio_frontera"), "el sitio de la parada 13");
            Assert.That(b.Fading, Is.False);
            Rect hero = view.HeroRect;
            float ribbonTop = view.VictoryPanelTopDp - 38f;
            TestContext.WriteLine("héroe: " + hero.yMin + "-" + hero.yMax + " dp; horizonte " + view.HorizonY + " dp; cinta " + ribbonTop + " dp");
            Assert.That(hero.height, Is.GreaterThan(0f), "con la silueta provisional");
            Assert.That(hero.yMax, Is.LessThanOrEqualTo(ribbonTop), "el sitio entero por encima de la cinta de la victoria");
            Assert.That(b.BandHole.width, Is.EqualTo(0f), "la banda del agua, entera");
        }

        [UnityTest]
        public IEnumerator WithSixteenCuias_TheFadeStartsWhenTheHopWaveEnds()
        {
            // 16 cuias (6/5/5, siempre en 3 filas, D-006) en el 131: la última cuia salta de 900 a 1.140 ms de la celebración,
            // así que el panel espera 240 ms tras abrirse la victoria y acaba de irse a los 1.390 ms, antes de Siguiente.
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.LevelPackJsonOverride = SixteenCuiaWinPack(131);
            yield return VictoryArrivalTests.Win(131);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            CuiaView[] cuias = view.Views;
            Assert.That(cuias.Length, Is.EqualTo(16));
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            Assert.That(GameScreen.CelebrationWaveEndMs(16), Is.EqualTo(1140f));
            Assert.That(view.VictoryViewOn, Is.True);
            Assert.That(view.VictoryViewFading, Is.False, "la ola sigue: el fundido espera");
            Assert.That(view.BoardAlpha, Is.EqualTo(1f));

            // La victoria se abre en el primer paso de 16 ms con ≥ 900 ms de celebración: faltan 1.140 − (900…915) ms.
            float wait = view.VictoryViewWaitMs;
            Assert.That(wait, Is.GreaterThan(1140f - 900f - PlayModeHelpers.FrameMs).And.LessThanOrEqualTo(1140f - 900f));

            // Milisegundo a milisegundo: cuándo deja de saltar la última cuia, cuándo empieza el fundido y cuándo acaba.
            int lastHopMs = -1;
            int fadeStart = -1;
            int fadeEnd = -1;
            float maxHopDuringFade = 0f;
            for (int ms = 0; ms <= 600 && fadeEnd < 0; ms++)
            {
                float hop = 0f;
                for (int i = 0; i < cuias.Length; i++)
                {
                    hop = Mathf.Max(hop, cuias[i].BodyOffset.y);
                }

                if (hop > 0.001f)
                {
                    lastHopMs = ms;
                }

                if (fadeStart < 0 && view.BoardAlpha < 1f)
                {
                    fadeStart = ms;
                }

                if (fadeStart >= 0)
                {
                    maxHopDuringFade = Mathf.Max(maxHopDuringFade, hop);
                }

                if (view.BoardAlpha <= 0f)
                {
                    fadeEnd = ms;
                }

                PlayModeHelpers.TickExact(boot, 1);
            }

            float unit = cuias[0].Unit;
            TestContext.WriteLine("16 cuias, desde la victoria: espera " + wait + " ms; último salto hasta " + lastHopMs + " ms; fundido "
                + fadeStart + "-" + fadeEnd + " ms; salto máximo durante el fundido " + maxHopDuringFade + " dp (salto entero "
                + (0.15f * unit) + " dp)");
            int waitMs = Mathf.CeilToInt(wait);
            Assert.That(fadeStart, Is.EqualTo(waitMs + 1), "el tablero empieza a irse al acabar la ola (1 ms después ya baja)");
            Assert.That(fadeEnd, Is.EqualTo(waitMs + 250), "alfa 0 a los 250 ms del fin de la ola");
            Assert.That(lastHopMs, Is.GreaterThan(200), "la última cuia aún saltaba al abrirse la victoria");
            Assert.That(fadeStart, Is.GreaterThanOrEqualTo(lastHopMs - PlayModeHelpers.FrameMs),
                "el fundido no empieza antes del fin de la ola (como mucho un paso de 16 ms: la cuia arranca su salto en el paso)");
            Assert.That(maxHopDuringFade, Is.LessThan(0.15f * unit * 0.25f), "ninguna cuia salta mientras el tablero se va");
            float victoryAt = 1140f - wait; // ms de celebración al abrirse la victoria
            Assert.That(victoryAt + fadeEnd, Is.LessThanOrEqualTo(1400f), "se va antes de Siguiente (1,40 s)");
        }

        /// <summary>
        /// Paquete de un nivel <paramref name="level"/> del Trapecio (mundo 4) con 16 cuias (6/5/5, D-006) que se gana en pocos
        /// vertidos: 14 tintes de 4 dosis en 7 parejas de cuias con una dosis cambiada (3 a + 1 b y 3 b + 1 a; ninguna empieza
        /// sellada) y 2 vacías.
        /// </summary>
        internal static string SixteenCuiaWinPack(int level)
        {
            string[] ids = { "guanabana", "cupuacu", "tapereba", "aguaje", "urucum", "camu_camu", "bacaba", "acai", "jenipapo", "agua_tapajos", "lulo", "araza", "cacao", "agua_negro" };
            var cuias = new int[16][];
            for (int i = 0; i < 16; i++)
            {
                int a = (i & ~1) + 1; // la pareja (2k, 2k + 1) lleva los tintes 2k + 1 y 2k + 2
                int b = a + 1;
                cuias[i] = i >= ids.Length ? new int[0] : ((i & 1) == 0 ? new[] { a, a, a, b } : new[] { b, b, b, a });
            }
            return PlayModeHelpers.BoardPack(4, "w04_trapecio", "white", level, ids, cuias);
        }

        // Milisegundo (desde la victoria) en que Siguiente se habilita, empieza a verse y se ve entero.
        private static IEnumerator MeasureNext(int level, int[] times)
        {
            yield return VictoryArrivalTests.Win(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            for (int i = 0; i < times.Length; i++)
            {
                times[i] = -1;
            }

            for (int ms = 0; ms <= 1700; ms++)
            {
                if (times[0] < 0 && view.NextButton.interactable)
                {
                    times[0] = ms;
                }

                if (times[1] < 0 && view.NextAlpha > 0f)
                {
                    times[1] = ms;
                }

                if (times[2] < 0 && view.NextAlpha >= 1f)
                {
                    times[2] = ms;
                }

                PlayModeHelpers.TickExact(boot, 1);
            }
        }
    }
}
