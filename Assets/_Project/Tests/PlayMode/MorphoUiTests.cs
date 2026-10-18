#nullable enable
using System;
using System.Collections;
using System.Threading;
using AF.Ads;
using AF.Boosters;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La interfaz de Morpho · Pista (tarea MO2; 04 §2.3; 07 §7.4 y §7.6; 02 T5; D-019; D-002): la ranura de la bandeja en el
    /// orden canónico con 3, 4 y 5 ranuras, su insignia (stock, precio o video) y «Te faltan {n}», la mariposa que vuela al origen
    /// en 0,4 s y salta al destino, los anillos de 3 s que se apagan al verter otra cosa, Reducir movimiento sin vuelo, la tarjeta
    /// de las respuestas sin vertido (Deshacer y Volver a la orilla, sin cobrar) y el globo «La morpho no conoce este tramo
    /// todavía». Los textos, a 12 sp o más y sin solapes.
    /// </summary>
    public sealed class MorphoUiTests
    {
        private const int Level = 15;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Tray_CanonicalOrder_With3And4Slots()
        {
            // 11: aún sin Pista (hint_unlock_level = 12): Marea y Cuia extra.
            yield return LoadPlaying(11, FunnelSetup.Profile(500));
            BoosterTray tray = GameBootstrap.Current!.View!.Tray;
            Assert.That(tray.HintShown, Is.False, "antes del 12 la ranura no existe");
            Assert.That(tray.SlotCount, Is.EqualTo(2));

            // 15: 3 ranuras de 64 dp, Pista la 3.ª tras Cuia extra, la bandeja centrada (16 + 3·64 + 2·8 + 16 = 240).
            using (var layout = new AfLayoutLog())
            {
                yield return LoadPlaying(Level, FunnelSetup.Profile(500));
                GameScreen view = GameBootstrap.Current!.View!;
                tray = view.Tray;
                AssertSlots(view, tray.UndoButton, tray.ExtraCuiaButton, tray.HintButton);
                Assert.That(BoosterTray.PositionOf(TraySlot.Hint, tray.ActiveMask), Is.EqualTo(2));
                Assert.That(tray.HintLabel.text, Is.EqualTo("Pista"), "glosario §10");
                Assert.That(tray.HintGlyph.sprite.name, Is.EqualTo("icon_morpho"), "07 §7.6: la mariposa azul sobre dos cuias");
                Assert.That(tray.HintPriceShown, Is.EqualTo(100), "hint_seed_price");
                string level = layout.RequireLast("level", Level, "nivel 15");
                Assert.That(AfLayoutLog.ButtonIds(level), Does.Contain("hint"), level);
                AfLayoutLog.AssertCenterInside(level, "hint", (RectTransform)tray.HintButton.transform);
            }

            // 100 (Aguas turbias): 4 ranuras de 64 dp; Revelar va detrás de Pista (pregunta 29; D-020).
            yield return LoadPlaying(100, FunnelSetup.Profile(500));
            GameScreen murky = GameBootstrap.Current!.View!;
            tray = murky.Tray;
            AssertSlots(murky, tray.UndoButton, tray.ExtraCuiaButton, tray.HintButton, tray.RevealButton);
            Assert.That(BoosterTray.PositionOf(TraySlot.Reveal, tray.ActiveMask), Is.EqualTo(3));
        }

        [Test]
        public void TrayOrder_With5Slots_IsCanonical_And56dp()
        {
            // Con los cinco comodines (Pororoca llega en la tanda siguiente): Marea, Cuia extra, Pista, Pororoca y Revelar, de
            // 56 dp (07 §7.4); sin alguno, las demás conservan el orden relativo y no dejan hueco.
            int all = 0b11111;
            Assert.That(BoosterTray.PositionOf(TraySlot.Undo, all), Is.EqualTo(0));
            Assert.That(BoosterTray.PositionOf(TraySlot.ExtraCuia, all), Is.EqualTo(1));
            Assert.That(BoosterTray.PositionOf(TraySlot.Hint, all), Is.EqualTo(2));
            Assert.That(BoosterTray.PositionOf(TraySlot.Shuffle, all), Is.EqualTo(3));
            Assert.That(BoosterTray.PositionOf(TraySlot.Reveal, all), Is.EqualTo(4), "Revelar, la última (pregunta 29)");
            Assert.That(BoosterTray.BaseSlotDpFor(5), Is.EqualTo(56f));
            Assert.That(BoosterTray.BaseSlotDpFor(4), Is.EqualTo(64f));
            Assert.That(BoosterTray.BaseSlotDpFor(3), Is.EqualTo(64f));
            Assert.That(BoosterTray.WidthFor(5), Is.LessThanOrEqualTo(360f - (2f * BoosterTray.TrayEdgeDp)), "cabe a 360 dp");

            int noExtra = all & ~(1 << (int)TraySlot.ExtraCuia) & ~(1 << (int)TraySlot.Shuffle);
            Assert.That(BoosterTray.PositionOf(TraySlot.Hint, noExtra), Is.EqualTo(1));
            Assert.That(BoosterTray.PositionOf(TraySlot.Reveal, noExtra), Is.EqualTo(2));
            Assert.That(BoosterTray.PositionOf(TraySlot.ExtraCuia, noExtra), Is.EqualTo(-1));
        }

        [UnityTest]
        public IEnumerator Badge_Stock_Price_Video_AndMissing()
        {
            // Stock: «+3» ámbar, sin precio; se gasta antes que las semillas (04 §1.1).
            Profile stocked = FunnelSetup.Profile(500);
            stocked.Stock.Hint = 3;
            yield return LoadPlaying(Level, stocked);
            GameBootstrap boot = GameBootstrap.Current!;
            BoosterTray tray = boot.View!.Tray;
            Assert.That(tray.HintBadgeText, Is.EqualTo("+3"));
            Assert.That(tray.HintPriceShown, Is.EqualTo(0));
            yield return PressAndWait(boot);
            Assert.That(boot.Flow!.LastHint!.Value.Answer, Is.EqualTo(HintAnswer.Pour));
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Hint), Is.EqualTo(2), "del stock");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(500), "sin semillas");

            // Sin stock ni saldo y con video: «+1» con el icono; tocarla ve el video, suma la unidad y la gasta en la pista.
            yield return LoadPlaying(Level, FunnelSetup.Profile(0));
            boot = GameBootstrap.Current!;
            tray = boot.View!.Tray;
            Assert.That(tray.HintShowsVideo, Is.True, "rv_hint");
            Assert.That(tray.HintBadgeText, Is.EqualTo("+1"));
            Assert.That(boot.PressHint(), Is.True);
            yield return WaitAnswer(boot);
            Assert.That(boot.Flow!.LastHint!.Value.Charged, Is.True);
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Hint), Is.EqualTo(0), "la unidad del video, gastada");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(0));

            // Sin stock, saldo ni video: el precio en rojo y «Te faltan 50» encima, que late al tocarla; nada se pide ni se cobra.
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            yield return LoadPlaying(Level, FunnelSetup.Profile(50));
            boot = GameBootstrap.Current!;
            tray = boot.View!.Tray;
            Assert.That(tray.HintPriceShown, Is.EqualTo(100));
            Assert.That(tray.HintShowsMissing, Is.True);
            Assert.That(tray.HintMissingText, Is.EqualTo("Te faltan 50"));
            Assert.That(boot.PressHint(), Is.False);
            Assert.That(tray.HintMissingPulsing, Is.True, "responde la burbuja (pregunta 48 (b))");
            Assert.That(boot.Flow!.HintThinking, Is.False);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(50));
        }

        [UnityTest]
        public IEnumerator Pour_ButterflyFlies400ms_ThenBothCuiasRing_AndOtherPourTurnsThemOff()
        {
            yield return LoadPlaying(Level, FunnelSetup.Profile(500));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            MorphoFx fx = view.MorphoFx;
            yield return PressAndWait(boot);
            Pour hint = boot.Flow!.ActiveHint;
            Assert.That(fx.Active && fx.Flying, Is.True, "despega del botón");
            Assert.That(fx.Source, Is.EqualTo(hint.Source));
            Assert.That(fx.Target, Is.EqualTo(hint.Target));
            Assert.That(fx.SourceRinging || fx.TargetRinging, Is.False, "aún en vuelo");

            // Revisión de MO2: despega con la envergadura del glifo de la ranura (28 dp), con su halo claro.
            Assert.That(fx.ButterflySpanDp, Is.EqualTo(MorphoFx.TakeoffSpanDp).Within(0.5f), "despega con la envergadura del glifo");
            PlayModeHelpers.TickExact(boot, 100);
            Assert.That(fx.WingsHalo.color.a, Is.GreaterThan(0.5f), "halo claro");
            Assert.That(fx.ButterflySpanDp, Is.InRange(MorphoFx.PerchSpanDp, MorphoFx.TakeoffSpanDp), "baja de 28 a 24 dp en el vuelo");

            // 0,4 s: se posa en el origen, que late con su anillo; la estela del vuelo se ve aún (hasta 14 puntos que se apagan),
            // como un trazo (revisión de MO2: un punto cada ≤ 8 dp recorridos, no motas sueltas) y detrás de las cuias.
            PlayModeHelpers.TickExact(boot, (int)MorphoFx.FlightMs - 100 + 4);
            Assert.That(fx.Flying, Is.False);
            Assert.That(fx.SourceRinging, Is.True, "el origen late");
            Assert.That(fx.TargetRinging, Is.False);
            Assert.That(fx.ButterflySpanDp, Is.EqualTo(MorphoFx.PerchSpanDp).Within(0.01f), "posada, 24 dp");
            Assert.That(fx.TrailVisible, Is.InRange(6, MorphoFx.TrailCount), "la estela del vuelo");
            Assert.That(fx.TrailNewestAlpha, Is.InRange(0.5f, MorphoFx.TrailAlpha + 0.001f), "el punto más nuevo, casi a 0,8");
            Assert.That(fx.TrailLargestStepDp, Is.InRange(0.01f, MorphoFx.TrailEveryDp + 0.01f), "un punto cada ≤ 8 dp");
            Assert.That(fx.TrailRoot.parent, Is.SameAs(view.MechanicBackLayer), "la estela, en la capa de mecánicas trasera");
            Assert.That(view.MechanicBackLayer.GetSiblingIndex(), Is.LessThan(view.Views[0].Root.GetSiblingIndex()), "detrás de las cuias");
            Assert.That(fx.Root.parent, Is.SameAs(view.MechanicFrontLayer), "la mariposa, delante");

            // Salta al destino con su estela; las dos cuias laten. La mariposa nunca baja de 22 dp.
            PlayModeHelpers.TickExact(boot, (int)(MorphoFx.PerchMs + (MorphoFx.HopMs * 0.9f)));
            Assert.That(fx.Hopping, Is.True);
            Assert.That(fx.TrailVisible, Is.InRange(6, MorphoFx.TrailCount), "la estela del salto");
            Assert.That(fx.TrailLargestStepDp, Is.LessThanOrEqualTo(MorphoFx.TrailEveryDp + 0.01f), "un punto cada ≤ 8 dp");
            Assert.That(fx.ButterflySpanDp, Is.GreaterThanOrEqualTo(MorphoFx.MinSpanDp));
            PlayModeHelpers.TickExact(boot, (int)(MorphoFx.HopMs * 0.1f) + 8);
            Assert.That(fx.OnTarget && fx.SourceRinging && fx.TargetRinging, Is.True, "las dos laten");
            Assert.That(fx.SparklesRequested, Is.GreaterThan(0), "estela de destellos azules");

            // Elegir el origen no la apaga (es una ayuda explícita: D-002 no la quita).
            Assert.That(boot.TapCuia(hint.Source).Kind, Is.EqualTo(TapKind.Selected));
            PlayModeHelpers.Step(boot, 100);
            Assert.That(fx.SourceRinging && fx.TargetRinging, Is.True);
            Assert.That(boot.TapCuia(hint.Source).Kind, Is.EqualTo(TapKind.Deselected));

            // Verter otra cosa: los anillos se apagan (04 §2.3 d).
            Pour other = OtherLegalPour(boot.Flow.Session.Current, hint);
            Assert.That(boot.TapCuia(other.Source).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(other.Target).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
            Assert.That(boot.Flow.HasActiveHint, Is.False);
            PlayModeHelpers.TickExact(boot, (int)MorphoFx.FadeMs + PlayModeHelpers.FrameMs);
            Assert.That(fx.Active, Is.False, "anillos y mariposa, apagados");
            Assert.That(fx.SourceRing.gameObject.activeInHierarchy || fx.TargetRing.gameObject.activeInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator Pour_FramesWithTheButterflyAndRings_DoNotAllocate()
        {
            // 08 §5.4: el vuelo, el salto y los anillos solo colocan y tiñen (los sprites ya están desde la carga del nivel).
            yield return LoadPlaying(Level, FunnelSetup.Profile(500));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            MorphoFx fx = view.MorphoFx;
            Assert.That(fx.SpritesReady, Is.True, "Prewarm al cargar un nivel con la ranura");
            yield return PressAndWait(boot);

            void Run10()
            {
                for (int f = 0; f < 10; f++)
                {
                    view.Tick(PlayModeHelpers.FrameMs);
                    boot.Flow!.Tick(PlayModeHelpers.FrameMs, view.IsViewIdle);
                }
            }

            Run10(); // en vuelo; la primera vez se calientan los lotes del lienzo
            Assert.That(Run10, Is.Not.AllocatingGCMemory(), "10 fotogramas del vuelo y la posada");
            PlayModeHelpers.TickExact(boot, (int)(MorphoFx.FlightMs + MorphoFx.PerchMs + MorphoFx.HopMs));
            Assert.That(fx.OnTarget, Is.True);

            // Con las dos cuias latiendo, el efecto solo coloca, escala y tiñe sus imágenes (lo que el lienzo hace después con
            // ellas es de Unity, como el pulso de la Luciérnaga).
            CuiaView[] views = view.Views;
            void Ring10()
            {
                for (int f = 0; f < 10; f++)
                {
                    fx.Tick(PlayModeHelpers.FrameMs, views);
                }
            }

            Ring10();
            Assert.That(Ring10, Is.Not.AllocatingGCMemory(), "10 fotogramas con las dos cuias latiendo");
            Assert.That(fx.SourceRinging && fx.TargetRinging, Is.True);
        }

        [UnityTest]
        public IEnumerator Tray_FourSlots_At360dp_130Percent_es419_NoBrokenLabels()
        {
            // 02 §12 y tarea L4 con la 4.ª ranura: en el 100 a 360 × 780 dp con la fuente al 130 %, las cuatro etiquetas a 12 sp
            // (× escala) o más, enteras, sin palabras partidas y dentro de su ranura, y la bandeja dentro de la pantalla.
            yield return LongLabelSetup.LoadLevel100(LocLocale.SourceLocale, 1.3f, HudLabelTests.Screen780);
            GameScreen view = GameBootstrap.Current!.View!;
            BoosterTray tray = view.Tray;
            TextMeshProUGUI[] labels = { tray.UndoLabel, tray.ExtraLabel, tray.HintLabel, tray.RevealLabel };
            Image[] glyphs = { tray.UndoGlyph, tray.ExtraGlyph, tray.HintGlyph, tray.RevealGlyph };
            string[] names = { "Marea", "Cuia extra", "Pista", "Revelar" };
            for (int i = 0; i < labels.Length; i++)
            {
                LongLabelSetup.AssertSlotFits(view, labels[i], glyphs[i], 1.3f, names[i]);
            }

            Assert.That(tray.Compact, Is.True, "«Deshacer» (68,7 dp a 15,6 sp) no cabía entero con la separación de siempre: la bandeja se aprieta");
            LongLabelSetup.AssertTrayInside(view);
        }

        [UnityTest]
        public IEnumerator Hop_InTheTopRow_StaysInsideTheBoardFrame()
        {
            // Revisión de MO2: el salto más largo de la fila de arriba (de la primera a la última cuia) sube en arco, pero la
            // mariposa (con media envergadura y el halo: HopHalfExtentDp) no pasa del canto de arriba del marco del tablero.
            yield return LoadPlaying(Level, FunnelSetup.Profile(500));
            GameScreen view = GameBootstrap.Current!.View!;
            MorphoFx fx = view.MorphoFx;
            CuiaView[] views = view.Views;
            float topRow = float.MinValue;
            for (int i = 0; i < views.Length; i++)
            {
                topRow = Mathf.Max(topRow, views[i].FootPosition.y);
            }

            int first = -1;
            int last = -1;
            for (int i = 0; i < views.Length; i++)
            {
                if (Mathf.Abs(views[i].FootPosition.y - topRow) < 0.5f)
                {
                    first = first < 0 ? i : first;
                    last = i;
                }
            }

            Assert.That(last, Is.GreaterThan(first), "dos cuias en la fila de arriba");
            float ceiling = -view.Layout!.PanelY;
            fx.Play(new Vector2(view.CanvasWidth * 0.5f, ceiling - 600f), first, last, views, false, ceiling);
            PlayModeHelpers.TickExact(GameBootstrap.Current!, (int)(MorphoFx.FlightMs + MorphoFx.PerchMs) + 4);
            Assert.That(fx.PerchedOnSource || fx.Hopping, Is.True);
            float perchY = fx.ButterflyPosition.y;
            float highest = float.MinValue;
            for (int t = 0; t < MorphoFx.HopMs + 40; t += 4)
            {
                fx.Tick(4f, views);
                if (fx.Hopping)
                {
                    highest = Mathf.Max(highest, fx.ButterflyPosition.y);
                }
            }

            Assert.That(fx.OnTarget, Is.True);
            Assert.That(highest + MorphoFx.HopHalfExtentDp + MorphoFx.HopCeilingGapDp, Is.LessThanOrEqualTo(ceiling + 0.5f), "dentro del marco por arriba");
            Assert.That(highest, Is.GreaterThan(perchY + 4f), "sigue siendo un salto en arco");
        }

        [UnityTest]
        public IEnumerator Pour_RingsLast3s_ThenGoOff()
        {
            yield return LoadPlaying(Level, FunnelSetup.Profile(500));
            GameBootstrap boot = GameBootstrap.Current!;
            MorphoFx fx = boot.View!.MorphoFx;
            yield return PressAndWait(boot);
            PlayModeHelpers.TickExact(boot, (int)(MorphoFx.FlightMs + MorphoFx.PerchMs + MorphoFx.HopMs) + 12);
            Assert.That(fx.OnTarget, Is.True);
            PlayModeHelpers.TickExact(boot, (int)MorphoFx.RingMs - 100);
            Assert.That(fx.SourceRinging && fx.TargetRinging, Is.True, "siguen a los 2,9 s");
            PlayModeHelpers.TickExact(boot, 100 + (int)MorphoFx.FadeMs + (2 * PlayModeHelpers.FrameMs));
            Assert.That(fx.Active, Is.False, "a los 3 s se apagan (04 §2.3)");
        }

        [UnityTest]
        public IEnumerator Pour_ReduceMotion_NoFlight_BothRingsAtOnce()
        {
            yield return LoadPlaying(Level, FunnelSetup.Profile(500));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.Flow!.ReduceMotion = true;
            MorphoFx fx = boot.View!.MorphoFx;
            yield return PressAndWait(boot);
            Assert.That(fx.Active, Is.True);
            Assert.That(fx.Flying || fx.Hopping, Is.False, "sin vuelo (07 §7.8)");
            Assert.That(fx.SourceRinging && fx.TargetRinging, Is.True, "los dos anillos a la vez");
            Assert.That(fx.SparklesRequested, Is.EqualTo(0), "sin estela");
            Assert.That(fx.ButterflyAlpha, Is.InRange(MorphoFx.ReducedStartAlpha - 0.001f, 0.75f), "aparece ya casi opaca (revisión de MO2)");
            PlayModeHelpers.TickExact(boot, (int)MorphoFx.ReducedFadeMs + 4);
            Assert.That(fx.ButterflyAlpha, Is.EqualTo(1f).Within(0.001f), "opaca a los 120 ms");
            float alpha = fx.SourceRing.color.a;
            PlayModeHelpers.TickExact(boot, 450);
            Assert.That(fx.SourceRing.color.a, Is.EqualTo(alpha).Within(0.001f), "no laten: alfa fijo");
            Assert.That(fx.SourceRing.rectTransform.localScale.x, Is.EqualTo(1f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator NotFound_ShowsTheCard_WithUndoAndRestart_WithoutCharging()
        {
            yield return LoadPlaying(Level, FunnelSetup.Profile(500));
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;

            // Un vertido, para que Deshacer tenga qué deshacer.
            Pour first = OtherLegalPour(flow.Session.Current, default);
            boot.TapCuia(first.Source);
            boot.TapCuia(first.Target);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(1));

            using (var layout = new AfLayoutLog())
            {
                yield return NotFound(boot);
                HintAnswerCard card = view.HintAnswerCard;
                Assert.That(card.Visible, Is.True, "«No encuentro un camino desde aquí» (03 §5)");
                Assert.That(card.Answer, Is.EqualTo(HintAnswer.NotFound));
                Assert.That(card.Text, Is.EqualTo("No encuentro un\u00A0camino desde aquí"));
                Assert.That(boot.Wallet!.Seeds, Is.EqualTo(500), "no cobra");
                Assert.That(card.UndoEnabled, Is.True);
                string line = layout.RequireLast("hint_answer", Level, "tarjeta de Morpho");
                Assert.That(AfLayoutLog.ButtonIds(line), Is.EqualTo(new[] { "close", "hint_undo", "hint_restart" }));

                // Deshacer: como Marea.
                card.UndoButton.onClick.Invoke();
                Assert.That(card.Visible, Is.False);
                Assert.That(flow.Session.AppliedPours, Is.EqualTo(0), "deshecho");
            }

            // Volver a la orilla: como el botón de la cabecera (con menos de 8 vertidos, sin confirmación).
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            boot.TapCuia(first.Source);
            boot.TapCuia(first.Target);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            yield return NotFound(boot);
            Assert.That(view.HintAnswerCard.Visible, Is.True);
            int attempt = flow.Attempt.FailsLevel;
            view.HintAnswerCard.RestartButton.onClick.Invoke();
            Assert.That(view.HintAnswerCard.Visible, Is.False);
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(0), "otra vez desde la orilla");
            Assert.That(flow.Attempt.FailsLevel, Is.EqualTo(attempt));
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(500));

            // La X sigue jugando.
            for (int t = 0; t < 4000 && flow.Phase != FlowPhase.Playing; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            yield return NotFound(boot);
            view.HintAnswerCard.CloseButton.onClick.Invoke();
            Assert.That(view.HintAnswerCard.Visible, Is.False);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        [UnityTest]
        public IEnumerator Card_EveryAnswer_At12spOrMore_WithoutOverlaps([Values(100, 130)] int percent)
        {
            float scale = percent / 100f;
            LaunchOptions.FontScaleOverride = scale;
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            yield return LoadPlaying(Level, FunnelSetup.Profile(500));
            GameScreen view = GameBootstrap.Current!.View!;
            HintAnswerCard card = view.HintAnswerCard;
            HintAnswer[] answers = { HintAnswer.NoExit, HintAnswer.NoSafePour, HintAnswer.NoSafePour, HintAnswer.NotFound };
            bool[] tinaja = { false, false, true, false };
            // Con el salto tras los dos puntos y los espacios duros tras los artículos de la tabla (revisión de MO2).
            string[] texts =
            {
                "Desde aquí no hay salida:\nla\u00A0Marea o\u00A0la\u00A0orilla", "No veo un\u00A0vertido seguro:\nla\u00A0luciérnaga ilumina una\u00A0cuia",
                "No veo un\u00A0vertido seguro:\nla\u00A0luciérnaga ilumina la\u00A0tinaja", "No encuentro un\u00A0camino desde aquí",
            };
            for (int i = 0; i < answers.Length; i++)
            {
                view.OnHint(new HintResult(answers[i], default, tinaja[i], HintSolver.SafeMoves, 2, 0, 0, false, false));
                yield return null;
                string what = answers[i] + (tinaja[i] ? " (tinaja)" : string.Empty) + " al " + percent + " %";
                Assert.That(card.Visible, Is.True, what);
                Assert.That(card.Text, Is.EqualTo(texts[i]), what + ": 04 §2.3 y 02 T5");
                Canvas.ForceUpdateCanvases();
                card.Body.ForceMeshUpdate(true, true);
                Assert.That(card.Body.fontSize, Is.GreaterThanOrEqualTo(12f - 0.01f), what + ": ≥ 12 sp");
                Assert.That(new LocInventory(Loc.Locale, scale).ProblemsOf(card.Body), Is.Empty, what + ": la frase cabe entera");
                RectTransform root = (RectTransform)view.Canvas.transform;
                Rect panel = VictorySetup.CanvasRect(card.Panel, root);
                Rect body = VictorySetup.CanvasRect(card.Body.rectTransform, root);
                Rect close = VictorySetup.CanvasRect((RectTransform)card.CloseButton.transform, root);
                Rect undo = VictorySetup.CanvasRect((RectTransform)card.UndoButton.transform, root);
                Rect restart = VictorySetup.CanvasRect((RectTransform)card.RestartButton.transform, root);
                var screen = new Rect(0f, 0f, view.CanvasWidth, 780f); // HudLabelTests.Screen780: 360 × 780 dp
                Assert.That(screen.Contains(panel.min + new Vector2(0.5f, 0.5f)) && screen.Contains(panel.max - new Vector2(0.5f, 0.5f)), Is.True, what + ": dentro de la pantalla " + panel + " en " + screen);
                foreach (Rect r in new[] { body, close, undo, restart })
                {
                    Assert.That(panel.Contains(r.min - new Vector2(-0.5f, -0.5f)) && panel.Contains(r.max - new Vector2(0.5f, 0.5f)), Is.True, what + ": dentro de la tarjeta " + r);
                }

                Rect disc = VictorySetup.CanvasRect(card.IconDisc, root);
                Assert.That(panel.Contains(disc.min + new Vector2(0.5f, 0.5f)) && panel.Contains(disc.max - new Vector2(0.5f, 0.5f)), Is.True, what + ": el disco, dentro de la tarjeta " + disc);
                Assert.That(disc.center.x, Is.EqualTo(panel.center.x).Within(0.5f), what + ": el disco, centrado sobre la frase");
                Assert.That(body.Overlaps(close) || body.Overlaps(undo) || body.Overlaps(restart) || undo.Overlaps(restart) || close.Overlaps(undo), Is.False, what + ": sin solapes");
                Assert.That(disc.Overlaps(close) || disc.Overlaps(body), Is.False, what + ": el disco no pisa la X ni la frase");

                // Margen de la frase (revisión de MO2: ≥ 20 dp hasta el canto de la tarjeta) y cortes cuidados: con dos puntos,
                // la primera línea acaba en ellos; ninguna línea, salvo la última, acaba en un artículo o una preposición de 1-2
                // letras (los espacios duros de la tabla).
                Rect ink = InkOf(card.Body, root);
                Assert.That(ink.xMin - panel.xMin, Is.GreaterThanOrEqualTo(20f - 0.5f), what + ": margen izquierdo de la frase");
                Assert.That(panel.xMax - ink.xMax, Is.GreaterThanOrEqualTo(20f - 0.5f), what + ": margen derecho de la frase");
                string[] lines = Lines(card.Body);
                Assert.That(lines.Length, Is.InRange(1, 2), what + ": líneas «" + string.Join(" / ", lines) + "»");
                if (texts[i].IndexOf(':') >= 0)
                {
                    Assert.That(lines.Length == 2 && lines[0].EndsWith(":", StringComparison.Ordinal), Is.True, what + ": corta tras los dos puntos «" + string.Join(" / ", lines) + "»");
                }

                for (int l = 0; l < lines.Length - 1; l++)
                {
                    string[] words = lines[l].Split(' ', '\u00A0');
                    Assert.That(words[words.Length - 1].Length, Is.GreaterThan(2), what + ": la línea «" + lines[l] + "» acaba en una palabra corta");
                }
                Assert.That(undo.height >= 48f - 0.01f && restart.height >= 48f - 0.01f && close.height >= 48f - 0.01f, Is.True, what + ": botones de 48 dp o más (07 §7.8)");
            }
        }

        [UnityTest]
        public IEnumerator Unsupported_GreySlot_TooltipOnTap()
        {
            // Un nivel sin hint_supported (04 §2.3 b): la ranura existe, gris; tocarla no pide nada y enseña el globo.
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGameWithPack(UnsupportedPack(Level), Level, new MemoryProgressStore(Level, Level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            yield return Ready(boot);
            BoosterTray tray = boot.View!.Tray;
            Assert.That(boot.Flow!.Hint.State, Is.EqualTo(HintState.Unsupported));
            Assert.That(tray.HintShown, Is.True);
            Assert.That(tray.HintLooksEnabled, Is.False, "gris");
            Assert.That(tray.HintPriceShown, Is.EqualTo(0));
            Assert.That(tray.HintButton.interactable, Is.True, "el toque llega");
            Assert.That(tray.HintTooltipVisible, Is.False);

            tray.HintButton.onClick.Invoke();
            Assert.That(boot.Flow.HintThinking, Is.False, "no se pide nada");
            Assert.That(tray.HintTooltipVisible, Is.True);
            Assert.That(tray.HintTooltipText, Is.EqualTo("La morpho no conoce este tramo todavía"));
            Canvas.ForceUpdateCanvases();
            tray.HintTooltipLabel.ForceMeshUpdate(true, true);
            Assert.That(tray.HintTooltipLabel.fontSize, Is.GreaterThanOrEqualTo(12f), "≥ 12 sp");
            Assert.That(new LocInventory(Loc.Locale, 1f).ProblemsOf(tray.HintTooltipLabel), Is.Empty, "el texto cabe en el globo");

            // Revisión de MO2: dos líneas parejas, nunca una palabra sola en la segunda («La morpho no conoce / este tramo
            // todavía»), y el globo centrado sobre su ranura si cabe en la pantalla.
            TMP_TextInfo info = tray.HintTooltipLabel.textInfo;
            Assert.That(info.lineCount, Is.EqualTo(2), "en dos líneas");
            int a = info.lineInfo[0].characterCount;
            int b = info.lineInfo[1].characterCount;
            Assert.That(Mathf.Min(a, b), Is.GreaterThanOrEqualTo((int)(0.6f * Mathf.Max(a, b))), "líneas parejas: " + a + " y " + b + " caracteres");
            RectTransform root = (RectTransform)boot.View.Canvas.transform;
            Rect tip = VictorySetup.CanvasRect(tray.HintTooltipRect, root);
            Rect slot = VictorySetup.CanvasRect((RectTransform)tray.HintButton.transform, root);
            Assert.That(tip.xMin >= BoosterTray.TrayEdgeDp - 0.5f && tip.xMax <= boot.View.CanvasWidth - BoosterTray.TrayEdgeDp + 0.5f, Is.True, "dentro de la pantalla " + tip);
            bool clamped = tip.xMax >= boot.View.CanvasWidth - BoosterTray.TrayEdgeDp - 0.5f || tip.xMin <= BoosterTray.TrayEdgeDp + 0.5f;
            Assert.That(clamped || Mathf.Abs(tip.center.x - slot.center.x) <= 0.5f, Is.True, "centrado sobre la ranura " + tip + " / " + slot);
            Assert.That(tip.yMax, Is.LessThanOrEqualTo(slot.yMin + 0.5f), "encima de la ranura");
            Assert.That(tip.Overlaps(VictorySetup.CanvasRect((RectTransform)tray.ExtraCuiaButton.transform, root)), Is.False, "sin tapar la vecina");

            float left = tray.HintTooltipLeftMs;
            Assert.That(left, Is.InRange((float)NoticeTiming.MinMs, (float)NoticeTiming.MaxMs), "lo que se tarda en leerlo (D-011)");
            PlayModeHelpers.TickExact(boot, (int)left + PlayModeHelpers.FrameMs);
            Assert.That(tray.HintTooltipVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator MissingNotices_OfFourSlots_At12sp_WithoutOverlaps([Values(100, 130)] int percent)
        {
            // 106 con 50 semillas y sin video: Cuia extra (150), Pista (100) y Revelar (120) enseñan su «Te faltan {n}» a la vez,
            // estrechos, cada uno sobre su ranura y sin pisarse; los tres a 12 sp (× escala: 02 §12; D-020, pregunta 36 (a)) y
            // enteros, también el de Pista, el nuevo de MO2.
            float scale = percent / 100f;
            LaunchOptions.FontScaleOverride = scale;
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            yield return LoadPlaying(106, FunnelSetup.Profile(50));
            GameScreen view = GameBootstrap.Current!.View!;
            BoosterTray tray = view.Tray;
            Assert.That(tray.SlotCount, Is.EqualTo(4));
            Assert.That(tray.ExtraShowsMissing && tray.HintShowsMissing && tray.RevealShowsMissing, Is.True);
            Assert.That(tray.HintMissingText, Is.EqualTo("Te faltan 50"));
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect extra = VictorySetup.CanvasRect(tray.ExtraMissingRect, root);
            Rect hint = VictorySetup.CanvasRect(tray.HintMissingRect, root);
            Rect reveal = VictorySetup.CanvasRect(tray.RevealMissingRect, root);
            Assert.That(extra.Overlaps(hint) || hint.Overlaps(reveal), Is.False, "sin solapes");
            Rect slot = VictorySetup.CanvasRect((RectTransform)tray.HintButton.transform, root);
            Assert.That(hint.center.x, Is.EqualTo(slot.center.x).Within(0.5f), "sobre su ranura");
            Assert.That(hint.yMax, Is.LessThanOrEqualTo(slot.yMin + 0.5f));
            Canvas.ForceUpdateCanvases();
            string[] names = { "Cuia extra", "Pista", "Revelar" };
            TextMeshProUGUI[] labels = { tray.ExtraMissingLabel, tray.HintMissingLabel, tray.RevealMissingLabel };
            for (int i = 0; i < labels.Length; i++)
            {
                TextMeshProUGUI label = labels[i];
                label.ForceMeshUpdate(true, true);
                Assert.That(label.fontSize, Is.EqualTo(12f * scale).Within(0.01f), names[i] + ": 12 sp × escala (02 §12)");
                Assert.That(new LocInventory(Loc.Locale, scale).ProblemsOf(label), Is.Empty, names[i] + ": «" + label.text + "» cabe en su placa");
            }
        }

        [Test]
        public void Colors_ButterflyAndRings_AtLeast10DeltaE00FromEveryTint()
        {
            // direccion-visual-v2 §2.4, regla 1: lo que toca o rodea una cuia (la mariposa posada en la boca, el anillo).
            Color[] surfaces =
            {
                ProceduralArt.MorphoBlue, ProceduralArt.MorphoDeep, ProceduralArt.MorphoLight, ProceduralArt.MorphoBody, ProceduralArt.MorphoRingColor,
                ProceduralArt.MorphoFlightBlue, ProceduralArt.MorphoFlightLight,
            };
            for (int c = 0; c < surfaces.Length; c++)
            {
                for (int tint = 1; tint <= Cuia.MaxTintColor; tint++)
                {
                    Assert.That(ColorMath.DeltaE00(surfaces[c], Palette.Tint(tint)), Is.GreaterThanOrEqualTo(10f), "color " + c + " frente al tinte " + tint);
                }
            }

            Assert.That(ColorMath.DeltaE00(ProceduralArt.MorphoBlue, Palette.Hex(0x2F6FD6)), Is.LessThan(0.01f), "el azul morpho de 07 §7.6");
        }

        // Las líneas que pinta TextMeshPro (con ForceMeshUpdate hecho), sin los espacios del corte.
        private static string[] Lines(TextMeshProUGUI text)
        {
            TMP_TextInfo info = text.textInfo;
            var lines = new string[info.lineCount];
            for (int l = 0; l < info.lineCount; l++)
            {
                TMP_LineInfo line = info.lineInfo[l];
                var chars = new char[Math.Max(0, line.lastCharacterIndex - line.firstCharacterIndex + 1)];
                for (int c = 0; c < chars.Length; c++)
                {
                    chars[c] = info.characterInfo[line.firstCharacterIndex + c].character;
                }

                lines[l] = new string(chars).Trim(' ', '\n', '\u00A0');
            }

            return lines;
        }

        // La caja de la tinta de un texto (los glifos visibles) en el lienzo de root.
        private static Rect InkOf(TextMeshProUGUI text, RectTransform root)
        {
            Bounds b = text.textBounds;
            Vector3 min = root.InverseTransformPoint(text.rectTransform.TransformPoint(b.min));
            Vector3 max = root.InverseTransformPoint(text.rectTransform.TransformPoint(b.max));
            Rect r = root.rect;
            return Rect.MinMaxRect(min.x - r.xMin, r.yMax - max.y, max.x - r.xMin, r.yMax - min.y);
        }

        // n ranuras de 64 dp en el orden dado, con 8 dp entre ellas, y la bandeja (16 + n·64 + (n − 1)·8 + 16) centrada.
        private static void AssertSlots(GameScreen view, params Button[] slots)
        {
            BoosterTray tray = view.Tray;
            int n = slots.Length;
            float width = BoosterTray.WidthFor(n);
            Assert.That(tray.SlotCount, Is.EqualTo(n));
            Assert.That(tray.Width, Is.EqualTo(width).Within(0.01f));
            float x = ((view.CanvasWidth - width) * 0.5f) + 16f;
            for (int i = 0; i < n; i++)
            {
                var rt = (RectTransform)slots[i].transform;
                Assert.That(slots[i].gameObject.activeInHierarchy, Is.True, rt.name);
                Assert.That(rt.rect.width, Is.EqualTo(64f).Within(0.01f), rt.name + ": 64 dp");
                Assert.That(rt.anchoredPosition.x, Is.EqualTo(x).Within(0.01f), rt.name + ": en su sitio, centrada y sin huecos");
                x += 72f;
            }
        }

        private static IEnumerator LoadPlaying(int level, Profile profile)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            yield return Ready(GameBootstrap.Current!);
        }

        private static IEnumerator Ready(GameBootstrap boot)
        {
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            for (int t = 0; t < 4000 && (boot.Flow!.Phase != FlowPhase.Playing || !boot.View!.IsViewIdle); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            PlayModeHelpers.Step(boot, 100);
            yield return null;
        }

        // Pide la pista con el botón (IGameActions.PressHint) y espera la respuesta.
        private static IEnumerator PressAndWait(GameBootstrap boot)
        {
            Assert.That(boot.PressHint(), Is.True);
            yield return WaitAnswer(boot);
        }

        private static IEnumerator WaitAnswer(GameBootstrap boot)
        {
            float start = Time.realtimeSinceStartup;
            while (boot.Flow!.HintThinking)
            {
                Assert.That(Time.realtimeSinceStartup - start, Is.LessThan(PlayModeHelpers.TimeoutSeconds), "Morpho no respondió");
                PlayModeHelpers.TickExact(boot, 4);
                yield return null;
            }
        }

        // «No encuentro un camino desde aquí»: el hilo de Morpho, retenido, pasa el corte de hint_timeout_ms (150 ms).
        private static IEnumerator NotFound(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(BoosterConfig.HintCooldownMs + 1)); // pasado el enfriamiento [P]
            using (var gate = new ManualResetEventSlim(false))
            {
                flow.Morpho = new MorphoBooster(flow.Boosters, flow.Clock, () => gate.Wait(20000));
                Assert.That(boot.PressHint(), Is.True);
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                FunnelSetup.Clock.Advance(TimeSpan.FromMilliseconds(flow.Boosters.HintTimeoutMs));
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                gate.Set();
                yield return WaitAnswer(boot);
            }

            Assert.That(flow.LastHint!.Value.Answer, Is.EqualTo(HintAnswer.NotFound));
        }

        /// <summary>
        /// El tablero del nivel 7 en el nivel <paramref name="level"/> del mundo 2 con <c>hint_supported</c> = false (04 §2.3 b: el
        /// solver del cliente aún no modela su mecánica).
        /// </summary>
        internal static string UnsupportedPack(int level)
        {
            string json = PlayModeHelpers.Level7BoardPack(2, "w02_tapajos", "clear", level);
            string off = json.Replace("\"difficulty\": { \"par\": 1 }", "\"difficulty\": { \"par\": 1 }, \"hint_supported\": false");
            Assert.That(off, Is.Not.EqualTo(json), "el paquete de prueba cambió de forma");
            return off;
        }

        // Un vertido legal distinto de skip (el primero en orden de origen y destino).
        internal static Pour OtherLegalPour(BoardState board, Pour skip)
        {
            for (int s = 0; s < board.Count; s++)
            {
                for (int t = 0; t < board.Count; t++)
                {
                    if (s != t && !(s == skip.Source && t == skip.Target) && PourRules.IsLegal(board, s, t))
                    {
                        return new Pour(s, t, 1);
                    }
                }
            }

            Assert.Fail("sin otro vertido legal");
            return default;
        }
    }
}
