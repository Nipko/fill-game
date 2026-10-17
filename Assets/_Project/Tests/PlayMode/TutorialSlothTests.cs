#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Boosters;
using AF.GameFlow;
using AF.Localization;
using AF.Presentation;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// TiTino en el tutorial de los niveles 1, 2, 3 y 6 (tarea TU1a; D-016, T2-A y T5-B; 02 §13; 04 §2.1; 06 §2.2; guion de la
    /// intro §14; propuesta de TiTino d.3 y paso 6e de (i)):
    /// <list type="bullet">
    /// <item>la instrucción del nivel (<see cref="TutorialInstruction"/>) sale tras la intro y cuando la tropa ya se fue, nunca
    /// antes (el orden de la intro, intacto), en la tarjeta de TiTino con su retrato y <c>sloth_present</c>, por la cola y la
    /// primera, durante su tiempo de lectura; sobre la franja del panorama si mide ≥ 64 dp y si no sobre el muelle;</item>
    /// <item>ninguna intersección con cuias (con su elevación), bandeja ni HUD, a 360 × 640 y 360 × 780, al 100 % y al 130 %; la
    /// frase, a 12 sp o más y en ≤ 2 líneas; sin toques (ningún gráfico es <c>raycastTarget</c>);</item>
    /// <item>en el 3, el tooltip de Marea, cuyo primer uso no descuenta (Marea ilimitada en 1-10);</item>
    /// <item>en el 2, «Fruta nueva: Taperebá» sale después de la instrucción (06 §2.2) [P];</item>
    /// <item>el 8 sigue con Capi (su aviso de la Cuia extra) y sin TiTino; los demás niveles, sin instrucción;</item>
    /// <item>tras Volver a la orilla en el 6, la instrucción otra vez y ningún aviso de TiTino del reinicio; un modal del embudo la
    /// oculta; sin TiTino, sale como aviso de texto.</item>
    /// </list>
    /// </summary>
    public sealed class TutorialSlothTests
    {
        [SetUp]
        public void SetUp()
        {
            Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.TutorialSlothOverride = null;
            GameScreen.SlothRestartNoticeOverride = null;
            GameScreen.FruitNewOverride = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Instruction_AfterTheIntroAndTheTroop_WithPortraitAndPresent([Values(1, 2, 3, 6)] int level)
        {
            yield return Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothNoticeCard card = WaitForInstruction(boot, "el " + level);
            string? key = TutorialInstruction.KeyFor(level);
            Assert.That(key, Is.Not.Null);
            Assert.That(card.Text, Is.EqualTo(Loc.T(key!)), "la instrucción del " + level);
            Assert.That(card.Text, Is.Not.Empty);
            Assert.That(card.Pose, Is.EqualTo(SlothPoseId.Present), "sloth_present");
            Assert.That(card.Portrait.sprite, Is.Not.Null, "su retrato");
            Assert.That(card.Lines, Is.InRange(1, SlothNoticeCard.MaxLines), "≤ 2 líneas (02 §13)");
            Assert.That(card.Label.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f), "nunca menos de 12 sp");
            Assert.That(card.DurationMs, Is.EqualTo((float)NoticeTiming.ReadMs(card.Text)), "dura por lectura (D-011)");
            Assert.That(view.IntroTroopVisible, Is.False, "cuando la tropa ya se fue");
            Assert.That(view.BannerText, Is.Null);
            Assert.That(view.LastTutorialProbe, Does.Contain("\"key\":\"" + key + "\""));
            Assert.That(view.LastTutorialProbe, Does.Contain("\"mode\":\"" + (view.TutorialSlothOnDock ? "dock" : "band") + "\""));
            Assert.That(view.Sighting.Visible || view.SightingPending, Is.False, "sin tarjeta del miquito en el " + level);
            Assert.That(view.StopLabelView.Visible, Is.False, "sin rótulo del lugar");
            AssertClear(view, "el " + level);

            // Se va sola tras su tiempo de lectura: no se queda fija.
            PlayModeHelpers.Step(boot, (int)card.DurationMs + 100);
            Assert.That(view.TutorialSlothVisible, Is.False, "se va sola");
            Assert.That(view.SlothNoticeVisible, Is.False);
            Assert.That(view.TutorialShownCount, Is.EqualTo(1), "una instrucción por nivel");
            Assert.That(view.SlothSession.LinesThisSession, Is.EqualTo(0), "no es una frase del banco de TiTino");
        }

        [UnityTest]
        public IEnumerator IntroOrder_Untouched_NothingBeforeTheTroopLeaves()
        {
            // Sin saltar la intro: ni durante «Llenar y teñir» ni con la tropa en la rama (guion §14).
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore(1, 0));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.TakeClock(boot);
            bool sawTroop = false;
            int t = 0;
            for (; t < 9000 && !view.TutorialSlothVisible; t += PlayModeHelpers.FrameMs)
            {
                sawTroop |= view.IntroTroopVisible;
                Assert.That(view.SlothNoticeVisible, Is.False, "nada de TiTino antes de tiempo, a los " + t + " ms");
                Assert.That(view.ToastText, Is.Null, "a los " + t + " ms");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(sawTroop, Is.True, "condición del test: la tropa estuvo en la rama");
            Assert.That(view.TutorialSlothVisible, Is.True, "la instrucción sale tras la intro");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.IntroTroopVisible, Is.False);
            TestContext.WriteLine("AF_TUTORIAL nivel 1: a los " + t + " ms de cargar");
        }

        [UnityTest]
        public IEnumerator Level3_UndoTooltip_FirstUseDoesNotCount()
        {
            yield return Load(3);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothNoticeCard card = WaitForInstruction(boot, "el 3");
            Assert.That(card.Text, Is.EqualTo(Loc.T(TutorialInstruction.UndoKey)), "el tooltip de Marea (04 §2.1)");
            Assert.That(view.LastTutorialProbe, Does.Contain("\"undo\":true"));
            LevelFlow flow = boot.Flow!;
            UndoStatus before = flow.UndoStatus;
            Assert.That(before.Unlimited, Is.True, "Marea ilimitada en 1-10");

            // Un vertido (la cuia 0 a la vacía 3) y Marea: el primer uso no descuenta.
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(boot.PressUndo(), Is.True, "Marea con la instrucción a la vista: no captura toques");
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            UndoStatus after = flow.UndoStatus;
            Assert.That(after.Unlimited, Is.True);
            Assert.That(after.FreeLeft, Is.EqualTo(before.FreeLeft), "el primer uso no descuenta cupo");
            Assert.That(after.Stock, Is.EqualTo(before.Stock), "ni stock");
            Assert.That(after.HasQuota, Is.True);
        }

        [UnityTest]
        public IEnumerator Level2_TheInstructionFirst_ThenFruitNew()
        {
            GameScreen.FruitNewOverride = true;
            yield return Load(2);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothNoticeCard card = WaitForInstruction(boot, "el 2");
            Assert.That(view.FruitNewView.Visible, Is.False, "una cada vez: primero el tutorial");
            int t = 0;
            for (; t < 9000 && !view.FruitNewView.Visible; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.FruitNewView.Visible && view.TutorialSlothVisible, Is.False, "nunca las dos a la vez");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialSlothVisible, Is.False, "la instrucción ya se fue");
            Assert.That(view.FruitNewView.Visible, Is.True, "«Fruta nueva: Taperebá» después (06 §2.2)");
            Assert.That(t, Is.GreaterThanOrEqualTo((int)card.DurationMs - 400), "tras la instrucción");
        }

        [UnityTest]
        public IEnumerator Level8_StaysWithCapi_AndOtherLevels_NoInstruction([Values(8, 4, 5, 7, 12)] int level)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            bool capi = false;
            for (int t = 0; t < 6000; t += PlayModeHelpers.FrameMs)
            {
                capi |= view.ToastText == UiText.ExtraCuiaGift;
                Assert.That(view.TutorialSlothVisible, Is.False, "nivel " + level + ": sin instrucción de TiTino");
                Assert.That(view.SlothNoticeVisible, Is.False, "nivel " + level + ": sin TiTino");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialShownCount, Is.EqualTo(0));
            Assert.That(view.LastTutorialProbe, Is.Null);
            if (level == 8)
            {
                Assert.That(capi, Is.True, "en el 8 habla Capi, que trae la cuia (04 §2.2)");
            }
        }

        [UnityTest]
        public IEnumerator Restart_InLevel6_TheInstructionAgain_NoRestartNotice()
        {
            GameScreen.SlothRestartNoticeOverride = true;
            yield return Load(6);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothNoticeCard card = WaitForInstruction(boot, "el 6");
            PlayModeHelpers.Step(boot, (int)card.DurationMs + 100);
            Assert.That(view.TutorialSlothVisible, Is.False);

            Assert.That(boot.PressRestart(), Is.True, "Volver a la orilla desde la cabecera");
            WaitForInstruction(boot, "el 6 tras el reinicio");
            Assert.That(view.LastSlothRestartLine.Speaks, Is.False, "sin aviso del reinicio: la instrucción ya lleva a TiTino (guion §14)");
            Assert.That(view.TutorialShownCount, Is.EqualTo(2), "en cada intento con intro");
            AssertClear(view, "el 6 tras el reinicio");
        }

        [UnityTest]
        public IEnumerator FunnelModal_HidesIt()
        {
            // El tablero del 6 se atasca en 3 → 4 (FunnelSetup.GetStuck): Aguas estancadas oculta la instrucción y no vuelve.
            yield return Load(6);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            WaitForInstruction(boot, "el 6");
            FunnelSetup.GetStuck(boot);
            Assert.That(view.TutorialSlothVisible, Is.False, "nunca sobre Aguas estancadas");
            Assert.That(view.SlothNoticeVisible, Is.False);
            PlayModeHelpers.Step(boot, 500);
            Assert.That(view.SlothNoticeVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator WithoutTiTino_TheInstructionAsAText()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(6, new MemoryProgressStore(6, 5));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.TakeClock(boot);
            view.SlothEnabled = false; // ui_sloth_enabled apagado (o, en release, sin su arte)
            boot.TapAnywhere();
            string text = Loc.T(TutorialInstruction.OneEmptyKey);
            bool seen = false;
            for (int t = 0; t < 6000 && !seen; t += PlayModeHelpers.FrameMs)
            {
                seen = view.ToastText == text;
                Assert.That(view.SlothNoticeVisible, Is.False, "sin TiTino");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(seen, Is.True, "la instrucción no se pierde: como aviso de texto");
            Assert.That(view.LastTutorialProbe, Does.Contain("\"mode\":\"toast\""));
            Assert.That(view.TutorialShownCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator NoIntersection_640And780_100And130_AndNoTouches([Values(1, 3, 6)] int level)
        {
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    string at = "el " + level + " a " + (small ? "640" : "780") + " al " + (scale > 1.01f ? "130" : "100") + " %";
                    GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
                    LaunchOptions.FontScaleOverride = scale;
                    yield return Load(level);
                    GameBootstrap boot = GameBootstrap.Current!;
                    GameScreen view = boot.View!;
                    SlothNoticeCard card = WaitForInstruction(boot, at);
                    TestContext.WriteLine("AF_TUTORIAL " + at + ": " + (view.TutorialSlothOnDock ? "muelle" : "panorama") + ", " + card.Lines
                        + " líneas, " + SlothFailTests.F(card.Height) + " dp, " + SlothFailTests.F(card.Label.fontSize) + " dp de texto");
                    Assert.That(card.Label.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * scale) - 0.01f), at + ": nunca menos de 12 sp");
                    Assert.That(card.Lines, Is.InRange(1, SlothNoticeCard.MaxLines), at);
                    var graphics = new List<Graphic>();
                    card.CollectGraphics(graphics);
                    foreach (Graphic g in graphics)
                    {
                        Assert.That(g.raycastTarget, Is.False, at + ": " + g.name + " sin raycast");
                    }

                    for (int t = 0; t < 1000; t += PlayModeHelpers.FrameMs)
                    {
                        AssertClear(view, at + " a los " + t + " ms");
                        PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                    }

                    LaunchOptions.Reset();
                    Hooks();
                }
            }
        }

        // ------------------------------------------------------------------------------------------- utilidades

        internal static void Hooks()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            GameScreen.TutorialSlothOverride = true;
        }

        /// <summary>El nivel <paramref name="level"/> en su primer intento, sin reloj y con la intro saltada.</summary>
        internal static IEnumerator Load(int level)
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        /// <summary>Espera (hasta 6 s) a que salga la instrucción en la tarjeta de TiTino; nunca antes de que se vaya la tropa.</summary>
        internal static SlothNoticeCard WaitForInstruction(GameBootstrap boot, string at)
        {
            GameScreen view = boot.View!;
            for (int t = 0; t < 6000 && !view.TutorialSlothVisible; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.SlothNoticeVisible, Is.False, at + ": nada antes de tiempo");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialSlothVisible, Is.True, at + ": la instrucción de TiTino sale");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), at + ": en PLAYING");
            Assert.That(view.IntroTroopVisible, Is.False, at + ": cuando la tropa ya se fue");
            PlayModeHelpers.Step(boot, 300); // termina su fundido de entrada
            return view.SlothNoticeView!;
        }

        /// <summary>La tarjeta no tapa cuias (con su elevación), bandeja ni HUD (<see cref="SlothFailTests.AssertNoticeClear"/>).</summary>
        internal static void AssertClear(GameScreen view, string at)
        {
            SlothFailTests.AssertNoticeClear(view, at);
        }
    }
}
