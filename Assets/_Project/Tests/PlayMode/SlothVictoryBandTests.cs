#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// TiTino en la victoria con vista (tarea DC2; pregunta 53 a y 54 a de <c>docs/GDD.md</c> §6.3, D-028; 02 §6, filas TiTino y
    /// victoria con vista; 07 §5.4; T6-A; propuesta de TiTino d.4):
    /// <list type="bullet">
    /// <item>con la victoria con vista (<c>ui_victory_view</c>, el default), la caja de TiTino es la franja del pie de la cabecera
    /// a la cima de la cinta (menos <see cref="GameScreen.SlothRibbonGapDp"/>) sin el héroe del sitio del fondo: el trozo más alto
    /// de los dos que deja (<see cref="GameScreen.SlothViewBand"/>); sin héroe, la franja entera;</item>
    /// <item>en los hitos reales (40, 60, 80 y 140, tableros de 2 filas) y con un tablero de 3 filas, a 360 × 780 y 360 × 640, al
    /// 100 % y al 130 %, con la ranura del Dato del río y sin ella: TiTino sale, y habla en sus hitos si la franja mide ≥ 104 dp
    /// (la llegada va entonces sin dato, 02 §6 y T6-A); nunca se cruza con el héroe, la cinta, la cabecera ni el panel. Al 130 %
    /// con la ranura la cinta sube a 162,6 dp y, en los hitos con héroe (60, 80 y 140), lo que deja el héroe no da para TiTino:
    /// no sale y el hito lleva su dato (medido en DC2);</item>
    /// <item>las reglas de 54 (a), ratificadas, no cambian: con ≥ 104 dp, gesto y bocadillo; con 56-103, solo el gesto; la rama
    /// entra con la pantalla de victoria (la fila 0,80) y «Siguiente» sale a los 1,40 s; con 11-16 cuias, la vista y TiTino
    /// esperan a que acabe la ola de saltos [P]; con Reducir movimiento, sin animación nueva;</item>
    /// <item>el héroe con el que se decide la caja (antes de que el fondo cambie a la parada de la llegada) es el que se ve.</item>
    /// </list>
    /// Cada victoria escribe una línea <c>AF_SLOTH_BAND</c> con el alto de la franja (para DC1, que lleva las cifras a 07 §5.4).
    /// </summary>
    public sealed class SlothVictoryBandTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
        }

        [TearDown]
        public void TearDown()
        {
            SlothVictoryTests.NoSlot(false);
            MapArtRule.ForceProvisional = null;
            GameScreen.SimulatedScreen = null;
            GameScreen.VictoryViewOverride = null;
            LaunchOptions.Reset();
        }

        // ------------------------------------------------------------------------------------------- la franja (pura)

        [Test]
        public void ViewBand_IsTheTallerSideOfTheHero_OrTheWholeBandWithoutIt()
        {
            var none = new Rect(0f, 0f, 0f, 0f);
            GameScreen.SlothViewBand(60f, 400f, none, out float top, out float bottom);
            Assert.That(top, Is.EqualTo(60f), "sin héroe, la franja entera");
            Assert.That(bottom, Is.EqualTo(400f));

            var hero = new Rect(60f, 112f, 240f, 80f); // de 112 a 192
            GameScreen.SlothViewBand(60f, 400f, hero, out top, out bottom);
            Assert.That(top, Is.EqualTo(192f), "debajo del héroe (208 dp) frente a encima (52 dp)");
            Assert.That(bottom, Is.EqualTo(400f));

            GameScreen.SlothViewBand(60f, 230f, hero, out top, out bottom);
            Assert.That(top, Is.EqualTo(60f), "encima del héroe (52 dp) frente a debajo (38 dp)");
            Assert.That(bottom, Is.EqualTo(112f));

            GameScreen.SlothViewBand(60f, 400f, new Rect(60f, 420f, 240f, 80f), out top, out bottom);
            Assert.That(top, Is.EqualTo(60f), "un héroe fuera de la franja no la corta");
            Assert.That(bottom, Is.EqualTo(400f));

            GameScreen.SlothViewBand(60f, 40f, none, out top, out bottom);
            Assert.That(bottom, Is.EqualTo(top), "sin franja, vacía");
        }

        // ------------------------------------------------------------------------------------------- hitos reales y 3 filas

        [UnityTest]
        public IEnumerator RealHitos_TheSlothComesOut_AndSpeaksIfTheBandHas104([Values(40, 60, 80, 140)] int level, [Values(780, 640)] int height,
            [Values(true, false)] bool slot)
        {
            SlothVictoryTests.NoSlot(!slot);
            GameScreen.SimulatedScreen = height == 640 ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            yield return VictoryArrivalTests.Win(level);
            string at = "nivel " + level + " a 360 × " + height + (slot ? " con" : " sin") + " ranura";
            AssertBandVictory(at, true, height == 780, true, level != 40);
        }

        [UnityTest]
        public IEnumerator RealHitos_At130Percent([Values(40, 60, 80, 140)] int level, [Values(780, 640)] int height)
        {
            GameScreen.SimulatedScreen = height == 640 ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = 1.3f;
            yield return VictoryArrivalTests.Win(level);
            AssertBandVictory("nivel " + level + " a 360 × " + height + " con ranura al 130 %", true, false, false, level != 40);
        }

        [UnityTest]
        public IEnumerator ThreeRows_TheSlothComesOutWithTheView([Values(780, 640)] int height, [Values(1f, 1.3f)] float scale,
            [Values(true, false)] bool slot)
        {
            SlothVictoryTests.NoSlot(!slot);
            GameScreen.SimulatedScreen = height == 640 ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = scale;
            yield return SlothVictoryTests.WinPack(SlothVictoryTests.FifteenCuiaPack(40), 40);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            string at = "40 de 3 filas a 360 × " + height + " al " + (scale > 1.01f ? "130" : "100") + " %" + (slot ? " con" : " sin") + " ranura";
            AssertBandVictory(at, true, height == 780 && scale < 1.01f, scale < 1.01f || !slot);
        }

        // ------------------------------------------------------------------------------------------- tiempos

        [UnityTest]
        public IEnumerator ThreeRows_TheSlothWaitsForTheWave_AndEntersWithTheView()
        {
            // 15 cuias: la ola de saltos acaba a los 1.100 ms de la celebración, después de abrirse la victoria (900 ms); el tablero
            // espera para irse (M2.2) y TiTino entra con la vista, nunca sobre las cuias a la vista [P].
            SlothVictoryTests.NoSlot(true);
            yield return SlothVictoryTests.WinPack(SlothVictoryTests.FifteenCuiaPack(40), 40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothVictory sloth = view.SlothView;
            Assert.That(view.VictoryViewWaitMs, Is.GreaterThan(0f), "premisa: la ola de saltos sigue al abrirse la victoria");
            Assert.That(sloth.Visible, Is.False, "TiTino espera a la vista");
            Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.True, "lo que dirá ya está decidido (el 40 es su hito)");
            Decided decided = Decided.Of(view);
            int waited = 0;
            while (view.VictoryViewWaitMs >= 0f && waited < 1000)
            {
                Assert.That(sloth.Visible, Is.False, "a los " + waited + " ms, el tablero aún está");
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                waited += PlayModeHelpers.FrameMs;
            }

            Assert.That(view.VictoryViewWaitMs, Is.LessThan(0f));
            Assert.That(sloth.Visible, Is.True, "entra con la vista");
            Assert.That(sloth.ClockMs, Is.LessThanOrEqualTo(PlayModeHelpers.FrameMs), "su reloj arranca con la vista");
            decided.AssertSeen(view, "3 filas al empezar la vista");
            PlayModeHelpers.Step(boot, 1600);
            Assert.That(sloth.BubbleShown, Is.True, "y habla");
            Assert.That(sloth.BubbleText, Is.EqualTo(Loc.T("sloth_line_milestone_01")));
            SlothVictoryTests.AssertNoIntersection(view, "3 filas");
            decided.AssertSeen(view, "3 filas a los 1,6 s");
        }

        [UnityTest]
        public IEnumerator ThreeRows60_WaitsForTheWave_WithTheHeroItDecidedWith()
        {
            // 15 cuias en el 60 (hito con llegada y héroe): TiTino espera a la vista y el héroe con el que se decidió la caja (antes
            // de que el fondo cambie a la parada de la llegada) es el que se ve al entrar y después (revisión de DC2).
            SlothVictoryTests.NoSlot(true);
            yield return SlothVictoryTests.WinPack(SlothVictoryTests.FifteenCuiaPack(60), 60);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothVictory sloth = view.SlothView;
            Assert.That(view.Layout!.Rows, Is.EqualTo(3));
            Assert.That(view.VictoryViewWaitMs, Is.GreaterThan(0f), "premisa: la ola de saltos sigue al abrirse la victoria");
            Decided decided = Decided.Of(view);
            Assert.That(decided.Hero.width, Is.GreaterThan(0f), "premisa: el 60 trae héroe");
            Assert.That(sloth.Visible, Is.False, "TiTino espera a la vista");
            for (int t = 0; t < 1000 && view.VictoryViewWaitMs >= 0f; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.VictoryViewWaitMs, Is.LessThan(0f));
            decided.AssertSeen(view, "60 de 3 filas al empezar la vista");
            Assert.That(sloth.Visible, Is.EqualTo(decided.BoxDp >= SlothVictory.GestureBoxDp), "sale si la caja decidida da");
            PlayModeHelpers.Step(boot, 1600);
            decided.AssertSeen(view, "60 de 3 filas a los 1,6 s");
            Assert.That(view.SlothCouldSpeak, Is.EqualTo(view.CurrentVictoryLine.SlothSpeaks));
            if (view.CurrentVictoryLine.SlothSpeaks)
            {
                Assert.That(sloth.BubbleShown, Is.True, "habla con la caja con la que se decidió");
            }

            SlothVictoryTests.AssertNoIntersection(view, "60 de 3 filas");
        }

        [UnityTest]
        public IEnumerator Timing_TheBranchAtTheVictoryScreen_AndNextAt140_AsToday()
        {
            // 5 cuias: la ola ya acabó y la vista empieza al abrirse la victoria; TiTino también (la fila 0,80, 54 a).
            SlothVictoryTests.NoSlot(true);
            yield return SlothVictoryTests.WinPack(SlothVictoryTests.OneRowPack(40), 40);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothVictory sloth = view.SlothView;
            Assert.That(view.VictoryViewOn, Is.True);
            Assert.That(sloth.Visible, Is.True, "sale con la pantalla de victoria");
            Assert.That(sloth.ClockMs, Is.EqualTo(0f));
            Assert.That(view.SlothBandBottomDp, Is.EqualTo(view.VictoryPanelTopDp - 38f - GameScreen.SlothRibbonGapDp).Within(0.01f)
                .Or.EqualTo(view.SlothViewHeroRect.yMin).Within(0.01f), "la caja acaba en la cinta (o en el héroe, si va encima)");
            PlayModeHelpers.TickExact(boot, (int)GameScreen.VictoryButtonsInMs - 10);
            Assert.That(view.NextAlpha, Is.EqualTo(0f), "Siguiente aún no");
            PlayModeHelpers.TickExact(boot, 12);
            Assert.That(view.NextAlpha, Is.GreaterThan(0f), "Siguiente a los 1,40 s, como hoy");
            Assert.That(view.BoardAlpha, Is.EqualTo(0f), "el tablero ya se fue");
            Assert.That(sloth.BubbleShown, Is.True);
            SlothVictoryTests.AssertNoIntersection(view, "40");
        }

        [UnityTest]
        public IEnumerator ReduceMotion_InItsPlace_AfterTheWave()
        {
            SlothVictoryTests.NoSlot(true);
            yield return SlothVictoryTests.WinPack(SlothVictoryTests.FifteenCuiaPack(40), 40, v => GameBootstrap.Current!.Flow!.ReduceMotion = true);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothVictory sloth = view.SlothView;
            Assert.That(sloth.Visible, Is.False, "con el tablero a la vista, aún no");
            for (int t = 0; t < 1000 && view.VictoryViewWaitMs >= 0f; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.BoardAlpha, Is.EqualTo(0f), "corte, sin fundido");
            Assert.That(sloth.Visible, Is.True);
            Assert.That(sloth.RootX, Is.EqualTo(sloth.RestX).Within(0.01f), "en su sitio, sin deslizar");
            Assert.That(sloth.Rig!.ReduceMotion, Is.True);
        }

        [UnityTest]
        public IEnumerator WithoutTheView_TheBoxIsTheOneOfTi6a()
        {
            GameScreen.VictoryViewOverride = false;
            SlothVictoryTests.NoSlot(true);
            yield return SlothVictoryTests.WinPack(SlothVictoryTests.OneRowPack(40), 40);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.VictoryViewOn, Is.False);
            Assert.That(view.SlothBandTopDp, Is.EqualTo(view.SlothHeaderBottomDp).Within(0.01f), "del pie de la cabecera");
            Assert.That(view.SlothBandBottomDp, Is.LessThan(view.VictoryPanelTopDp - 38f), "a la cima de las cuias");
            Assert.That(view.SlothViewHeroRect.width, Is.EqualTo(0f));
        }

        // ------------------------------------------------------------------------------------------- utilidades

        // Avanza la victoria 1,6 s comprobando cada fotograma que TiTino no se cruza con nada, mide la franja y comprueba lo de
        // DC2: TiTino sale (si mustShow) y, con ≥ 104 dp, habla en su hito; el héroe y la caja con los que se decidió la victoria
        // (SlothVictoryShown: SlothCanSpeak y el árbitro), leídos antes del primer tick, son los que se ven en cada fotograma de la
        // vista (con withHero, el héroe no es vacío: un hito con llegada y héroe).
        private static void AssertBandVictory(string at, bool hito, bool mustSpeak, bool mustShow = true, bool withHero = false)
        {
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothVictory sloth = view.SlothView;
            Decided decided = Decided.Of(view);
            if (withHero)
            {
                Assert.That(decided.Hero.width, Is.GreaterThan(0f), at + ": premisa: el hito trae héroe");
            }

            for (int ms = 0; ms <= 1600; ms += PlayModeHelpers.FrameMs)
            {
                SlothVictoryTests.AssertNoIntersection(view, at + " a los " + ms + " ms");
                AssertClearOfHeroAndRibbon(view, at + " a los " + ms + " ms");
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                if (view.VictoryViewWaitMs < 0f)
                {
                    decided.AssertSeen(view, at + " a los " + ms + " ms"); // la caja no se mueve durante la victoria
                }
            }

            VictoryLine line = view.CurrentVictoryLine;
            Rect hero = view.HeroRect;
            float header = view.SlothHeaderBottomDp;
            float ribbon = view.VictoryPanelTopDp - 38f;
            float band = ribbon - GameScreen.SlothRibbonGapDp - header;
            GameScreen.SlothViewBand(header, ribbon - 4f, hero, out float t4, out float b4);
            GameScreen.SlothViewBand(header, ribbon, hero, out float t0, out float b0);
            string report = "AF_SLOTH_BAND " + at + ": franja " + SlothVictoryTests.F(header) + " → "
                + SlothVictoryTests.F(ribbon - GameScreen.SlothRibbonGapDp) + " (" + SlothVictoryTests.F(band) + " dp), héroe "
                + (hero.height > 0f ? SlothVictoryTests.F(hero.yMin) + " → " + SlothVictoryTests.F(hero.yMax) : "no") + ", caja "
                + SlothVictoryTests.F(view.SlothBandTopDp) + " → " + SlothVictoryTests.F(view.SlothBandBottomDp) + " (" + SlothVictoryTests.F(view.SlothBoxDp)
                + " dp; con aire 4: " + SlothVictoryTests.F(b4 - t4) + ", con aire 0: " + SlothVictoryTests.F(b0 - t0) + "), " + view.Layout!.Rows
                + " filas, TiTino " + sloth.Mode + " a " + SlothVictoryTests.F(sloth.SizeDp) + " dp (rama en " + SlothVictoryTests.F(sloth.RootY)
                + "), habla " + line.SlothSpeaks + ", dato de llegada " + line.ArrivalFact + ", bocadillo "
                + (sloth.HasBubble ? sloth.BubbleLines + " líneas a " + SlothVictoryTests.F(sloth.BubbleFontDp) + " dp" : "no");
            TestContext.WriteLine(report);
            Debug.Log(report);

            Assert.That(view.VictoryViewOn, Is.True, at + ": la victoria con vista");
            Assert.That(view.BoardAlpha, Is.EqualTo(0f), at + ": el tablero ya se fue");
            decided.AssertSeen(view, at + " a los 1,6 s");
            if (mustShow)
            {
                Assert.That(view.SlothBoxDp, Is.GreaterThanOrEqualTo(SlothVictory.GestureBoxDp), at + ": la franja da para TiTino");
                Assert.That(sloth.Visible, Is.True, at + ": TiTino sale");
            }
            else
            {
                // Al 130 % con la ranura, la cinta sube y, con héroe, la franja que queda no siempre da para TiTino: no sale y no
                // tapa el héroe (medidas de DC2).
                Assert.That(sloth.Visible, Is.EqualTo(view.SlothBoxDp >= SlothVictory.GestureBoxDp), at + ": sale si la caja da");
                if (!sloth.Visible)
                {
                    Assert.That(line.SlothSpeaks, Is.False, at);
                    Assert.That(line.ArrivalFact, Is.True, at + ": sin TiTino, el hito lleva su dato");
                    return;
                }
            }

            Assert.That(view.SlothCouldSpeak, Is.EqualTo(line.SlothSpeaks), at + ": SlothCanSpeak decide la línea (SlothHito)");
            if (hito)
            {
                Assert.That(sloth.Pose, Is.EqualTo(SlothPoseId.CheerBig), at + ": sloth_cheer_big en sus hitos");
                if (view.SlothBoxDp >= GameScreen.SlothSpeakBoxDp && line.SlothSpeaks)
                {
                    Assert.That(sloth.Mode, Is.EqualTo(SlothVictoryMode.Speak), at);
                    Assert.That(sloth.BubbleShown, Is.True, at + ": gesto y bocadillo");
                    Assert.That(line.ArrivalFact, Is.False, at + ": la llegada va sin dato (el dato, al 10k+1)");
                    Assert.That(line.FruitKey, Is.Null, at + ": una sola línea");
                }
                else if (!line.SlothSpeaks)
                {
                    Assert.That(line.ArrivalFact, Is.True, at + ": si TiTino no habla, el hito lleva su dato");
                }
            }

            if (mustSpeak)
            {
                Assert.That(view.SlothBoxDp, Is.GreaterThanOrEqualTo(GameScreen.SlothSpeakBoxDp), at + ": la franja mide ≥ 104 dp");
                Assert.That(line.SlothSpeaks, Is.True, at + ": TiTino habla en su hito");
                Assert.That(sloth.BubbleShown, Is.True, at);
            }

            // TiTino cuelga arriba de su caja, como en TI6a (SlothVictory.Place): con la vista, bajo la cabecera o bajo el héroe.
            Assert.That(sloth.RootY - (SlothVictory.RigUpS * sloth.SizeDp), Is.EqualTo(view.SlothBandTopDp + SlothVictory.InsetDp).Within(0.6f),
                at + ": TiTino arriba de su caja");
        }

        // Héroe y caja con los que se decidió la victoria (GameScreen.SlothDecided*), leídos antes del primer tick.
        private readonly struct Decided
        {
            public readonly Rect Hero;
            public readonly float Top;
            public readonly float Bottom;

            private Decided(Rect hero, float top, float bottom)
            {
                Hero = hero;
                Top = top;
                Bottom = bottom;
            }

            public float BoxDp => Bottom - Top;

            public static Decided Of(GameScreen view)
            {
                return new Decided(view.SlothDecidedHeroRect, view.SlothDecidedBandTopDp, view.SlothDecidedBandBottomDp);
            }

            // Con la vista ya empezada, el héroe que se ve (el del fondo) es el decidido y la caja de TiTino es la decidida.
            public void AssertSeen(GameScreen view, string at)
            {
                Rect seen = view.HeroRect;
                string heroes = ": el héroe que se ve " + seen + " es el decidido " + Hero;
                Assert.That(seen.xMin, Is.EqualTo(Hero.xMin).Within(0.01f), at + heroes);
                Assert.That(seen.yMin, Is.EqualTo(Hero.yMin).Within(0.01f), at + heroes);
                Assert.That(seen.width, Is.EqualTo(Hero.width).Within(0.01f), at + heroes);
                Assert.That(seen.height, Is.EqualTo(Hero.height).Within(0.01f), at + heroes);
                Assert.That(view.SlothBandTopDp, Is.EqualTo(Top).Within(0.01f), at + ": la caja que se ve es la decidida (arriba)");
                Assert.That(view.SlothBandBottomDp, Is.EqualTo(Bottom).Within(0.01f), at + ": la caja que se ve es la decidida (abajo)");
                if (view.SlothView.Visible)
                {
                    Assert.That(view.SlothView.BoxTop, Is.EqualTo(Top).Within(0.01f), at + ": TiTino en la caja decidida (arriba)");
                    Assert.That(view.SlothView.BoxBottom, Is.EqualTo(Bottom).Within(0.01f), at + ": TiTino en la caja decidida (abajo)");
                }
            }
        }

        // TiTino (sus piezas y el bocadillo) fuera del héroe del sitio y de la cinta (con la escala a la que esté).
        private static void AssertClearOfHeroAndRibbon(GameScreen view, string at)
        {
            SlothVictory sloth = view.SlothView;
            if (!sloth.Visible)
            {
                return;
            }

            var mine = new List<Rect>();
            sloth.CollectRects(sloth.Layer, mine, true);
            Rect hero = view.HeroRect;
            Rect ribbon = VictorySetup.CanvasRect(view.VictoryRibbonRect, sloth.Layer);
            foreach (Rect m in mine)
            {
                if (m.width <= 0.01f || m.height <= 0.01f)
                {
                    continue;
                }

                if (hero.width > 0f && hero.height > 0f)
                {
                    Assert.That(m.Overlaps(hero), Is.False, at + ": TiTino " + m + " ∩ héroe " + hero);
                }

                Assert.That(m.Overlaps(ribbon), Is.False, at + ": TiTino " + m + " ∩ cinta " + ribbon);
            }
        }

        /// <summary>Mide y comprueba una victoria ya abierta (las capturas de <see cref="SlothVictoryBandSnapshotTests"/>).</summary>
        internal static void AssertBand(string at, bool mustSpeak, bool mustShow)
        {
            AssertBandVictory(at, true, mustSpeak, mustShow);
        }
    }

    /// <summary>
    /// Capturas de TiTino en la victoria con vista (tarea DC2; pregunta 53 a de D-028), en <c>Logs/snapshots/es-419/</c>, a los
    /// 1,60 s de abrirse la pantalla de victoria (la fila 2,40), con el bocadillo a la vista si habla; las de 640, a 1080 × 1920 px
    /// (16:9, solo la pantalla simulada):
    /// <list type="bullet">
    /// <item><c>dc2_real_l{40|60|80|140}_{780|640}</c>: los hitos del catálogo real con la ranura del Dato del río, y
    /// <c>…_sinranura</c>, sin ella;</item>
    /// <item><c>dc2_3filas_{780|640}</c>: un tablero de 3 filas (el 40 con 15 cuias), con la ranura, y <c>…_sinranura</c>, sin
    /// ella;</item>
    /// <item><c>dc2_real_l{40|60}_{780|640}_130</c>: el 40 (sin héroe) y el 60 (con héroe; TiTino no cabe) al 130 %.</item>
    /// </list>
    /// Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter AF.Tests.PlayMode.SlothVictoryBandSnapshotTests</c>
    /// sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de TiTino en la victoria con vista (DC2); necesitan dispositivo gráfico.")]
    public sealed class SlothVictoryBandSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
        }

        [TearDown]
        public void TearDown()
        {
            SlothVictoryTests.NoSlot(false);
            SnapshotKit.Release();
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureRealHitos()
        {
            foreach (bool slot in new[] { true, false })
            {
                foreach (bool small in new[] { false, true })
                {
                    foreach (int level in new[] { 40, 60, 80, 140 })
                    {
                        SlothVictoryTests.NoSlot(!slot);
                        string name = "dc2_real_l" + level + "_" + (small ? "640" : "780") + (slot ? string.Empty : "_sinranura");
                        yield return Capture(name, null, level, small, 1f, !small);
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator CaptureThreeRowsAnd130()
        {
            foreach (bool small in new[] { false, true })
            {
                SlothVictoryTests.NoSlot(false);
                yield return Capture("dc2_3filas_" + (small ? "640" : "780"), SlothVictoryTests.FifteenCuiaPack(40), 40, small, 1f, !small);
                SlothVictoryTests.NoSlot(true);
                yield return Capture("dc2_3filas_" + (small ? "640" : "780") + "_sinranura", SlothVictoryTests.FifteenCuiaPack(40), 40, small, 1f, false);
                SlothVictoryTests.NoSlot(false);
                yield return Capture("dc2_real_l40_" + (small ? "640" : "780") + "_130", null, 40, small, 1.3f, false);
                yield return Capture("dc2_real_l60_" + (small ? "640" : "780") + "_130", null, 60, small, 1.3f, false);
            }
        }

        private static IEnumerator Capture(string name, string? json, int level, bool small, float scale, bool mustSpeak)
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = scale;
            if (json != null)
            {
                yield return SlothVictoryTests.WinPack(json, level);
            }
            else
            {
                yield return VictoryArrivalTests.Win(level);
            }

            SnapshotKit.Attach(GameBootstrap.Current!);
            SlothVictoryBandTests.AssertBand(name, mustSpeak, scale < 1.01f);
            yield return SnapshotKit.Wait(0.2f);

            // A 360 × 640 (1080 × 1920 px, 16:9), solo la pantalla simulada: GameScreen la maqueta arriba de la captura y lo de
            // abajo es fondo que en ese teléfono no existe (revisión de DC2; como FX5).
            SnapshotKit.SaveTop(dir, name, small ? (int)HudLabelTests.Screen640.y : SnapshotKit.Height);
            SnapshotKit.Release();
            LaunchOptions.Reset();
        }
    }
}
