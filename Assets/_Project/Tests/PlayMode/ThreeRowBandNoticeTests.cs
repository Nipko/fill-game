#nullable enable
using System.Collections;
using AF.Ads;
using AF.GameFlow;
using AF.Localization;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Los avisos con 3 filas y el suelo bajado (D-046, punto 3; Q-T6T-3, decidida por el dueño el 2026-10-24: «Bajar y mover
    /// avisos»), en el 135 a 1080 × 2340 px (360 × 780 dp: 12 cuias, 4/4/4, con ocultas; el panel acaba 11,5 dp sobre la
    /// bandeja): las burbujas «Te faltan {n}» que pisarían el panel van a la franja de la tropa, sobre el panel y bajo la fila del
    /// HUD, legibles (12 sp × escala, sin desbordes); no se dibujan mientras la tropa está en la franja. El aviso «¡Una cuia
    /// más!» tras el rescate, que ya no cabe entre el tablero y la bandeja, va a la franja, legible al 100 % y al 130 % en es-419 y
    /// en-XA, sin pisar el panel ni la fila del HUD; si con las burbujas no cabe, ellas no se dibujan mientras dura y vuelven
    /// después. Sin la franja (16:9), lo de siempre (MinTextSizeTests y PhoneFixesTests con TroopBandOffForTests).
    /// </summary>
    public sealed class ThreeRowBandNoticeTests
    {
        [TearDown]
        public void TearDown()
        {
            PseudoLocWalk.Release();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Level135_TheRescueNotice_GoesToTheBand_Legible([Values(100, 130)] int percent, [Values(LocLocale.SourceLocale, LocLocale.Pseudo)] string locale)
        {
            float scale = percent / 100f;
            yield return Load(135, 300, locale, scale);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            BoardLayoutResult l = view.Layout!;
            string where = "135 tras el rescate, " + locale + " al " + percent + " %";
            Assert.That(l.TroopBand, Is.True, where + ": la franja de la tropa");
            PhoneFixesTests.RescueForReal(boot, null, "135");
            l = view.Layout!;
            Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied));
            Assert.That(view.ToastInTroopBand, Is.True, where + ": el aviso, en la franja");
            Rect toast = view.NoticeToast.SeenRect;
            Assert.That(toast.yMin, Is.GreaterThanOrEqualTo(view.HudBottomDp - 0.01f), where + ": bajo la fila del HUD " + toast);
            Assert.That(toast.yMax, Is.LessThanOrEqualTo(l.PanelY - GameScreen.NoticeBoardGapDp + 0.01f), where + ": sin pisar el panel " + toast);
            MinTextSizeTests.AssertTextFits(view.NoticeToast.Label, scale, where);
            Assert.That(view.Tray.ExtraShowsMissing && view.Tray.RevealShowsMissing, Is.True, where + ": las dos burbujas, sin semillas");
            Assert.That(view.Tray.AnyMissingInBand, Is.True, where + ": en la franja");
            if (view.ToastHidesBandBubbles)
            {
                Assert.That(view.Tray.AnyMissingInBandDrawn, Is.False, where + ": no caben con el aviso: no se dibujan mientras dura");
            }
            else
            {
                AssertBubbleClear(view, view.Tray.ExtraMissingRect, toast, where + ", «+1 cuia»");
                AssertBubbleClear(view, view.Tray.RevealMissingRect, toast, where + ", Revelar");
            }

            TestContext.WriteLine(where + ": aviso " + toast + ", burbujas ocultas " + view.ToastHidesBandBubbles);
            for (int t = 0; t < 9000 && view.ToastText != null; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.ToastText, Is.Null, where + ": el aviso se va");
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.Tray.AnyMissingInBandDrawn, Is.True, where + ": las burbujas vuelven");
            AssertBubbleInBand(view, view.Tray.ExtraMissingRect, scale, view.Tray.ExtraMissingLabel, where + ", «+1 cuia»");
            AssertBubbleInBand(view, view.Tray.RevealMissingRect, scale, view.Tray.RevealMissingLabel, where + ", Revelar");
        }

        [UnityTest]
        public IEnumerator Level135_Bubbles_WaitForTheTroop_AndAreNotOverThePanel()
        {
            yield return Load(135, 0, LocLocale.SourceLocale, 1f);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Layout!.TroopBand, Is.True);
            Assert.That(view.Tray.AnyMissingInBand, Is.True, "las burbujas no caben bajo el panel: a la franja");
            Assert.That(view.Tray.AnyMissingInBandDrawn, Is.True, "sin tropa, se dibujan");
            AssertBubbleInBand(view, view.Tray.ExtraMissingRect, 1f, view.Tray.ExtraMissingLabel, "«+1 cuia»");
            AssertBubbleInBand(view, view.Tray.RevealMissingRect, 1f, view.Tray.RevealMissingLabel, "Revelar");

            // La intro otra vez, con la tropa en la rama: mientras se ve, las burbujas de la franja no se dibujan.
            view.DebugReplayIntro(1400, false);
            int seen = 0;
            for (int t = 0; t < 4000; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                if (view.IntroTroopVisible)
                {
                    seen++;
                    Assert.That(view.Tray.AnyMissingInBandDrawn, Is.False, "con la tropa en la franja, a " + t + " ms");
                }
            }

            Assert.That(seen, Is.GreaterThan(10), "la tropa se vio");
            Assert.That(view.IntroTroopVisible, Is.False);
            Assert.That(view.Tray.AnyMissingInBandDrawn, Is.True, "al irse la tropa, vuelven");
        }

        // El 106 (Difícil, Aguas turbias, 11 cuias) con 50 semillas: tres burbujas, una en dos líneas, en la franja; la banda de
        // etiquetas del HUD (dificultad y «?») no las pisa: se compacta o va pegada bajo la placa (al 100 y al 130 %).
        [UnityTest]
        public IEnumerator Level106_BubblesInTheBand_ClearOfTheHudLabels([Values(100, 130)] int percent)
        {
            float scale = percent / 100f;
            yield return Load(106, 50, LocLocale.SourceLocale, scale);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            string where = "106 al " + percent + " %";
            Assert.That(view.Layout!.TroopBand, Is.True);
            Assert.That(view.Tray.AnyMissingInBandDrawn, Is.True, where + ": burbujas en la franja");
            Assert.That(view.HudDifficultyChip.Visible && view.HudMurkyChip.Visible, Is.True, where + ": las dos etiquetas");
            Rect chip = CanvasRect(view, view.HudDifficultyChip.Root);
            Rect murky = CanvasRect(view, view.HudMurkyChip.Root);
            foreach (RectTransform bubble in new[] { view.Tray.ExtraMissingRect, view.Tray.RevealMissingRect })
            {
                if (!bubble.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Rect r = CanvasRect(view, bubble);
                AssertBubbleInBand(view, bubble, scale, bubble.GetComponentInChildren<TMPro.TextMeshProUGUI>(), where);
                Assert.That(r.Overlaps(chip), Is.False, where + ": la burbuja " + r + " no pisa la dificultad " + chip);
                Assert.That(r.Overlaps(murky), Is.False, where + ": ni el «?» " + murky);
            }

            TestContext.WriteLine(where + ": banda del HUD " + view.HudLabelsMode + ", dificultad " + chip + ", «?» " + murky);
        }

        private static IEnumerator Load(int level, int seeds, string locale, float scale)
        {
            FunnelSetup.Hooks();
            LaunchOptions.LanguageOverride = locale == LocLocale.SourceLocale ? null : locale;
            if (Loc.IsConfigured && Loc.Locale != locale)
            {
                Loc.SetLocale(locale);
            }

            LaunchOptions.FontScaleOverride = scale;
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        private static Rect CanvasRect(GameScreen view, RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            return VictorySetup.CanvasRect(rect, (RectTransform)view.Canvas.transform);
        }

        private static void AssertBubbleInBand(GameScreen view, RectTransform bubble, float scale, TMPro.TextMeshProUGUI label, string what)
        {
            Rect r = CanvasRect(view, bubble);
            BoardLayoutResult l = view.Layout!;
            Assert.That(r.yMax, Is.LessThanOrEqualTo(l.PanelY - BoosterTray.MissingPanelClearDp + 0.01f), what + ": sobre el panel " + r);
            Assert.That(r.yMin, Is.GreaterThanOrEqualTo(view.HudBottomDp - 0.01f), what + ": bajo la fila del HUD " + r);
            MinTextSizeTests.AssertTextFits(label, scale, what);
        }

        private static void AssertBubbleClear(GameScreen view, RectTransform bubble, Rect toast, string what)
        {
            Rect r = CanvasRect(view, bubble);
            Assert.That(r.Overlaps(toast), Is.False, what + ": el aviso " + toast + " no tapa la burbuja " + r);
        }
    }
}
