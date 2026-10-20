#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using AF.Ads;
using AF.Economy;
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
    /// TiTino en Nivel fallido y el aviso tras Volver a la orilla en 1-30 (tarea TI6b; propuesta de TiTino d.5 y paso 6b de
    /// (i); D-016, T5-B; 04 §2.7 y §6.6; guion de la intro §14; D-008; D-011):
    /// <list type="bullet">
    /// <item>Nivel fallido: <c>sloth_encourage</c> en el hueco izquierdo de la cesta y la frase bajo ella solo en el primer
    /// fallo; en los siguientes, solo el gesto; «Reintentar» a la vista y activo desde el primer fotograma; al volver de Sin
    /// canoas, lo mismo sin apuntar otra frase; con los mensajes apagados, solo el gesto; con <c>ui_sloth_enabled</c> apagado,
    /// nada;</item>
    /// <item>nunca en Aguas estancadas, ¿Seguro? ni Sin canoas;</item>
    /// <item>ninguna intersección de TiTino ni de su frase con la tarjeta (cabecera, X, franja, cesta, premio y Reintentar) a
    /// 360 × 640 y 360 × 780, al 100 % y al 130 %, en todos los fotogramas de su gesto, ni de la tarjeta con el pie del HUD ni
    /// con el <c>safeBottom</c> real cuando lleva la frase; la frase, a 12 sp o más y en ≤ 2 líneas; si al 130 % la tarjeta con
    /// la frase no cabe sobre <c>safeBottom</c> (un recorte inferior grande), solo el gesto, sin apuntar frase en la sesión;</item>
    /// <item>el aviso tras Volver a la orilla: en 1-30, en el primer reinicio del nivel (desde la cabecera o desde el pie de Aguas
    /// estancadas), sin instrucción del tutorial, por la cola, con su retrato de 40 dp y su frase <c>sloth_line_restart_*</c>,
    /// cuando la tropa ya se fue y durante <see cref="NoticeTiming.ReadMs(string)"/>; nunca con el modal abierto, ni en el
    /// segundo reinicio, ni en los niveles del tutorial (1, 2, 3, 6, 8 y 12), ni desde el 31; sin tapar cuias, bandeja ni HUD;
    /// sobre el muelle, se oculta si sale debajo una burbuja «Te faltan {n}»;</item>
    /// <item>su rama sale de detrás del marco: el rig, bajo el marco de la tarjeta, y la rama, con el remate dentro del canto y en
    /// corteza (la provisional, teñida);</item>
    /// <item>sin toques: ningún gráfico de TiTino es <c>raycastTarget</c>.</item>
    /// </list>
    /// </summary>
    public sealed class SlothFailTests
    {
        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            MapArtRule.ForceProvisional = true;
            GameScreen.SlothRestartNoticeOverride = true;
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
        }

        [TearDown]
        public void TearDown()
        {
            GameScreen.SlothRestartNoticeOverride = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        // ------------------------------------------------------------------------------------------- Nivel fallido

        [UnityTest]
        public IEnumerator FirstFail_SpeaksUnderTheBasket_LaterFailsOnlyTheGesture()
        {
            yield return LoadLevel31(5);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            FailToLevelFailed(boot, true);
            LevelFailedScreen failed = view.Failed;
            Assert.That(failed.SlothVisible, Is.True, "TiTino en Nivel fallido");
            Assert.That(failed.Sloth!.Pose, Is.EqualTo(SlothPoseId.Encourage), "sloth_encourage");
            Assert.That(failed.SlothSize, Is.InRange(LevelFailedScreen.SlothMinSizeDp, LevelFailedScreen.SlothSizeDp));
            SlothLine line = view.LastSlothFailLine;
            Assert.That(line.Speaks, Is.True, "primer fallo del nivel: frase");
            Assert.That(line.Moment, Is.EqualTo(SlothMoment.Fail));
            Assert.That(failed.PhraseShown, Is.True);
            Assert.That(failed.PhraseText, Is.EqualTo(Loc.T(line.Key!)));
            Assert.That(failed.PhraseLines, Is.InRange(1, LevelFailedScreen.PhraseMaxLines));
            Assert.That(view.SlothSession.LinesThisSession, Is.EqualTo(1));
            Assert.That(failed.RetryButton.gameObject.activeInHierarchy, Is.True, "Reintentar sale a la vez que hoy");
            Assert.That(failed.RetryButton.interactable, Is.True);
            AssertFailedClear(view, "primer fallo");

            // Segundo fallo del nivel: solo el gesto, sin frase.
            boot.PressRetry();
            PlayModeHelpers.Step(boot, 1200);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            FailToLevelFailed(boot, false);
            Assert.That(failed.SlothVisible, Is.True);
            Assert.That(view.LastSlothFailLine.Speaks, Is.False, "en los siguientes fallos, solo el gesto (d.5)");
            Assert.That(failed.PhraseShown, Is.False);
            Assert.That(view.SlothSession.LinesThisSession, Is.EqualTo(1));
            AssertFailedClear(view, "segundo fallo");
        }

        [UnityTest]
        public IEnumerator NeverInTheStuckModal_GiveUpConfirm_OrNoCanoes_AndBackFromNoCanoesTheSamePhrase()
        {
            yield return LoadLevel31(1);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            AssertNoSlothAnywhere(view, "Aguas estancadas");
            Assert.That(boot.PressGiveUp(), Is.True);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.GiveUpConfirm));
            PlayModeHelpers.Step(boot, 300);
            AssertNoSlothAnywhere(view, "¿Seguro?");
            Assert.That(boot.ConfirmGiveUp(), Is.True);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            string phrase = view.Failed.PhraseText;
            Assert.That(phrase, Is.Not.Empty);

            // Con 0 canoas, Reintentar abre Sin canoas: TiTino no está.
            Assert.That(boot.PressRetry(), Is.True);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            PlayModeHelpers.Step(boot, 300);
            AssertNoSlothAnywhere(view, "Sin canoas");

            // «Llenar canoas» vuelve a Nivel fallido: el mismo fallo, la misma frase, sin apuntar otra.
            view.NoCanoes.RefillButton.onClick.Invoke();
            PlayModeHelpers.Step(boot, 20);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(view.Failed.SlothVisible, Is.True);
            Assert.That(view.Failed.PhraseText, Is.EqualTo(phrase));
            Assert.That(view.SlothSession.LinesThisSession, Is.EqualTo(1), "no se apunta dos veces");
        }

        [UnityTest]
        public IEnumerator WithoutMessages_OnlyTheGesture_AndDisabled_Nothing()
        {
            yield return LoadLevel31(5);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            view.SlothVoice = false;
            FailToLevelFailed(boot, true);
            Assert.That(view.Failed.SlothVisible, Is.True, "Mensajes de TiTino apagado: el gesto");
            Assert.That(view.Failed.PhraseShown, Is.False, "sin frase");
            Assert.That(view.LastSlothFailLine.Speaks, Is.False);

            yield return LoadLevel31(5);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            view.SlothEnabled = false;
            FailToLevelFailed(boot, true);
            Assert.That(view.Failed.SlothVisible, Is.False, "ui_sloth_enabled apagado: nada");
            Assert.That(view.Failed.Sloth, Is.Null, "ni se crea");
            Assert.That(view.Failed.PhraseShown, Is.False);
        }

        [UnityTest]
        public IEnumerator NoIntersection_640And780_100And130()
        {
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    string at = (small ? "640" : "780") + " al " + (scale > 1.01f ? "130" : "100") + " %";
                    GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
                    LaunchOptions.FontScaleOverride = scale;
                    yield return LoadLevel31(5);
                    GameBootstrap boot = GameBootstrap.Current!;
                    GameScreen view = boot.View!;
                    FailToLevelFailed(boot, true);
                    LevelFailedScreen failed = view.Failed;
                    TestContext.WriteLine("AF_SLOTH_FAIL " + at + ": TiTino " + F(failed.SlothSize) + " dp en " + F(failed.Sloth!.Root.anchoredPosition.x)
                        + "/" + F(-failed.Sloth.Root.anchoredPosition.y) + ", frase " + (failed.PhraseShown ? failed.PhraseLines + " líneas a "
                        + F(failed.PhraseFontDp) + " dp" : "no") + ", tarjeta de " + F(failed.Panel.sizeDelta.y) + " dp desde " + F(-failed.Panel.anchoredPosition.y));
                    for (int t = 0; t <= 1600; t += PlayModeHelpers.FrameMs)
                    {
                        AssertFailedClear(view, at + " a los " + t + " ms");
                        PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                    }

                    if (failed.PhraseShown)
                    {
                        Assert.That(failed.PhraseFontDp, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * scale) - 0.01f), at + ": nunca menos de 12 sp");
                    }
                    else
                    {
                        Assert.That(view.LastSlothFailLine.Speaks, Is.False, at + ": sin sitio, solo el gesto (y no se apunta frase)");
                    }

                    LaunchOptions.Reset();
                    FunnelSetup.Hooks();
                    MapArtRule.ForceProvisional = true;
                    GameScreen.SlothRestartNoticeOverride = true;
                }
            }
        }

        [UnityTest]
        public IEnumerator At130_ThePhraseOnlyIfTheCardFitsOverTheRealSafeBottom_IfNotOnlyTheGesture([Values(40, 220)] int safeBottomDp)
        {
            // 360 × 780 dp al 130 % con un recorte inferior de safeBottomDp dp (3 px/dp). Con 40 dp la tarjeta con la frase cabe
            // sobre ese safeBottom real (no sobre el mínimo de 24 dp). Con 220 dp no cabe (d.5: «si al 130 % no cabe sobre
            // safeBottom, solo el gesto»): sale TiTino sin frase, no se apunta ninguna en la sesión y «Reintentar» sigue a la
            // vista y activo.
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, safeBottomDp * 3f);
            LaunchOptions.FontScaleOverride = 1.3f;
            yield return LoadLevel31(5);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            string at = "360 × 780 al 130 % con safeBottom de " + safeBottomDp + " dp";
            Assert.That(view.CardSafeBottom, Is.EqualTo((float)safeBottomDp).Within(0.01f), at + ": condición del test");
            int linesBefore = view.SlothSession.LinesThisSession;

            // La frase que toca en este primer fallo del 31 con la sesión de ahora (la misma decisión que GameScreen.Funnel.cs):
            // así se ve que la razón de no decirla es el sitio, y no los topes, los mensajes o el texto.
            var context = view.SlothSession.Context.WithFail(new SlothFailFacts(true, false, 0, true));
            SlothLine wanted = SlothLines.ForFail(31, in context);
            Assert.That(wanted.Key, Is.Not.Null, at + ": condición del test, en el 31 toca frase");
            string wantedText = Loc.T(wanted.Key!);
            FailToLevelFailed(boot, true);
            LevelFailedScreen failed = view.Failed;
            TestContext.WriteLine("AF_SLOTH_FAIL " + at + ": frase " + (failed.PhraseShown ? failed.PhraseLines + " líneas a " + F(failed.PhraseFontDp) + " dp" : "no")
                + ", tarjeta de " + F(failed.Panel.sizeDelta.y) + " dp desde " + F(-failed.Panel.anchoredPosition.y) + ", límites " + F(failed.TopLimitDp)
                + "-" + F(failed.BottomLimitDp));
            Assert.That(failed.SlothVisible, Is.True, at + ": TiTino sale");
            Assert.That(failed.Sloth!.Pose, Is.EqualTo(SlothPoseId.Encourage), at + ": sloth_encourage");
            Assert.That(failed.RetryButton.gameObject.activeInHierarchy, Is.True, at + ": Reintentar a la vista");
            Assert.That(failed.RetryButton.interactable, Is.True, at + ": y activo");
            Assert.That(failed.BottomLimitDp, Is.EqualTo(view.CardCanvasHeight - safeBottomDp).Within(0.01f), at + ": el límite es el safeBottom real");
            for (int t = 0; t <= 1600; t += PlayModeHelpers.FrameMs)
            {
                AssertFailedClear(view, at + " a los " + t + " ms");
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
            }

            if (safeBottomDp == 40)
            {
                Assert.That(failed.PhraseShown, Is.True, at + ": la tarjeta con la frase cabe");
                Assert.That(view.LastSlothFailLine.Key, Is.EqualTo(wanted.Key), at + ": la frase que tocaba");
                Assert.That(failed.PhraseText, Is.EqualTo(wantedText));
                Assert.That(view.SlothSession.LinesThisSession, Is.EqualTo(linesBefore + 1));
                Assert.That(view.SlothSession.PreviousScreenSpoke, Is.True);
                Assert.That(-failed.Panel.anchoredPosition.y + failed.Panel.sizeDelta.y, Is.GreaterThan(view.CardCanvasHeight - 220f),
                    at + ": condición del test, con la frase la tarjeta pasaría del safeBottom de 220 dp");
            }
            else
            {
                Assert.That(failed.PhraseShown, Is.False, at + ": sin sitio sobre safeBottom, solo el gesto");
                Assert.That(failed.PhraseText, Is.Empty);
                Assert.That(view.LastSlothFailLine.Speaks, Is.False, at + ": no se apunta frase");
                Assert.That(view.LastSlothFailLine.Gesture, Is.EqualTo(SlothGesture.Encourage), at + ": el gesto sí");
                Assert.That(view.SlothSession.LinesThisSession, Is.EqualTo(linesBefore), at + ": la sesión no cuenta ninguna frase");
                Assert.That(view.SlothSession.PreviousScreenSpoke, Is.False, at + ": esta pantalla no habló");
            }

            // La frase que tocaba cabe con 40 dp y no con 220 (al final: PhraseFits recoloca la tarjeta para medir).
            Assert.That(failed.PhraseFits(wantedText, view.CanvasWidth, view.CardCanvasHeight), Is.EqualTo(safeBottomDp == 40),
                at + ": la frase «" + wantedText + "» " + (safeBottomDp == 40 ? "cabe" : "no cabe") + " sobre safeBottom");
        }

        [UnityTest]
        public IEnumerator NeverCapturesTouches()
        {
            yield return LoadLevel31(5);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            FailToLevelFailed(boot, true);
            var graphics = new List<Graphic>();
            view.Failed.CollectSlothGraphics(graphics);
            Assert.That(graphics.Count, Is.GreaterThan(SlothRig.BodyGraphics), "el rig y la frase");
            foreach (Graphic g in graphics)
            {
                Assert.That(g.raycastTarget, Is.False, g.name + ": sin raycast");
            }

            view.ShowSlothNotice("Volver a empezar también es avanzar."); // la crea (en el juego, nunca con el modal abierto)
            view.SlothNoticeView?.CollectGraphics(graphics);
            foreach (Graphic g in graphics)
            {
                Assert.That(g.raycastTarget, Is.False, g.name + ": sin raycast");
            }
        }

        // ------------------------------------------------------------------------------------------- aviso tras Volver a la orilla

        [UnityTest]
        public IEnumerator Restart_1To30_FirstRestartOnly_ByTheQueue_ForItsReadTime()
        {
            yield return LoadLevel(4, 0);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.PressRestart(), Is.True, "Volver a la orilla desde la cabecera");
            SlothNoticeCard card = WaitForNotice(boot, "primer reinicio del 4");
            SlothLine line = view.LastSlothRestartLine;
            Assert.That(line.Moment, Is.EqualTo(SlothMoment.Restart));
            Assert.That(card.Text, Is.EqualTo(Loc.T(line.Key!)));
            Assert.That(card.Lines, Is.InRange(1, SlothNoticeCard.MaxLines));
            Assert.That(card.DurationMs, Is.EqualTo((float)NoticeTiming.ReadMs(card.Text)), "dura por lectura (D-011)");
            Assert.That(card.Portrait.sprite, Is.Not.Null, "su retrato");
            Assert.That(view.IntroTroopVisible, Is.False, "cuando la tropa ya se fue");
            Assert.That(view.BannerText, Is.Null, "y tras el rótulo del intento");
            Assert.That(view.SlothSession.LinesThisSession, Is.EqualTo(1));
            AssertNoticeClear(view, "4");

            // Se va solo tras su tiempo de lectura.
            PlayModeHelpers.Step(boot, (int)card.DurationMs + 100);
            Assert.That(view.SlothNoticeVisible, Is.False);

            // Segundo reinicio del mismo nivel: sin aviso.
            Assert.That(boot.PressRestart(), Is.True);
            PlayModeHelpers.Step(boot, 6000);
            Assert.That(view.SlothNoticeVisible, Is.False, "solo en el primer reinicio del nivel");
            Assert.That(view.LastSlothRestartLine.Gesture, Is.EqualTo(SlothGesture.None));
        }

        [UnityTest]
        public IEnumerator Restart_FromTheStuckModalFooter_NeverOverTheModal()
        {
            // El 10 con el tablero 6 del tutorial (desplazamiento 4): 3 → 4 deja Aguas estancadas; en 1-30 su pie es Volver a
            // la orilla (fallo duro de la rama C, sin canoa).
            yield return LoadLevel(10, 4);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            Assert.That(view.SlothNoticeVisible, Is.False, "nunca sobre Aguas estancadas");
            Assert.That(boot.PressRestart(), Is.True, "pie del modal: Volver a la orilla");
            SlothNoticeCard card = WaitForNotice(boot, "reinicio desde el modal del 10");
            Assert.That(card.Text, Is.EqualTo(Loc.T(view.LastSlothRestartLine.Key!)));
            AssertNoticeClear(view, "10");
        }

        [UnityTest]
        public IEnumerator Restart_NoNotice_InTutorialLevels_From31_OrWithoutMessages([Values(3, 6, 8, 12, 31, 0)] int level)
        {
            // 0: el 5 con los mensajes de TiTino apagados (sin frase, no hay aviso).
            int played = level == 0 ? 5 : level;
            yield return LoadLevel(played, played == 31 ? 25 : 0);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            if (level == 0)
            {
                view.SlothVoice = false;
            }

            Assert.That(boot.PressRestart(), Is.True);
            for (int t = 0; t < 6000; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(view.SlothNoticeVisible, Is.False, "nivel " + played + ": sin aviso a los " + t + " ms");
            }

            Assert.That(view.LastSlothRestartLine.Speaks, Is.False);
            Assert.That(view.SlothSession.LinesThisSession, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator RestartNotice_NoIntersection_640And780_100And130()
        {
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    string at = (small ? "640" : "780") + " al " + (scale > 1.01f ? "130" : "100") + " %";
                    GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
                    LaunchOptions.FontScaleOverride = scale;
                    yield return LoadLevel(9, 0);
                    GameBootstrap boot = GameBootstrap.Current!;
                    GameScreen view = boot.View!;
                    Assert.That(boot.PressRestart(), Is.True);
                    if (small && !NoticeShows(boot))
                    {
                        // 16:9 desde DC3 (360 × 640 dp; D-028): el 9 (2 filas) no deja sitio ni en la franja ni sobre el muelle y el
                        // aviso no sale (la regla de siempre: si no cabe, no sale). Pendiente del dueño (open issue de DC3).
                        Assert.That(view.SlothNoticeVisible, Is.False, at);
                        TestContext.WriteLine("AF_SLOTH_RESTART " + at + ": no cabe y no sale (franja " + F(view.Layout!.PanelY - view.TopRowDp - 48f)
                            + " dp; muelle " + F(view.TrayTopDp - view.Layout.PanelBottom) + " dp)");
                        LaunchOptions.Reset();
                        FunnelSetup.Hooks();
                        MapArtRule.ForceProvisional = true;
                        GameScreen.SlothRestartNoticeOverride = true;
                        continue;
                    }

                    SlothNoticeCard card = WaitForNotice(boot, at);
                    TestContext.WriteLine("AF_SLOTH_RESTART " + at + ": " + (view.SlothNoticeOnDock ? "muelle" : "panorama") + ", " + card.Lines
                        + " líneas, " + F(card.Height) + " dp, " + F(card.Label.fontSize) + " dp de texto");
                    Assert.That(card.Label.fontSize, Is.GreaterThanOrEqualTo((UiBuilder.MinTextSp * scale) - 0.01f), at + ": nunca menos de 12 sp");
                    for (int t = 0; t < 1000; t += PlayModeHelpers.FrameMs)
                    {
                        AssertNoticeClear(view, at + " a los " + t + " ms");
                        PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                    }

                    LaunchOptions.Reset();
                    FunnelSetup.Hooks();
                    MapArtRule.ForceProvisional = true;
                    GameScreen.SlothRestartNoticeOverride = true;
                }
            }
        }

        [UnityTest]
        public IEnumerator RestartNotice_OnTheDock_HidesWhenAMissingBubbleAppearsUnderIt()
        {
            // Como la tarjeta del miquito (MinTextSizeTests): con el aviso de TiTino sobre el muelle (130 %), una burbuja «Te
            // faltan {n}» que sale después debajo (tras gastar las semillas que quedaban) se taparía; el aviso no puede subir sin
            // tapar las cuias y se oculta en el acto (GameScreen.Notices.cs, KeepSightingClearOfBubbles).
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = 1.3f;
            yield return LoadDockBoard(150);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Tray.TryGetMissingTop(out _), Is.False, "condición del test: con 150 semillas, ninguna burbuja");
            Assert.That(view.ShowSlothNotice(Loc.T("sloth_line_restart_02")), Is.True, "sin burbujas, el aviso sale");
            PlayModeHelpers.Step(boot, 250);
            Assert.That(view.SlothNoticeOnDock, Is.True, "condición del test: el aviso, sobre el muelle");
            Assert.That(view.SlothNoticeVisible, Is.True, "sin burbujas, sigue");
            AssertNoticeClear(view, "el aviso sin burbujas");
            Assert.That(boot.Wallet!.Apply(Transaction.Buy(SeedSink.ExtraCuia)).IsApplied, Is.True, "gasta las 150 semillas");
            PlayModeHelpers.Step(boot, 2 * PlayModeHelpers.FrameMs);
            Assert.That(view.Tray.TryGetMissingTop(out _), Is.True, "salen las burbujas «Te faltan {n}»");
            Assert.That(view.SlothNoticeVisible, Is.False, "se oculta antes que tapar la burbuja");
            Assert.That(view.SlothNoticeOnDock, Is.False);
        }

        // ------------------------------------------------------------------------------------------- utilidades

        // El tablero 5 del tutorial como nivel 46 (2 filas; la tarjeta del miquito, sobre el muelle al 130 %), sin video y con
        // seeds semillas, en juego con la vista quieta (como MinTextSizeTests.LoadDockBoard).
        private static IEnumerator LoadDockBoard(int seeds)
        {
            LaunchOptions.LevelOffsetOverride = 41;
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds));
            yield return PlayModeHelpers.LoadGame(46, new MemoryProgressStore(46, 45));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            PlayModeHelpers.Step(boot, 1500);
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.View!.SlothNoticeVisible, Is.False, "condición del test: sin aviso propio");
        }

        /// <summary>El 31 con el tablero 6 del tutorial (3 → 4 bloquea), <paramref name="canoes"/> canoas, sin reloj y jugando.</summary>
        internal static IEnumerator LoadLevel31(int canoes)
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500, canoes));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        /// <summary>El nivel <paramref name="level"/> con el desplazamiento <paramref name="offset"/>, sin reloj y jugando.</summary>
        internal static IEnumerator LoadLevel(int level, int offset)
        {
            if (offset > 0)
            {
                LaunchOptions.LevelOffsetOverride = offset;
            }

            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(level, level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 3000); // la intro y la tropa se van; los avisos del primer intento, también
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        /// <summary>Aguas estancadas → ¿Seguro? → Nivel fallido (desde el 31, en el tablero 6 del tutorial).</summary>
        internal static void FailToLevelFailed(GameBootstrap boot, bool first)
        {
            FunnelSetup.GetStuck(boot);
            Assert.That(boot.PressGiveUp(), Is.True);
            Assert.That(boot.ConfirmGiveUp(), Is.True);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(boot.Flow.Attempt.FailsLevel == 1, Is.EqualTo(first), "primer fallo del nivel");
            Assert.That(boot.View!.Failed.Visible, Is.True);
        }

        // Tras un reinicio, espera (hasta 6 s) a que salga el aviso de TiTino.
        // Espera al aviso de TiTino como WaitForNotice, sin exigirlo: true si sale.
        private static bool NoticeShows(GameBootstrap boot)
        {
            GameScreen view = boot.View!;
            for (int t = 0; t < 6000 && !view.SlothNoticeVisible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            return view.SlothNoticeVisible;
        }

        private static SlothNoticeCard WaitForNotice(GameBootstrap boot, string at)
        {
            GameScreen view = boot.View!;
            for (int t = 0; t < 6000 && !view.SlothNoticeVisible; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(boot.Flow!.Phase == FlowPhase.Playing || !view.SlothNoticeVisible, Is.True);
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.SlothNoticeVisible, Is.True, at + ": el aviso de TiTino sale");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing), at + ": en PLAYING");
            PlayModeHelpers.Step(boot, 300); // termina su fundido de entrada
            return view.SlothNoticeView!;
        }

        /// <summary>TiTino ni nada suyo a la vista: ni en la tarjeta de Nivel fallido, ni el aviso, ni en la victoria.</summary>
        internal static void AssertNoSlothAnywhere(GameScreen view, string at)
        {
            Assert.That(view.Failed.SlothVisible, Is.False, at + ": TiTino no está");
            Assert.That(view.Failed.PhraseShown, Is.False, at);
            Assert.That(view.SlothNoticeVisible, Is.False, at + ": sin aviso de TiTino");
            Assert.That(view.SlothView.Visible, Is.False, at);
        }

        /// <summary>
        /// Ni TiTino ni su frase se cruzan con lo demás de la tarjeta de Nivel fallido; con la frase, la tarjeta va entre el pie del
        /// HUD y safeBottom.
        /// </summary>
        internal static void AssertFailedClear(GameScreen view, string at)
        {
            LevelFailedScreen failed = view.Failed;
            RectTransform space = failed.Root;
            var mine = new List<Rect>();
            failed.CollectSlothRects(space, mine);
            var parts = new List<(string, RectTransform)>();
            failed.CollectParts(parts);
            Rect phrase = default;
            bool hasPhrase = failed.PhraseShown;
            if (hasPhrase)
            {
                phrase = VictorySetup.CanvasRect(failed.PhraseLabel!.rectTransform, space);
            }

            Rect panel = VictorySetup.CanvasRect(failed.Panel, space);
            foreach ((string name, RectTransform rt) in parts)
            {
                Rect r = VictorySetup.CanvasRect(rt, space);
                foreach (Rect m in mine)
                {
                    if (m.width > 0.01f && m.height > 0.01f)
                    {
                        Assert.That(Overlaps(m, r), Is.False, at + ": TiTino " + m + " se cruza con " + name + " " + r);
                    }
                }

                if (hasPhrase && name != "frase")
                {
                    Assert.That(Overlaps(phrase, r), Is.False, at + ": la frase " + phrase + " se cruza con " + name + " " + r);
                }
            }

            foreach (Rect m in mine)
            {
                if (m.width > 0.01f && m.height > 0.01f)
                {
                    Assert.That(m.xMin >= panel.xMin - 0.01f && m.xMax <= panel.xMax + 0.01f && m.yMin >= panel.yMin - 0.01f
                        && m.yMax <= panel.yMax + 0.01f, Is.True, at + ": TiTino " + m + " dentro de la tarjeta " + panel);
                }
            }

            if (failed.SlothVisible)
            {
                // La rama sale de detrás del marco: el rig va bajo el marco y la rama empieza dentro del canto, en corteza.
                Transform frame = failed.Panel.Find("Frame");
                Assert.That(frame, Is.Not.Null);
                SlothRig rig = failed.Sloth!;
                Assert.That(rig.Root.GetSiblingIndex(), Is.LessThan(frame.GetSiblingIndex()), at + ": TiTino bajo el marco");
                Rect branch = VictorySetup.CanvasRect(rig.BranchImage.rectTransform, space);
                Assert.That(branch.xMin, Is.InRange(panel.xMin - 0.01f, panel.xMin + LevelFailedScreen.SlothBranchTuckDp + 0.01f),
                    at + ": el remate de la rama " + branch + ", bajo el canto del marco de " + panel);
                Assert.That(branch.xMax, Is.GreaterThan(panel.xMin + LevelFailedScreen.CardFrameDp), at + ": la rama asoma");
                Assert.That(rig.BranchImage.color, Is.EqualTo(LevelFailedScreen.SlothBranchTint), at + ": rama provisional en corteza");
            }

            if (hasPhrase)
            {
                // Con los límites que usa la tarjeta (SetBounds), que son el pie del HUD y el safeBottom real de la pantalla (no el
                // mínimo de 24 dp): con un recorte inferior mayor, la tarjeta tampoco lo invade.
                float safeBottom = view.CardCanvasHeight - view.CardSafeBottom;
                Assert.That(failed.TopLimitDp, Is.EqualTo(view.HudBottomDp).Within(0.01f), at + ": límite de arriba, el pie del HUD");
                Assert.That(failed.BottomLimitDp, Is.EqualTo(safeBottom).Within(0.01f), at + ": límite de abajo, el safeBottom real");
                Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(view.HudBottomDp - 0.5f), at + ": con la frase, bajo el pie del HUD");
                Assert.That(panel.yMax, Is.LessThanOrEqualTo(safeBottom + 0.5f), at + ": y sobre safeBottom (" + F(view.CardSafeBottom) + " dp)");
            }
        }

        /// <summary>El aviso de TiTino no tapa cuias (con su elevación), la bandeja ni el HUD.</summary>
        internal static void AssertNoticeClear(GameScreen view, string at)
        {
            SlothNoticeCard? card = view.SlothNoticeView;
            if (card == null || !card.Visible)
            {
                return;
            }

            RectTransform space = view.NoticeLayer;
            Rect r = VictorySetup.CanvasRect(card.Root, space);
            r.yMax += SlothNoticeCard.EdgeDp;
            Assert.That(r.yMin, Is.GreaterThanOrEqualTo(view.HudBottomDp - 0.5f), at + ": bajo el HUD " + r);
            Assert.That(r.yMax, Is.LessThanOrEqualTo(view.TrayTopDp + 0.5f), at + ": sobre la bandeja " + r);
            BoardLayoutResult layout = view.Layout!;
            float shift = view.BoardContainer.anchoredPosition.y;
            for (int i = 0; i < view.Views.Length; i++)
            {
                CuiaView cuia = view.Views[i];
                float half = layout.CuiaWidth * 0.5f;
                float top = layout.FootY[i] - (cuia.HeightU * cuia.Unit) - CuiaView.LiftDp - shift;
                float bottom = layout.FootY[i] - shift;
                Rect c = Rect.MinMaxRect(layout.CenterX[i] - half, top, layout.CenterX[i] + half, bottom);
                Assert.That(Overlaps(r, c), Is.False, at + ": el aviso " + r + " tapa la cuia " + i + " " + c);
            }
        }

        private static bool Overlaps(Rect a, Rect b)
        {
            return a.xMin < b.xMax - 0.01f && b.xMin < a.xMax - 0.01f && a.yMin < b.yMax - 0.01f && b.yMin < a.yMax - 0.01f;
        }

        internal static string F(float v)
        {
            return v.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
