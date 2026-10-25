#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La interfaz de la Crecida, la Hoja escudo y la Canasta en el juego (tarea B1b de la tanda 5), con el almacén de la Crecida
    /// del test (<see cref="GameBootstrap.StreakStoreOverride"/>) y el reloj fijo:
    /// <list type="bullet">
    /// <item>la barra vertical del HUD (02 §5; 07 §7.4: x = 348-356 e y = 120-560 a 360 × 780), que no está en la intro y sale en T
    /// (D-030, pregunta 4 (c)), con la Hoja escudo al pie, y no existe antes del 25;</item>
    /// <item>las barras del panel único de la victoria (D-033, puntos 8-10): la Crecida que sube y paga el hito 3 con las semillas
    /// que llegan al saldo al verse (D-011), la Canasta que se abre sola en el 10k, y un toque en Siguiente con animaciones
    /// pendientes que las completa, espera 250 ms y sigue (punto 9);</item>
    /// <item>la línea de la Hoja escudo en Aguas estancadas (04 §6.5) y su aviso tras el fallo cubierto (04 §8);</item>
    /// <item>la tarjeta de la Crecida al cargar el 25 (04 §4) y el widget del inicio (06 §4.7);</item>
    /// <item>«Cancelar» de la hoja de compra simulada dentro del margen inferior seguro (parche de B10; 08 §13.1 #4).</item>
    /// </list>
    /// </summary>
    public sealed class StreakUiTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private const string Today = "2026-10-08";

        private ManualClock _clock = null!;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            _clock = new ManualClock(T0, Bogota);
            LaunchOptions.ClockOverride = _clock;
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetStreakOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            if (!Loc.IsConfigured || Loc.Locale != LocLocale.SpanishLatinAmerica)
            {
                LocSetUpFixture.ConfigureEs419();
            }
        }

        // ============================================================================================ barra del HUD

        [UnityTest]
        public IEnumerator HudBar_NotInTheIntro_ComesInT_AtTheRightEdge_WithTheLeafAtItsFoot()
        {
            UseStore(Record(4, true));
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            yield return HudSetup.LoadAtIntro(30);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(view.StreakHudBar.Visible, Is.False, "durante la intro no está: la tropa lateral pasa por su franja (D-030, 4 c)");

            HudSetup.SkipIntro(boot);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.StreakHudBar.Visible, Is.True, "sale en T, al habilitarse el toque");
            Assert.That(view.StreakHudBar.Wins, Is.EqualTo(4));
            Assert.That(view.StreakHudBar.Fill01, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(view.StreakHudBar.ShieldShown, Is.True, "la Hoja escudo al pie");

            Rect track = CanvasRect(view, view.StreakHudBar.TrackRect);
            Assert.That(track.xMin, Is.EqualTo(348f).Within(0.01f), "07 §7.4: x = 348");
            Assert.That(track.xMax, Is.EqualTo(356f).Within(0.01f), "x = 356, 8 dp de ancho");
            Assert.That(track.yMin, Is.EqualTo(120f).Within(0.01f), "y = 120 con la zona segura de 32 dp");
            Assert.That(track.yMax, Is.EqualTo(560f).Within(0.01f), "y = 560");
            Rect leaf = CanvasRect(view, view.StreakHudBar.LeafRect);
            Assert.That(leaf.xMax, Is.LessThanOrEqualTo(view.CanvasWidth + 0.01f), "la hoja, dentro del lienzo");
            Assert.That(leaf.center.y, Is.EqualTo(560f).Within(0.5f), "al pie de la barra");
            foreach (UnityEngine.UI.Graphic g in view.StreakHudBar.Root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {
                Assert.That(g.raycastTarget, Is.False, g.name + ": no captura toques");
            }

            // Un reintento: otra intro, sin barra; al acabar, otra vez.
            Assert.That(boot.PressRestart(), Is.True);
            PlayModeHelpers.Step(boot, 50);
            if (boot.Flow.Phase == FlowPhase.Intro)
            {
                Assert.That(view.StreakHudBar.Visible, Is.False, "en la intro corta del reintento tampoco");
            }
        }

        [UnityTest]
        public IEnumerator HudBar_Before25_DoesNotExist()
        {
            UseStore(Record(0, true));
            yield return HudSetup.LoadPlaying(24);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.View!.StreakHudBar.Visible, Is.False, "visible desde el 25 (streak_unlock_level)");
        }

        // ============================================================================================ victoria

        [UnityTest]
        public IEnumerator Victory_Hit3_TheStreakRises_ThePrizeFlies_AndTheSeedsShowWhenTheyLand()
        {
            UseStore(Record(2, true));
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            yield return Win(27);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            VictoryBars bars = view.VictoryBarsView;
            Assert.That(bars.Plan.Streak && bars.Plan.Basket, Is.True, "la Crecida (desde el 25) y la Canasta (desde el 10)");
            Assert.That(bars.Plan.StreakPaid, Is.True, "el 27 lleva la barra a 3: el hito 3");
            Assert.That(bars.StreakText, Is.EqualTo("Crecida 2/10"), "abre con la barra de antes");
            Assert.That(view.StreakHudBar.Visible, Is.False, "la victoria la replica en grande (02 §6)");
            Assert.That(bars.Root.gameObject.activeSelf, Is.True);

            int seeds = boot.Services!.Wallet.Seeds;
            PlayModeHelpers.Step(boot, 400); // el panel ya subió; la Crecida aún no termina (900 ms)
            Assert.That(bars.Pending, Is.True);
            Assert.That(view.WalletBar.SeedsShown, Is.LessThanOrEqualTo(seeds - 300), "las 300 del hito no se dan por ganadas antes de verse (D-011)");

            // Orden del panel (D-033, punto 10): Cosecha → barras → Cosecha doble / Siguiente.
            Rect harvest = CanvasRect(view, view.VictoryHarvestRect);
            Rect streak = CanvasRect(view, bars.StreakRect);
            Rect basket = CanvasRect(view, bars.BasketRect);
            Assert.That(streak.yMin, Is.GreaterThanOrEqualTo(harvest.yMax - 0.01f), "bajo la Cosecha");
            Assert.That(basket.yMin, Is.GreaterThanOrEqualTo(streak.yMin - 0.01f), "la Canasta, con o tras la Crecida");
            Assert.That(Mathf.Max(streak.yMax, basket.yMax), Is.LessThanOrEqualTo(view.NextTopDp + 0.01f), "sobre Siguiente");

            PlayModeHelpers.Step(boot, 2000);
            Assert.That(bars.Pending, Is.False);
            Assert.That(bars.StreakText, Is.EqualTo("Crecida 3/10"));
            Assert.That(bars.StreakFill, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(bars.PendingSeeds, Is.EqualTo(0));
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(boot.Services.Wallet.Seeds), "ya se ve todo lo cobrado");
            Fx16bTests.AssertCompactVictory(view, "27 a 360 × 780");
        }

        [UnityTest]
        public IEnumerator Victory_At30_TheBasketOpensItself_AndATapOnNext_CompletesWaits250ms_AndGoesOn()
        {
            UseStore(Record(1, true));
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return Win(30);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            VictoryBars bars = view.VictoryBarsView;
            Assert.That(bars.Plan.BasketOpens, Is.True, "el 30 paga su Canasta: se abre sola (D-033, punto 8)");
            Assert.That(bars.Plan.BasketCount, Is.EqualTo(10));
            Assert.That(bars.Pending, Is.True, "aún suben");
            Fx16bTests.AssertCompactVictory(view, "30 a 360 × 640");
            Assert.That(bars.Half, Is.True, "a 360 × 640 en es-419, la Crecida y la Canasta en una fila");

            PlayModeHelpers.Step(boot, 700); // Siguiente ya entró (600 ms); la Canasta, aún no
            Assert.That(bars.Pending, Is.True);
            view.NextButton.onClick.Invoke();
            Assert.That(bars.Pending, Is.False, "el toque completa las animaciones (D-033, punto 9)");
            Assert.That(bars.BasketText, Does.EndWith("10/10"));
            Assert.That(bars.PendingSeeds, Is.EqualTo(0), "todo lo pagado, en el saldo");
            Assert.That(view.NextHoldPending, Is.True);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "enseña el estado final 250 ms");
            Assert.That(view.HomeButton.interactable, Is.False, "mientras espera, Inicio no responde");
            Assert.That(!view.DoubleHarvestShown || !view.DoubleHarvestButton.interactable, Is.True, "ni la Cosecha doble (no abre un vídeo fuera de la victoria)");
            Assert.That(bars.StreakTextAlpha, Is.EqualTo(1f).Within(0.001f), "los textos vuelven tras el salto del premio");
            Assert.That(bars.BasketTextAlpha, Is.EqualTo(1f).Within(0.001f));
            view.NextButton.onClick.Invoke(); // un segundo toque no hace nada
            PlayModeHelpers.Step(boot, 200);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.NextHoldPending, Is.False);
            Assert.That(boot.Flow.LevelNumber == 31 || boot.Flow.Phase != FlowPhase.VictoryScreen, Is.True, "y sigue por el camino de Siguiente");
        }

        [UnityTest]
        public IEnumerator Victory_HalfWidth_AWidePrize_HidesTheOtherLabelOnlyWhileItJumps()
        {
            UseStore(Record(5, true));
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return Win(30); // hito 6 (su Canasta) y la Canasta del 30: dos premios que saltan a media anchura
            GameBootstrap boot = GameBootstrap.Current!;
            VictoryBars bars = boot.View!.VictoryBarsView;
            Assert.That(bars.Half, Is.True);
            PlayModeHelpers.Step(boot, 950); // salta el premio del hito 6
            Assert.That(bars.Bursting, Is.True);
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(bars.Bursting, Is.False);
            Assert.That(bars.StreakTextAlpha, Is.EqualTo(1f).Within(0.001f), "la Crecida, otra vez a la vista");
            Assert.That(bars.BasketTextAlpha, Is.EqualTo(1f).Within(0.001f), "la Canasta, otra vez a la vista");
            Assert.That(bars.PendingSeeds, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Victory_WithoutPendingAnimations_NextGoesAtOnce()
        {
            UseStore(Record(0, true));
            yield return Win(27);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.Step(boot, 2000);
            Assert.That(view.VictoryBarsView.Pending, Is.False);
            view.NextButton.onClick.Invoke();
            Assert.That(view.NextHoldPending, Is.False);
            Assert.That(boot.Flow!.Phase, Is.Not.EqualTo(FlowPhase.VictoryScreen), "sin espera");
        }

        [UnityTest]
        public IEnumerator Victory_16x9And780_At100And130_BarsFit_AndTheCompactRulesHold(
            [Values(LocLocale.SpanishLatinAmerica, LocLocale.Pseudo)] string locale, [Values(1f, 1.3f)] float scale)
        {
            foreach (bool small in new[] { true, false })
            {
                UseStore(Record(5, false));
                GameScreen.SimulatedScreen = HudLabelTests.Screen780; // FactLineTests.Win pone la de 640 con small
                yield return FactLineTests.Win(29, null, locale, scale, small);
                GameBootstrap boot = GameBootstrap.Current!;
                GameScreen view = boot.View!;
                PlayModeHelpers.Step(boot, 2000);
                string what = "29 en " + locale + " al " + Mathf.RoundToInt(scale * 100f) + " % a " + (small ? "360 × 640" : "360 × 780");
                VictoryBars bars = view.VictoryBarsView;
                TestContext.WriteLine(what + ": escalón " + view.VictoryCompact + ", barras " + (view.VictoryBarsDropped ? "fuera" : bars.Half ? "en una fila" : "a lo ancho")
                    + ", " + bars.Height + " dp, panel " + view.VictoryPanelHeight + " de " + view.VictoryPanelBudgetDp);
                Fx16bTests.AssertCompactVictory(view, what);
                if (!view.VictoryBarsDropped)
                {
                    Assert.That(bars.Root.gameObject.activeSelf, Is.True, what);
                    if (view.VictoryCompact >= GameScreen.VictoryCompactText)
                    {
                        Assert.That(bars.Half, Is.True, what + ": desde el escalón 2, a media anchura (D-033, punto 10)");
                    }

                    foreach (TMPro.TextMeshProUGUI t in bars.Root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false))
                    {
                        if (string.IsNullOrEmpty(t.text))
                        {
                            continue;
                        }

                        t.ForceMeshUpdate();
                        Assert.That(t.isTextOverflowing, Is.False, what + ": «" + t.text + "» cabe");
                        Assert.That(t.textBounds.size.x, Is.LessThanOrEqualTo(t.rectTransform.rect.width + 1f), what + ": «" + t.text + "» en su caja");
                    }
                }
                else
                {
                    Assert.That(locale == LocLocale.Pseudo && scale > 1.01f && small, Is.True, what + ": solo en en-XA al 130 % en 16:9 no caben [P]");
                }

                if (!small)
                {
                    Assert.That(view.CardCanvasHeight, Is.EqualTo(GameScreen.ReferenceHeight).Within(0.01f), what + ": 360 × 780");
                }

                if (!small)
                {
                    // 360 × 780 no compacta (D-032, punto 2), salvo en-XA al 130 %: los márgenes justos del escalón 1 [P].
                    bool pseudo130 = locale == LocLocale.Pseudo && scale > 1.01f;
                    Assert.That(view.VictoryCompact, Is.LessThanOrEqualTo(pseudo130 ? GameScreen.VictoryCompactMargins : 0), what + ": a 360 × 780 no compacta (D-032, punto 2)");
                }

                GameScreen.SimulatedScreen = null;
                LaunchOptions.Reset();
                LaunchOptions.ClockOverride = _clock;
                FunnelSetup.Hooks();
            }
        }

        // ============================================================================================ Aguas estancadas

        [UnityTest]
        public IEnumerator Stuck_At27_TheShieldLine_ReadyThenUsed_AndAfterTheCoveredFail_TheToast()
        {
            InMemoryStreakStore store = UseStore(Record(3, true));
            yield return LoadStuck(27);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.StuckVisible, Is.True);
            Assert.That(view.Stuck.ShieldLineText, Is.EqualTo(UiText.StuckShieldReady), "04 §6.5: con la hoja de hoy");
            Assert.That(view.Stuck.ShieldLine.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f));
            Assert.That(view.StreakHudBar.Visible, Is.True, "la barra sigue a la vista con el modal");

            // Volver a la orilla desde el modal (fallo duro en 1-30): la hoja lo cubre y su aviso sale al volver a jugar.
            view.Stuck.RestartButton.onClick.Invoke();
            Assert.That(boot.Flow!.LastStreakResult, Is.Not.Null);
            Assert.That(boot.Flow.LastStreakResult!.Streak.ShieldUsed, Is.True, "C9: se gasta la hoja");
            for (int ms = 0; ms < 4000 && boot.Flow.Phase != FlowPhase.Playing; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            PlayModeHelpers.Step(boot, 400);
            Assert.That(view.ToastText, Is.EqualTo(UiText.ShieldToast), "04 §8: el toast del primer fallo cubierto");
            Assert.That(view.StreakHudBar.Wins, Is.EqualTo(3), "la barra no cambia");
            Assert.That(view.StreakHudBar.ShieldShown, Is.False, "ya no queda hoja hoy");

            // Otra vez atascado, sin hoja: «Sin Hoja escudo hoy: este fallo rompe tu Crecida de 3».
            FunnelSetup.GetStuck(boot);
            PlayModeHelpers.Step(boot, 500);
            Assert.That(view.Stuck.ShieldLineText, Is.EqualTo(UiText.StuckShieldUsed(3)));
            Assert.That(store.Load().ShieldAvailable, Is.False);
        }

        [UnityTest]
        public IEnumerator Stuck_WithTheBarAt0_NoShieldLine()
        {
            UseStore(Record(0, true));
            yield return LoadStuck(27);
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.StuckVisible, Is.True);
            Assert.That(view.Stuck.ShieldLineText, Is.Null, "con la barra a 0 no hay racha que romper [P]");
        }

        // ============================================================================================ tarjeta del 25

        [UnityTest]
        public IEnumerator Card25_AtLoad_TheStreakCard_OnlyOnTheUnlockLevel()
        {
            UseStore(Record(0, true));
            LaunchOptions.LevelCardsOverride = true;
            yield return HudSetup.LoadAtIntro(25);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelCard), "04 §4: tarjeta de la Crecida al cargar el 25");
            Assert.That(boot.Flow.LevelCard!.IntroMechanic, Is.EqualTo(LevelCardInfo.StreakIntro));
            Assert.That(boot.View!.LevelCard.TutorialText, Is.EqualTo(UiText.TutorialStreak));
            Assert.That(boot.View.StreakHudBar.Visible, Is.False, "la barra, aún no");
            Assert.That(boot.TapAnywhere(), Is.True, "un toque la cierra");
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro));

            yield return HudSetup.LoadAtIntro(26);
            Assert.That(GameBootstrap.Current!.Flow!.Phase, Is.Not.EqualTo(FlowPhase.LevelCard), "solo en el 25");
        }

        // ============================================================================================ inicio

        [UnityTest]
        public IEnumerator Lobby_Widget_From25_WithItsCount_AndOutOfTheWay()
        {
            UseStore(Record(3, true));
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(29)));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            StreakWidget widget = lobby.StreakWidget;
            Assert.That(widget.Visible, Is.True, "06 §4.7: widget de Crecida (25+)");
            Assert.That(widget.Wins, Is.EqualTo(3));
            Assert.That(widget.ShieldShown, Is.True);
            Rect w = LobbyRect(lobby, widget.Root);
            Rect play = LobbyRect(lobby, (RectTransform)lobby.PlayButton.transform);
            Assert.That(w.xMax, Is.LessThanOrEqualTo(lobby.CanvasWidth + 0.01f));
            Assert.That(w.Overlaps(play), Is.False, "no pisa «Jugar»");
            Assert.That(w.yMin, Is.GreaterThan(lobby.SafeTop + 48f), "bajo la cabecera");
            Assert.That(w.yMax, Is.LessThanOrEqualTo(play.yMin - 36f + 0.5f), "D-050: el poste, en el agua, por encima de la viga del muelle");
            Assert.That(widget.CountText, Is.EqualTo("3"), "la cifra en su placa");
            Assert.That(widget.MarksLit, Is.EqualTo(1), "el hito 3, encendido");
        }

        [UnityTest]
        public IEnumerator Lobby_Widget_Before25_Hidden()
        {
            UseStore(Record(0, true));
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(12)));
            Assert.That(GameBootstrap.Current!.Lobby!.StreakWidget.Visible, Is.False);
        }

        // ============================================================================================ B10: hoja de compra

        [UnityTest]
        public IEnumerator StoreSheet_Cancel_StaysAboveTheBottomSafeInset()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 220f); // 220 px de barra de navegación abajo
            LaunchOptions.PurchasePresenterOverride = null; // la hoja de verdad, no la compra inmediata de FunnelSetup
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(AudioHooksTests.NoCanoesAt31(_clock)));
            GameBootstrap boot = GameBootstrap.Current!;
            LobbyScreen lobby = boot.Lobby!;
            lobby.CanoesButton.onClick.Invoke();
            yield return null;
            lobby.NoCanoes.InfiniteButton.onClick.Invoke();
            StoreSheet sheet = boot.StoreSheetView!;
            Assert.That(sheet.Visible, Is.True);
            // En dp del lienzo de la hoja (1080 × 2340 px a 3 px/dp: 360 × 780, con 220 / 3 dp de margen abajo), desde arriba: el panel y
            // «Cancelar» van anclados arriba a la izquierda (UiBuilder.Place).
            RectTransform cancel = (RectTransform)sheet.CancelButton.transform;
            RectTransform panel = (RectTransform)cancel.parent;
            float bottom = -panel.anchoredPosition.y - cancel.anchoredPosition.y + cancel.rect.height;
            Assert.That(bottom, Is.LessThanOrEqualTo(780f - (220f / 3f) + 0.01f), "«Cancelar», por encima de la barra de navegación (08 §13.1 #4)");
            Assert.That(-panel.anchoredPosition.y + panel.rect.height, Is.GreaterThanOrEqualTo(780f - 0.01f), "la hoja sigue llegando al borde de abajo");
            sheet.CancelButton.onClick.Invoke();
            Assert.That(sheet.Visible, Is.False);
        }

        // ---- Ayudas ----

        private StreakRecord Record(int wins, bool shield)
        {
            return new StreakRecord { DateLocal = Today, Wins = wins, ShieldAvailable = shield };
        }

        private InMemoryStreakStore UseStore(StreakRecord record)
        {
            var store = new InMemoryStreakStore(_clock, record);
            GameBootstrap.StreakStoreOverride = store;
            return store;
        }

        private static IEnumerator Win(int level)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
        }

        // El tablero 6 del tutorial con el desplazamiento que lo deja en level, bloqueado (como StuckCompactTests.Load31).
        private static IEnumerator LoadStuck(int level)
        {
            LaunchOptions.LevelOffsetOverride = level - 6;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1000));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            FunnelSetup.GetStuck(boot);
            PlayModeHelpers.Step(boot, 500);
        }

        private Profile ProfileAt(int nextLevel)
        {
            Profile profile = Wallet.NewProfile(_clock);
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        private static Rect CanvasRect(GameScreen view, RectTransform rt)
        {
            return VictorySetup.CanvasRect(rt, (RectTransform)view.Canvas.transform);
        }

        private static Rect LobbyRect(LobbyScreen lobby, RectTransform rt)
        {
            return VictorySetup.CanvasRect(rt, (RectTransform)lobby.Canvas.transform);
        }
    }
}
