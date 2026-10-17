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
    /// primera, durante su tiempo de lectura; sobre la franja del panorama si mide ≥ 64 dp y si no sobre el muelle (con la franja
    /// forzada por debajo de 64 dp: a ≥ 8 dp del marco del tablero y a ≥ 12 dp del marco de Marea, y el poste que tape se funde
    /// con ella);</item>
    /// <item>ninguna intersección con cuias (con su elevación), bandeja ni HUD, a 360 × 640 y 360 × 780, al 100 % y al 130 %; la
    /// frase, a 12 sp o más y en ≤ 2 líneas; sin toques (ningún gráfico es <c>raycastTarget</c>);</item>
    /// <item>en el 3, el tooltip de Marea no sale tras la intro sino tras el primer vertido que no acerca a la solución, medido con
    /// el solver (02 §13; 04 §2.1) [P]: un vertido que acerca no lo dispara y deshacer lo deja esperando; sale una sola vez, ni en
    /// los reintentos [P]; la tarjeta va sobre el muelle con una cola que señala el botón de Marea (en su eje y a ≥ 8 dp de su
    /// marco, fuera de la bandeja), a 640 y 780 dp, al 100 y al 130 %; su primer uso no descuenta (Marea ilimitada en 1-10);</item>
    /// <item>en el 2, solo la instrucción: ninguna «Fruta nueva» (una tarjeta por nivel como mucho; D-023, punto 39; 02 §4.2);</item>
    /// <item>el 8 sigue con Capi (su aviso de la Cuia extra) y sin TiTino; los demás niveles, sin instrucción;</item>
    /// <item>tras Volver a la orilla en el 6, la instrucción otra vez y ningún aviso de TiTino del reinicio; un modal del embudo y
    /// Volver a la orilla la ocultan, también sobre el muelle; sin TiTino, sale como aviso de texto.</item>
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
            GameScreen.TutorialBandOverrideDp = null;
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
            PourIfUndoLevel(boot, level);
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
        public IEnumerator Level3_UndoTooltip_AfterAPourThatDoesNotApproach_PointingAtTheButton_FirstUseDoesNotCount()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen780; // 360 × 780 dp, al 100 %
            yield return Load(3);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;

            // Tras la intro, con la tropa ya ida, nada: el tooltip espera al vertido que empeora (04 §2.1: «tras ejecutarlo»).
            for (int t = 0; t < 6000; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.SlothNoticeVisible, Is.False, "sin vertido, sin tooltip (a los " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.IntroTroopVisible, Is.False, "condición del test: la tropa ya se fue");
            Assert.That(view.TutorialWaitsForPour, Is.True, "el tooltip de Marea espera al vertido");
            Assert.That(view.TutorialShownCount, Is.EqualTo(0));
            LevelFlow flow = boot.Flow!;
            UndoStatus before = flow.UndoStatus;
            Assert.That(before.Unlimited, Is.True, "Marea ilimitada en 1-10");

            // Un vertido que acerca a la solución (el primero de la óptima, la cuia 0 a la vacía 3; tutorial_v0_report.md): nada.
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            for (int t = 0; t < 3000; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.SlothNoticeVisible, Is.False, "un vertido que acerca no lo dispara (a los " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialWaitsForPour, Is.True, "sigue esperando");

            // Se deshace: de vuelta al tablero del intento, sigue esperando.
            Assert.That(boot.PressUndo(), Is.True);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            PlayModeHelpers.Step(boot, 1000);
            Assert.That(flow.Session.AppliedPours, Is.EqualTo(0), "condición del test: el tablero del intento");
            Assert.That(view.SlothNoticeVisible, Is.False, "deshacer no lo dispara");
            Assert.That(view.TutorialWaitsForPour, Is.True);
            before = flow.UndoStatus;

            // El vertido tentador (la cuia 1 a la vacía 3, que no acerca): en cuanto termina, TiTino señala Marea.
            PourIfUndoLevel(boot, 3);
            SlothNoticeCard card = WaitForInstruction(boot, "el 3 tras el vertido que no acerca");
            Assert.That(view.TutorialWaitsForPour, Is.False);
            Assert.That(view.UndoTutorialDone, Is.True);
            Assert.That(card.Text, Is.EqualTo(Loc.T(TutorialInstruction.UndoKey)), "el tooltip de Marea (04 §2.1)");
            Assert.That(card.Pose, Is.EqualTo(SlothPoseId.Present));
            Assert.That(view.LastTutorialProbe, Does.Contain("\"undo\":true"));
            Assert.That(view.LastTutorialProbe, Does.Contain("\"over_undo\":true"));
            Assert.That(view.TutorialPointsAtUndo, Is.True, "TiTino señala el botón (04 §2.1): la cola, en su eje");
            Assert.That(card.TailVisible, Is.True, "con la cola");
            RectTransform space = view.NoticeLayer;
            Rect portrait = VictorySetup.CanvasRect(card.Portrait.rectTransform, space);
            Rect undo = VictorySetup.CanvasRect((RectTransform)view.Tray.UndoButton.transform, space);
            TestContext.WriteLine("AF_TUTORIAL el 3: " + (view.TutorialSlothOnDock ? "muelle" : "panorama") + ", retrato x = "
                + SlothFailTests.F(portrait.center.x) + " dp, cola x = " + SlothFailTests.F(card.TailCenterXDp) + " dp, Marea x = "
                + SlothFailTests.F(undo.xMin) + "-" + SlothFailTests.F(undo.xMax) + " dp, " + view.LastTutorialProbe);
            Assert.That(portrait.center.x, Is.EqualTo(card.PortraitCenterXDp).Within(1f), "PortraitCenterXDp es el del retrato");
            Assert.That(portrait.yMax, Is.LessThan(undo.yMin), "por encima del botón");
            Assert.That(view.TutorialSlothOnDock, Is.True, "junto a la bandeja: sobre el muelle, si cabe (cabe en el 3 a 780 dp)");
            AssertDockAir(view, "el 3");
            AssertClear(view, "el 3");
            AssertTail(view, "el 3");

            // Marea con el tooltip a la vista: el primer uso no descuenta.
            Assert.That(boot.PressUndo(), Is.True, "Marea con la instrucción a la vista: no captura toques");
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            UndoStatus after = flow.UndoStatus;
            Assert.That(after.Unlimited, Is.True);
            Assert.That(after.FreeLeft, Is.EqualTo(before.FreeLeft), "el primer uso no descuenta cupo");
            Assert.That(after.Stock, Is.EqualTo(before.Stock), "ni stock");
            Assert.That(after.HasQuota, Is.True);
        }

        [UnityTest]
        public IEnumerator Level3_UndoTooltip_OnlyTheFirstTime_NotOnRetries()
        {
            // «La primera vez» (02 §13): tras Volver a la orilla, el mismo vertido tentador ya no lo trae [P].
            yield return Load(3);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PourIfUndoLevel(boot, 3);
            WaitForInstruction(boot, "el 3");
            Assert.That(view.TutorialShownCount, Is.EqualTo(1));

            Assert.That(boot.PressRestart(), Is.True, "Volver a la orilla");
            LevelFlow flow = boot.Flow!;
            for (int t = 0; t < 9000 && (flow.Phase != FlowPhase.Playing || view.IntroTroopVisible); t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "condición del test: el intento nuevo, en juego");
            Assert.That(view.TutorialWaitsForPour, Is.False, "ya no espera ningún vertido");
            PourIfUndoLevel(boot, 3);
            for (int t = 0; t < 6000; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.SlothNoticeVisible, Is.False, "no vuelve en el reintento (a los " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialShownCount, Is.EqualTo(1), "una sola vez");
            Assert.That(view.UndoTutorialDone, Is.True);
        }

        [UnityTest]
        public IEnumerator Level2_OnlyTheInstruction_NoFruitNew()
        {
            // Como mucho una tarjeta por nivel tras la intro (D-023, punto 39; 02 §4.2; guion §14): en el 2 la instrucción gana
            // a «Fruta nueva: Taperebá», que no sale ni antes ni después (06 §2.2 la lista: contradicción registrada).
            GameScreen.FruitNewOverride = true;
            yield return Load(2);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothNoticeCard card = WaitForInstruction(boot, "el 2");
            Assert.That(card.Text, Is.EqualTo(Loc.T(TutorialInstruction.KeepOneEmptyKey)));
            for (int t = 0; t < 12000; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.FruitNewView.Visible, Is.False, "sin «Fruta nueva» en el 2 (a los " + t + " ms)");
                Assert.That(view.Sighting.Visible || view.SightingPending, Is.False, "ni la del miquito");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.TutorialSlothVisible, Is.False, "la instrucción ya se fue");
            Assert.That(view.LastFruitNewProbe, Is.Null, "ninguna sonda fruit_new");
            Assert.That(view.NoticesQueued, Is.EqualTo(0));
            Assert.That(view.TutorialShownCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Instruction_OnTheDock_WhenTheBandIsUnder64dp_WithItsAir()
        {
            // En 1, 2, 3 y 6 la franja del panorama mide más de 64 dp en las pantallas de prueba: se fuerza por debajo
            // (TutorialBandOverrideDp) para probar el otro sitio de TU1a, sobre el muelle, a 640 y 780 dp, al 100 y al 130 %.
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    string at = "el 6 a " + (small ? "640" : "780") + " al " + (scale > 1.01f ? "130" : "100") + " % con la franja a 40 dp";
                    GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
                    LaunchOptions.FontScaleOverride = scale;
                    GameScreen.TutorialBandOverrideDp = 40f;
                    yield return Load(6);
                    GameBootstrap boot = GameBootstrap.Current!;
                    GameScreen view = boot.View!;
                    SlothNoticeCard card = WaitForInstruction(boot, at);
                    Assert.That(view.TutorialSlothOnDock, Is.True, at + ": franja < 64 dp, sobre el muelle");
                    Assert.That(view.LastTutorialProbe, Does.Contain("\"mode\":\"dock\""), at);
                    Assert.That(view.TutorialPointsAtUndo, Is.False, at + ": solo el tooltip del 3 señala Marea");
                    Assert.That(card.TailVisible, Is.False, at + ": sin cola");
                    AssertDockAir(view, at);
                    AssertPostUnder(view, at);
                    TestContext.WriteLine("AF_TUTORIAL " + at + ": muelle, " + card.Lines + " líneas, " + SlothFailTests.F(card.Height) + " dp");
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

        [UnityTest]
        public IEnumerator Instruction_BandOf64dpOrMore_GoesOverThePanorama([Values(1, 6)] int level)
        {
            // El criterio de TU1a, con la franja medida: ≥ 64 dp (y su alto + aire) → sobre el panorama, centrada en la franja.
            yield return Load(level);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothNoticeCard card = WaitForInstruction(boot, "el " + level);
            float band = view.Layout!.PanelY - view.HudBottomDp;
            Assert.That(band, Is.GreaterThanOrEqualTo(GameScreen.SightingBandDp), "condición del test: franja de " + SlothFailTests.F(band) + " dp");
            Assert.That(view.TutorialSlothOnDock, Is.False, "sobre el panorama");
            Assert.That(view.LastTutorialProbe, Does.Contain("\"mode\":\"band\""));
            Rect r = VictorySetup.CanvasRect(card.Root, view.NoticeLayer);
            Assert.That(r.yMin, Is.GreaterThanOrEqualTo(view.HudBottomDp - 0.5f));
            Assert.That(r.yMax, Is.LessThanOrEqualTo(view.Layout.PanelY + 0.5f), "dentro de la franja, sobre el marco del tablero");
        }

        [UnityTest]
        public IEnumerator Instruction_OnTheDock_TheFunnelModalHidesIt()
        {
            // En 1-6 no puede salir ninguna burbuja «Te faltan {n}» (Marea ilimitada hasta el 10, Cuia extra desde el 8 y la
            // Luciérnaga solo con ocultos): la instrucción sobre el muelle comparte el guardián de la tarjeta del miquito y del
            // aviso de TiTino (KeepSightingClearOfBubbles, con _slothNoticeDockBottom), que prueba TI6b con una burbuja de verdad
            // (SlothFailTests.RestartNotice_OnTheDock_HidesWhenAMissingBubbleAppearsUnderIt). Aquí: el modal del embudo también la
            // quita sobre el muelle (Volver a la orilla: Instruction_OnTheDock_RestartHidesItUntilTheNewIntro).
            GameScreen.TutorialBandOverrideDp = 40f;
            yield return Load(6);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            WaitForInstruction(boot, "el 6 sobre el muelle");
            Assert.That(view.TutorialSlothOnDock, Is.True, "condición del test: sobre el muelle");
            FunnelSetup.GetStuck(boot);
            Assert.That(view.SlothNoticeVisible, Is.False, "nunca sobre Aguas estancadas");
            Assert.That(view.TutorialSlothOnDock, Is.False);
        }

        [UnityTest]
        public IEnumerator Instruction_OnTheDock_RestartHidesItUntilTheNewIntro()
        {
            // Volver a la orilla con la instrucción a la vista sobre el muelle: se va en el acto y solo vuelve tras la intro del
            // intento nuevo, cuando la tropa ya se fue (WaitForInstruction comprueba que no sale antes).
            GameScreen.TutorialBandOverrideDp = 40f;
            yield return Load(6);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            WaitForInstruction(boot, "el 6 sobre el muelle");
            Assert.That(view.TutorialSlothOnDock, Is.True, "condición del test: sobre el muelle");
            Assert.That(boot.PressRestart(), Is.True, "Volver a la orilla con la instrucción a la vista");
            Assert.That(view.SlothNoticeVisible, Is.False, "se va en el acto");
            Assert.That(view.TutorialSlothOnDock, Is.False);
            Assert.That(boot.Flow!.Phase, Is.Not.EqualTo(FlowPhase.Playing), "condición del test: la intro del intento nuevo");
            WaitForInstruction(boot, "el 6 tras Volver a la orilla");
            Assert.That(view.TutorialSlothOnDock, Is.True, "otra vez sobre el muelle");
            Assert.That(view.TutorialShownCount, Is.EqualTo(2));
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
                    PourIfUndoLevel(boot, level);
                    SlothNoticeCard card = WaitForInstruction(boot, at);
                    if (view.TutorialSlothOnDock)
                    {
                        AssertDockAir(view, at);
                    }

                    if (TutorialInstruction.PointsAtUndo(level))
                    {
                        Assert.That(view.TutorialPointsAtUndo, Is.True, at + ": la cola señala Marea (04 §2.1)");
                        Assert.That(view.LastTutorialProbe, Does.Contain("\"over_undo\":true"), at);
                        AssertTail(view, at);
                    }

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

        /// <summary>
        /// La tarjeta no tapa cuias (con su elevación), bandeja ni HUD (<see cref="SlothFailTests.AssertNoticeClear"/>); con cola,
        /// tampoco ella (<see cref="AssertTail"/>).
        /// </summary>
        internal static void AssertClear(GameScreen view, string at)
        {
            SlothFailTests.AssertNoticeClear(view, at);
            SlothNoticeCard? card = view.SlothNoticeView;
            if (card != null && card.Visible && card.TailVisible)
            {
                AssertTail(view, at, false);
            }
        }

        /// <summary>
        /// La cola del tooltip de Marea (revisión de TU1a): bajo la tarjeta, con la punta en el eje del botón de Marea (a ≤
        /// <see cref="GameScreen.TutorialPointToleranceDp"/> dp) y, con su canto, a ≥ <see cref="GameScreen.TutorialTailUndoAirDp"/>
        /// dp sobre su marco y fuera de la bandeja (el cuenco pintado por código asoma 2 dp sobre su borde; BoosterTray.Layout).
        /// </summary>
        internal static void AssertTail(GameScreen view, string at, bool log = true)
        {
            SlothNoticeCard card = view.SlothNoticeView!;
            RectTransform space = view.NoticeLayer;
            Rect plate = VictorySetup.CanvasRect(card.Root, space);
            Rect tail = VictorySetup.CanvasRect(card.Tail, space);
            Rect undo = VictorySetup.CanvasRect((RectTransform)view.Tray.UndoButton.transform, space);
            float tip = tail.yMax + SlothNoticeCard.EdgeDp;
            if (log)
            {
                TestContext.WriteLine("AF_TUTORIAL " + at + ": cola en x = " + SlothFailTests.F(tail.center.x) + " dp (Marea en "
                    + SlothFailTests.F(undo.center.x) + "), punta a " + SlothFailTests.F(undo.yMin - tip) + " dp del marco de Marea y a "
                    + SlothFailTests.F(view.TrayTopDp - tip) + " dp del borde de la bandeja");
            }

            Assert.That(tail.width, Is.EqualTo(SlothNoticeCard.TailWidthDp).Within(0.5f), at + ": ancho de la cola");
            Assert.That(tail.yMax - plate.yMax, Is.EqualTo(SlothNoticeCard.TailHeightDp).Within(0.5f), at + ": alto de la cola");
            Assert.That(tail.center.x, Is.EqualTo(card.TailCenterXDp).Within(0.5f), at + ": TailCenterXDp es el de la cola");
            Assert.That(Mathf.Abs(tail.center.x - undo.center.x), Is.LessThanOrEqualTo(GameScreen.TutorialPointToleranceDp), at + ": en el eje de Marea");
            Assert.That(tail.xMin, Is.GreaterThanOrEqualTo(plate.xMin + SlothNoticeCard.TailInsetDp - (SlothNoticeCard.TailWidthDp * 0.5f) - 0.5f), at);
            Assert.That(tail.xMax, Is.LessThanOrEqualTo(plate.xMax - SlothNoticeCard.TailInsetDp + (SlothNoticeCard.TailWidthDp * 0.5f) + 0.5f), at);
            Assert.That(undo.yMin - tip, Is.GreaterThanOrEqualTo(GameScreen.TutorialTailUndoAirDp - 0.5f), at + ": aire con el marco de Marea");
            Assert.That(tip, Is.LessThanOrEqualTo(view.TrayTopDp - 2f + 0.01f), at + ": fuera de la bandeja");
        }

        /// <summary>
        /// En el 3 (el tooltip de Marea), el vertido tentador que lo dispara: la cuia 1 (el taperebá de arriba) a la vacía 3, que no
        /// acerca a la solución (de los 6 vertidos iniciales, solo los de la cuia 0 acercan; tools/out/tutorial_v0_report.md),
        /// hasta que la vista lo termina (02 §13; 04 §2.1). En los demás niveles, nada.
        /// </summary>
        internal static void PourIfUndoLevel(GameBootstrap boot, int level)
        {
            if (!TutorialInstruction.PointsAtUndo(level))
            {
                return;
            }

            Assert.That(boot.TapCuia(1).Kind, Is.EqualTo(TapKind.Selected), "el 3: selecciona la cuia 1");
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Poured), "el 3: vierte en la vacía 3");
            PlayModeHelpers.StepUntilIdle(boot, 4000);
        }

        /// <summary>
        /// Sobre el muelle, la instrucción queda a ≥ <see cref="GameScreen.TutorialDockBoardAirDp"/> dp bajo el marco del tablero
        /// y, con su canto, a ≥ <see cref="GameScreen.TutorialDockUndoAirDp"/> dp sobre el marco del botón de Marea.
        /// </summary>
        internal static void AssertDockAir(GameScreen view, string at)
        {
            Assert.That(view.TutorialSlothOnDock, Is.True, at + ": sobre el muelle");
            RectTransform space = view.NoticeLayer;
            Rect r = VictorySetup.CanvasRect(view.SlothNoticeView!.Root, space);
            float panelBottom = view.Layout!.PanelBottom - view.BoardContainer.anchoredPosition.y;
            Rect undo = VictorySetup.CanvasRect((RectTransform)view.Tray.UndoButton.transform, space);
            float boardAir = r.yMin - panelBottom;
            float undoAir = undo.yMin - (r.yMax + SlothNoticeCard.EdgeDp);
            TestContext.WriteLine("AF_TUTORIAL " + at + ": muelle, " + SlothFailTests.F(boardAir) + " dp bajo el marco del tablero y "
                + SlothFailTests.F(undoAir) + " dp sobre el marco de Marea");
            Assert.That(boardAir, Is.GreaterThanOrEqualTo(GameScreen.TutorialDockBoardAirDp - 0.5f), at + ": aire con el marco del tablero");
            Assert.That(undoAir, Is.GreaterThanOrEqualTo(GameScreen.TutorialDockUndoAirDp - 0.5f), at + ": aire con el marco de Marea");
        }

        /// <summary>
        /// El poste del muelle (si se ve): si la instrucción lo tapa, se funde con ella (como con «Fruta nueva»); si no, se queda.
        /// </summary>
        internal static void AssertPostUnder(GameScreen view, string at)
        {
            RectTransform post = view.DecorRects[0];
            if (!post.gameObject.activeInHierarchy)
            {
                TestContext.WriteLine("AF_TUTORIAL " + at + ": sin poste en el muelle");
                return;
            }

            Canvas.ForceUpdateCanvases();
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect card = VictorySetup.CanvasRect(view.SlothNoticeView!.Root, root);
            card.yMax += SlothNoticeCard.EdgeDp;
            bool covers = card.Overlaps(VictorySetup.CanvasRect(post, root));
            float alpha = post.GetComponent<Image>().canvasRenderer.GetAlpha();
            TestContext.WriteLine("AF_TUTORIAL " + at + ": " + (covers ? "tapa el poste" : "no tapa el poste") + ", poste a " + SlothFailTests.F(alpha));
            Assert.That(alpha, Is.EqualTo(covers ? 0f : 1f).Within(1e-3f), at + (covers ? ": el poste tapado se funde con ella" : ": el poste se queda"));
        }
    }
}
