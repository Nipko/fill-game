#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.Localization;
using AF.Platform;
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
    /// <item>el héroe con el que se decide la caja (antes de que el fondo cambie a la parada de la llegada) es el que se ve;</item>
    /// <item>B1b-Q10 (c), decisión del dueño (D-033, añadido del 2026-10-20): en sus hitos, si con las barras de la Crecida y la
    /// Canasta su frase no cabe y sin ellas sí, la victoria va sin barras y TiTino habla (D-016); la Canasta del 10k se abre igual
    /// y su contenido vuela al saldo, que lo enseña al llegar (D-011), y la Crecida avanza aunque no se dibuje. Si ni sin barras
    /// cabe, se quedan (<see cref="GameScreen.VictoryBarsYieldTried"/>).</item>
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
            GameBootstrap.ResetStreakOverrides();
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

            // En 16:9 con la ranura del Dato del río, desde DC3 (lienzo de 360 × 640 dp; D-028) la franja mide 44 dp en el 40 y
            // 0 en los hitos con héroe (60, 80 y 140), menos que los 56 del gesto: TiTino no sale y el hito lleva su dato (la regla
            // de DC2). Pendiente del dueño (open issue de DC3).
            AssertBandVictory(at, true, height == 780, height == 780 || !slot, level != 40);
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
            AssertBandVictory(at, true, height == 780 && scale < 1.01f, (scale < 1.01f && height == 780) || !slot); // 640 con ranura: DC3
        }

        // ------------------------------------------------------------------------------------------- B1b-Q10 (c)

        [UnityTest]
        public IEnumerator TitinoHitos_WithoutBars_WhenHisLineDoesNotFitWithThem([Values(60, 80, 140)] int level, [Values(780, 640)] int height,
            [Values(1f, 1.3f)] float scale)
        {
            // Con la ranura del Dato del río (el default) y la Crecida en 4 (la victoria la deja en 5, sin hito): a 360 × 780 al
            // 100 %, con las barras la franja de TiTino bajaba de 124 a 56 dp (< 104) en los tres; desde B1b-Q10 (c) la victoria va
            // sin barras y TiTino habla. Donde ni sin barras cabe su frase (16:9 con la ranura; 130 % con el héroe), se quedan.
            UseStreak(4);
            GameScreen.SimulatedScreen = height == 640 ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = scale;
            yield return VictoryArrivalTests.Win(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            VictoryBars bars = view.VictoryBarsView;
            string at = "nivel " + level + " a 360 × " + height + " al " + (scale > 1.01f ? "130" : "100") + " % con ranura";
            Assert.That(view.VictoryBarsPossible, Is.True, at + ": premisa: la campaña lleva barras");
            Assert.That(bars.Plan.Streak && bars.Plan.Basket, Is.True, at + ": premisa: la Crecida y la Canasta");
            Assert.That(bars.Plan.BasketOpens, Is.True, at + ": la Canasta del " + level + " se abre igual, con barras o sin ellas");
            StreakSnapshot? after = boot.Flow!.PeekStreak();
            Assert.That(after.HasValue && after.Value.Wins == 5, Is.True, at + ": la Crecida avanza (4 → 5) aunque su barra no se dibuje");
            bool main = height == 780 && scale < 1.01f;
            if (main)
            {
                Assert.That(view.VictoryBarsYielded, Is.True, at + ": con las barras no cabía su frase; sin ellas sí (B1b-Q10 c)");
            }

            int paid = bars.Plan.PaidSeeds;
            Assert.That(paid, Is.GreaterThan(0), at + ": premisa: la Canasta paga semillas");
            int wallet = boot.Services!.Wallet.Seeds;
            if (view.VictoryBarsYielded)
            {
                Assert.That(view.CurrentVictoryLine.SlothSpeaks, Is.True, at + ": sin barras, TiTino habla");
                Assert.That(bars.Root.gameObject.activeSelf, Is.True, at + ": la secuencia sigue (Barless)");
                Assert.That(bars.StreakRect.gameObject.activeSelf || bars.BasketRect.gameObject.activeSelf, Is.False, at + ": sin filas");
                Assert.That(bars.Height, Is.EqualTo(0f), at + ": las barras no ocupan alto");
            }

            // La Canasta se abre a los 1.200 ms y su contenido vuela al saldo y a la bandeja; el saldo enseña sus semillas al llegar.
            int flying = 0;
            for (int ms = 0; ms <= 1700; ms += PlayModeHelpers.FrameMs)
            {
                if (ms < (int)VictoryBars.BasketOpenMs)
                {
                    Assert.That(view.WalletBar.SeedsShown, Is.LessThanOrEqualTo(wallet - paid), at + " a los " + ms + " ms: lo de la Canasta aún no se ve (D-011)");
                }

                flying = Mathf.Max(flying, bars.FlyingCount);
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                int t = ms + PlayModeHelpers.FrameMs;
                if (t >= (int)VictoryBars.BasketOpenMs && t < (int)(VictoryBars.BasketOpenMs + VictoryBars.FlyMs))
                {
                    // Mientras la Canasta se abre (con barras, la tira salta en lugar de su línea): sin barras, la tira no se ve.
                    Assert.That(bars.Bursting, Is.EqualTo(!view.VictoryBarsYielded), at + " a los " + t + " ms: la tira, solo con barras");
                }
            }

            Assert.That(flying, Is.GreaterThan(0), at + ": su contenido vuela");
            Assert.That(bars.Pending, Is.False, at);
            Assert.That(bars.PendingSeeds, Is.EqualTo(0), at);
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(boot.Services.Wallet.Seeds), at + ": ya se ve todo lo cobrado");
            VictoryLine line = view.CurrentVictoryLine;
            TestContext.WriteLine(at + ": barras " + (view.VictoryBarsYielded ? "cedidas a TiTino" : view.VictoryBarsYieldTried ? "probadas sin ellas, se quedan" : "con TiTino")
                + ", caja " + SlothVictoryTests.F(view.SlothBoxDp) + " dp, habla " + line.SlothSpeaks + ", dato de llegada " + line.ArrivalFact);
            AssertBarsYieldRule(view, line, level, at);
            if (main)
            {
                Assert.That(view.SlothBoxDp, Is.GreaterThanOrEqualTo(GameScreen.SlothSpeakBoxDp), at + ": la franja vuelve a medir ≥ 104 dp");
                Assert.That(view.SlothView.BubbleShown, Is.True, at + ": dice su frase");
                Assert.That(view.SlothView.Mode, Is.EqualTo(SlothVictoryMode.Speak), at);
                Assert.That(line.ArrivalFact, Is.False, at + ": la llegada va sin dato (el dato, al 10k+1; T6-A)");
            }

            Fx16bTests.AssertCompactVictory(view, at);
        }

        [UnityTest]
        public IEnumerator NotATitinoHito_TheBarsStay()
        {
            // El 70 (10k sin hito de TiTino) a 360 × 780 con la ranura: lo de B1b, con barras (la Canasta se abre en su barra).
            UseStreak(4);
            yield return VictoryArrivalTests.Win(70);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(VictoryLineArbiter.IsSlothHito(70), Is.False, "premisa");
            Assert.That(view.VictoryBarsYielded || view.VictoryBarsYieldTried, Is.False, "fuera de sus hitos no se prueba");
            Assert.That(view.VictoryBarsView.Height, Is.GreaterThan(0f), "las barras, en el panel");
            Assert.That(view.VictoryBarsView.BasketRect.gameObject.activeSelf, Is.True);
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
            AssertBarsYieldRule(view, line, GameBootstrap.Current!.Flow!.LevelNumber, at);
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
                // B1b-Q10 (c), decisión del dueño: si con las barras de la Crecida y la Canasta (D-033, punto 10) su frase no cabe, la
                // victoria de su hito va sin ellas y TiTino habla, como antes de B1b (a 360 × 780 con la ranura: 60, 80 y 140).
                Assert.That(view.SlothBoxDp, Is.GreaterThanOrEqualTo(GameScreen.SlothSpeakBoxDp), at + ": la franja mide ≥ 104 dp");
                Assert.That(line.SlothSpeaks, Is.True, at + ": TiTino habla en su hito");
                Assert.That(sloth.BubbleShown, Is.True, at);
            }

            // TiTino cuelga arriba de su caja, como en TI6a (SlothVictory.Place): con la vista, bajo la cabecera o bajo el héroe.
            Assert.That(sloth.RootY - (SlothVictory.RigUpS * sloth.SizeDp), Is.EqualTo(view.SlothBandTopDp + SlothVictory.InsetDp).Within(0.6f),
                at + ": TiTino arriba de su caja");
        }

        // B1b-Q10 (c): en un hito de TiTino con voz, si las barras ocupan el panel es que TiTino habla con ellas o que se probó
        // quitarlas y ni así cabía su frase; si se quitaron, habla y no queda ninguna fila.
        private static void AssertBarsYieldRule(GameScreen view, VictoryLine line, int level, string at)
        {
            VictoryBars bars = view.VictoryBarsView;
            if (view.VictoryBarsYielded)
            {
                Assert.That(VictoryLineArbiter.IsSlothHito(level), Is.True, at + ": solo en sus hitos");
                Assert.That(line.SlothSpeaks, Is.True, at + ": sin barras, TiTino habla (B1b-Q10 c)");
                Assert.That(bars.StreakRect.gameObject.activeSelf || bars.BasketRect.gameObject.activeSelf, Is.False, at + ": sin filas");
                Assert.That(bars.Height, Is.EqualTo(0f), at);
                return;
            }

            if (view.VictoryBarsYieldTried)
            {
                // Se probó sin barras y ni así cabía su frase: las barras vuelven enteras, con sus filas.
                Assert.That(bars.StreakRect.gameObject.activeSelf, Is.EqualTo(bars.Plan.Streak), at + ": la Crecida, de vuelta");
                Assert.That(bars.BasketRect.gameObject.activeSelf, Is.EqualTo(bars.Plan.Basket), at + ": la Canasta, de vuelta");
                Assert.That(bars.Height, Is.GreaterThan(0f), at + ": las barras, de vuelta en el panel");
            }

            bool barsShown = bars.Root.gameObject.activeSelf && bars.Height > 0f;
            if (barsShown && VictoryLineArbiter.IsSlothHito(level) && view.SlothVoice && view.SlothEnabled && !line.SlothSpeaks)
            {
                Assert.That(view.VictoryBarsYieldTried, Is.True, at + ": con barras no habla: se probó sin ellas y tampoco cabía (B1b-Q10 c)");
            }
        }

        /// <summary>La Crecida en <paramref name="wins"/> con el reloj fijo del 2026-10-08, 10:00 en Bogotá (también las capturas).</summary>
        internal static void UseStreak(int wins)
        {
            var clock = new ManualClock(new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.StreakStoreOverride = new InMemoryStreakStore(clock,
                new StreakRecord { DateLocal = "2026-10-08", Wins = wins, ShieldAvailable = true });
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
            GameBootstrap.ResetStreakOverrides();
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
                        yield return Capture(name, null, level, small, 1f, !small, !small || !slot); // 640 con ranura: DC3
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
                yield return Capture("dc2_3filas_" + (small ? "640" : "780"), SlothVictoryTests.FifteenCuiaPack(40), 40, small, 1f, !small, !small);
                SlothVictoryTests.NoSlot(true);
                yield return Capture("dc2_3filas_" + (small ? "640" : "780") + "_sinranura", SlothVictoryTests.FifteenCuiaPack(40), 40, small, 1f, false, true);
                SlothVictoryTests.NoSlot(false);
                yield return Capture("dc2_real_l40_" + (small ? "640" : "780") + "_130", null, 40, small, 1.3f, false, false);
                yield return Capture("dc2_real_l60_" + (small ? "640" : "780") + "_130", null, 60, small, 1.3f, false, false);
            }
        }

        /// <summary>
        /// B1b-Q10 (c): el hito 60 a 360 × 780 con la ranura y la Crecida en 4, sin barras: <c>q10_l60_780_vuelo</c> a los 1,30 s
        /// (la Canasta abierta y su contenido volando al saldo y a la bandeja) y <c>q10_l60_780</c> a los 1,60 s (TiTino con su
        /// frase).
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureHito60WithoutBars()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            SlothVictoryBandTests.UseStreak(4);
            yield return VictoryArrivalTests.Win(60);
            GameBootstrap boot = GameBootstrap.Current!;
            SnapshotKit.Attach(boot);
            Assert.That(boot.View!.VictoryBarsYielded, Is.True, "premisa: el 60 va sin barras");
            PlayModeHelpers.TickExact(boot, 1300);
            Assert.That(boot.View.VictoryBarsView.FlyingCount, Is.GreaterThan(0), "a los 1,30 s, lo de la Canasta vuela");
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "q10_l60_780_vuelo");
            PlayModeHelpers.TickExact(boot, 300);
            Assert.That(boot.View.SlothView.BubbleShown, Is.True, "a los 1,60 s, TiTino con su frase");
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, "q10_l60_780");
        }

        private static IEnumerator Capture(string name, string? json, int level, bool small, float scale, bool mustSpeak, bool mustShow)
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
            SlothVictoryBandTests.AssertBand(name, mustSpeak, mustShow);
            yield return SnapshotKit.Wait(0.2f);

            // A 360 × 640 (1080 × 1920 px, 16:9), solo la pantalla simulada: GameScreen la maqueta arriba de la captura y lo de
            // abajo es fondo que en ese teléfono no existe (revisión de DC2; como FX5).
            SnapshotKit.SaveTop(dir, name, small ? (int)HudLabelTests.Screen640.y : SnapshotKit.Height);
            SnapshotKit.Release();
            LaunchOptions.Reset();
        }
    }
}
