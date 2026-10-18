#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Boosters;
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
using UnityEngine.UI;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// TiTino presenta a Morpho en el nivel 12 (tarea TU1b; 04 §2.3, «Desbloqueo y tutorial»: «al cargar el nivel 12 TiTino
    /// presenta el botón con su retrato y <c>sloth_present</c>: "¿Atascado? La morpho conoce el río" y se acreditan 3 Morphos
    /// al stock»; D-016, T5-B; 02 §13; 06 §2.2):
    /// <list type="bullet">
    /// <item>al cargar el 12 por primera vez se acreditan 3 Morphos (la insignia de la ranura, «+3») antes de la intro, una
    /// sola vez: ni Volver a la orilla ni reabrir la app los repiten (marca <c>m</c> de <c>level_state</c>);</item>
    /// <item>la presentación sale con el mecanismo de TU1a: tras la intro y cuando la tropa ya se fue, nunca antes (el orden de
    /// la intro, intacto), en la tarjeta de TiTino con su retrato y <c>sloth_present</c>, en el sitio de 02 §13 y D-016 (T5-B):
    /// sobre la franja del panorama si mide ≥ 64 dp y, si no, sobre el muelle, con la cola en el eje del botón Pista [P]; si no
    /// cabe en una línea, corta tras «¿Atascado?»; sin tapar cuias, bandeja ni HUD, a 640 y 780 dp, al 100 y al 130 %, y sin capturar toques:
    /// con ella a la vista, Pista gasta el primer Morpho del regalo;</item>
    /// <item>sin la ranura Pista (si Remote Config moviera <c>hint_unlock_level</c>), el 12 no la enseña.</item>
    /// </list>
    /// </summary>
    public sealed class TutorialMorphoTests
    {
        private const int Level = TutorialInstruction.MorphoLevel;

        [SetUp]
        public void SetUp()
        {
            TutorialSlothTests.Hooks();
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
        public IEnumerator Level12_ThePresentation_AfterTheIntro_PointingAtTheHintButton()
        {
            GameScreen.SimulatedScreen = HudLabelTests.Screen780; // 360 × 780 dp, al 100 %
            yield return Load(new MemoryProfileStore(FunnelSetup.Profile(500)));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            SlothNoticeCard card = TutorialSlothTests.WaitForInstruction(boot, "el 12");
            Assert.That(TutorialInstruction.KeyFor(Level), Is.EqualTo(TutorialInstruction.MorphoKey));
            Assert.That(card.Text, Is.EqualTo(Loc.T(TutorialInstruction.MorphoKey)), "«¿Atascado? La morpho conoce el río» (04 §2.3)");
            Assert.That(card.Text, Is.EqualTo(CanonText), "texto canon de 04 §2.3 y 06 §2.2 en es-419, con sus espacios duros");
            Assert.That(card.Pose, Is.EqualTo(SlothPoseId.Present), "sloth_present");
            Assert.That(card.Portrait.sprite, Is.Not.Null, "su retrato");
            Assert.That(card.Lines, Is.InRange(1, SlothNoticeCard.MaxLines), "≤ 2 líneas (02 §13)");
            Assert.That(card.Label.fontSize, Is.GreaterThanOrEqualTo(UiBuilder.MinTextSp - 0.01f), "nunca menos de 12 sp");
            Assert.That(card.DurationMs, Is.EqualTo((float)NoticeTiming.ReadMs(card.Text)), "dura por lectura (D-011)");
            Assert.That(view.Tray.HintShown, Is.True, "la ranura Pista, en la bandeja desde el 12");
            Assert.That(view.LastTutorialProbe, Does.Contain("\"key\":\"" + TutorialInstruction.MorphoKey + "\""));
            Assert.That(view.LastTutorialProbe, Does.Contain("\"hint\":true"));
            Assert.That(view.LastTutorialProbe, Does.Contain("\"undo\":false"));
            Assert.That(view.TutorialPointsAtUndo, Is.False, "señala Pista, no Marea");
            Assert.That(view.Sighting.Visible || view.SightingPending, Is.False, "sin tarjeta del miquito");
            Assert.That(view.StopLabelView.Visible, Is.False, "sin rótulo del lugar");
            LogPlace(view, card, "el 12 a 780 al 100 %");
            AssertPlace(view, card, "el 12 a 780 al 100 %");
            AssertClear(view, "el 12");

            // Se va sola tras su tiempo de lectura: no se queda fija.
            PlayModeHelpers.Step(boot, (int)card.DurationMs + 100);
            Assert.That(view.TutorialSlothVisible, Is.False, "se va sola");
            Assert.That(view.TutorialShownCount, Is.EqualTo(1), "una instrucción por nivel");
            Assert.That(view.SlothSession.LinesThisSession, Is.EqualTo(0), "no es una frase del banco de TiTino");
        }

        [UnityTest]
        public IEnumerator IntroOrder_Untouched_TheGiftAtLoad_ThePresentationAfterTheTroop()
        {
            // Sin saltar la intro: los 3 Morphos ya están al cargar (LOADING), y nada de TiTino ni avisos durante «Llenar y teñir»
            // ni con la tropa en la rama (guion de la intro §14).
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(Level, new MemoryProgressStore(Level, Level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Hint), Is.EqualTo(boot.Flow!.Boosters.HintTutorialUnits), "acreditados al cargar");
            Assert.That(view.HintGiftedUnits, Is.EqualTo(3), "04 §2.3: 3 Morphos");
            bool sawTroop = false;
            int t = 0;
            for (; t < 12000 && !view.TutorialSlothVisible; t += PlayModeHelpers.FrameMs)
            {
                sawTroop |= view.IntroTroopVisible;
                Assert.That(view.SlothNoticeVisible, Is.False, "nada de TiTino antes de tiempo, a los " + t + " ms");
                Assert.That(view.ToastText, Is.Null, "ningún aviso de texto (el regalo no lleva uno propio), a los " + t + " ms");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(sawTroop, Is.True, "condición del test: la tropa estuvo en la rama");
            Assert.That(view.TutorialSlothVisible, Is.True, "la presentación sale tras la intro");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.IntroTroopVisible, Is.False);
            Assert.That(view.Tray.HintBadgeText, Is.EqualTo("+3"), "la insignia de la ranura, con los 3 regalados");
            TestContext.WriteLine("AF_TUTORIAL nivel 12: a los " + t + " ms de cargar, " + view.LastTutorialProbe);
        }

        [UnityTest]
        public IEnumerator Gift_Once_TheFirstSpentWithThePresentationInView_NotOnRestartNorOnReopen()
        {
            var store = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return Load(store);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Hint), Is.EqualTo(3));
            Assert.That(boot.Flow!.Attempt.HintGifted, Is.True);
            TutorialSlothTests.WaitForInstruction(boot, "el 12");

            // Con la presentación a la vista, Pista responde (la tarjeta no captura toques) y gasta del stock, no semillas. Sin dedo
            // guía: 06 §2.2 lo pide para la primera, pero aún no existe en el cliente (pendiente; pregunta para el dueño) [P].
            Assert.That(boot.PressHint(), Is.True, "Pista con la presentación a la vista");
            yield return WaitAnswer(boot);
            Assert.That(boot.Flow.LastHint!.Value.Answer, Is.EqualTo(HintAnswer.Pour));
            Assert.That(boot.Wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(2), "la primera, del regalo");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(500), "sin semillas");

            // Volver a la orilla: la intro otra vez y la presentación otra vez (como en el 6), pero ningún Morpho más.
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(boot.PressRestart(), Is.True, "Volver a la orilla");
            TutorialSlothTests.WaitForInstruction(boot, "el 12 tras Volver a la orilla");
            Assert.That(view.TutorialShownCount, Is.EqualTo(2), "en cada intento con intro");
            Assert.That(boot.Wallet.Stock.Get(BoosterKind.Hint), Is.EqualTo(2), "Volver a la orilla no lo repite");
            Assert.That(view.HintGiftedUnits, Is.EqualTo(3), "un solo regalo");
            Assert.That(LevelStateSnapshot.TryParseHistory(ProfileJson.Deserialize(store.Json!).LevelState!.History, out SavedHistory saved), Is.True);
            Assert.That(saved.HintGifted, Is.True, "la marca m, guardada en level_state");

            // Cerrar y abrir la app con el nivel en curso: se reabre sin intro y sin regalo.
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap again = GameBootstrap.Current!;
            Assert.That(again, Is.Not.SameAs(boot));
            Assert.That(again.App!.Screen, Is.EqualTo(AppScreen.Level), "reabre el nivel");
            Assert.That(again.Flow!.LevelNumber, Is.EqualTo(Level));
            Assert.That(again.Flow.Attempt.HintGifted, Is.True, "con la marca del regalo");
            Assert.That(again.Wallet!.Stock.Get(BoosterKind.Hint), Is.EqualTo(2), "reabrir no lo repite");
            Assert.That(again.View!.HintGiftedUnits, Is.EqualTo(0), "ningún regalo en la pantalla nueva");
        }

        [UnityTest]
        public IEnumerator NoGift_WhenThe12IsAlreadyBeaten_AndNoGiftElsewhere([Values(12, 13)] int level)
        {
            // El regalo es del nivel de desbloqueo aún no superado: el 12 con max_level 12 (rejugarlo) o el 13 no regalan.
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(level, new MemoryProgressStore(13, 12));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(level), "condición del test");
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Hint), Is.EqualTo(0), "nivel " + level + ": sin regalo");
            Assert.That(boot.View!.HintGiftedUnits, Is.EqualTo(0));
            Assert.That(boot.Flow.Attempt.HintGifted, Is.False);
        }

        [UnityTest]
        public IEnumerator WithoutTheHintSlot_NoPresentation()
        {
            // hint_unlock_level movido por Remote Config (14): el 12 no tiene ranura Pista, así que TiTino no presenta un botón
            // que no está [P]. Se prueba en el intento nuevo tras Volver a la orilla, con la configuración ya cambiada.
            yield return Load(new MemoryProfileStore(FunnelSetup.Profile(500)));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            TutorialSlothTests.WaitForInstruction(boot, "el 12");
            boot.Flow!.Boosters.HintUnlockLevel = 14;
            PlayModeHelpers.StepUntilIdle(boot, 4000);
            Assert.That(boot.PressRestart(), Is.True, "Volver a la orilla");
            for (int t = 0; t < 12000; t += PlayModeHelpers.FrameMs)
            {
                Assert.That(view.TutorialSlothVisible, Is.False, "sin ranura Pista, sin presentación (a los " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing), "condición del test: el intento nuevo, en juego");
            Assert.That(boot.Flow.Hint.State, Is.EqualTo(HintState.Hidden), "condición del test: sin ranura");
            Assert.That(view.TutorialShownCount, Is.EqualTo(1), "solo la del primer intento");
        }

        [UnityTest]
        public IEnumerator NoIntersection_640And780_100And130_AndNoTouches()
        {
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    string at = "el 12 a " + (small ? "640" : "780") + " al " + (scale > 1.01f ? "130" : "100") + " %";
                    GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
                    LaunchOptions.FontScaleOverride = scale;
                    yield return Load(new MemoryProfileStore(FunnelSetup.Profile(500)));
                    GameBootstrap boot = GameBootstrap.Current!;
                    GameScreen view = boot.View!;
                    SlothNoticeCard card = TutorialSlothTests.WaitForInstruction(boot, at);
                    LogPlace(view, card, at);
                    AssertPlace(view, card, at);
                    AssertBreakAfterTheQuestion(card, at);
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
                    TutorialSlothTests.Hooks();
                }
            }
        }

        [UnityTest]
        public IEnumerator OnTheDock_WhenTheBandIsUnder64dp_TheTailPointsAtTheHintButton()
        {
            // La otra rama de 02 §13 y D-016 (T5-B), con la franja forzada por debajo de 64 dp (TutorialBandOverrideDp): sobre el
            // muelle, pegada a la bandeja y con la cola en el eje del botón Pista, a 640 y 780 dp, al 100 y al 130 %.
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    string at = "el 12 a " + (small ? "640" : "780") + " al " + (scale > 1.01f ? "130" : "100") + " % con la franja a 40 dp";
                    GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
                    LaunchOptions.FontScaleOverride = scale;
                    GameScreen.TutorialBandOverrideDp = 40f;
                    yield return Load(new MemoryProfileStore(FunnelSetup.Profile(500)));
                    GameBootstrap boot = GameBootstrap.Current!;
                    GameScreen view = boot.View!;
                    SlothNoticeCard card = TutorialSlothTests.WaitForInstruction(boot, at);
                    LogPlace(view, card, at);
                    Assert.That(view.TutorialSlothOnDock, Is.True, at + ": franja < 64 dp, sobre el muelle");
                    Assert.That(view.LastTutorialProbe, Does.Contain("\"mode\":\"dock\""), at);
                    Assert.That(card.TailVisible, Is.True, at + ": con la cola");
                    Assert.That(view.TutorialPointsAtHint, Is.True, at + ": TiTino presenta el botón (04 §2.3): la cola, en el eje de Pista");
                    Assert.That(view.TutorialPointsAtUndo, Is.False, at + ": señala Pista, no Marea");
                    Assert.That(view.LastTutorialProbe, Does.Contain("\"over_hint\":true"), at);
                    AssertHintTail(view, at);
                    TutorialSlothTests.AssertDockAir(view, at);
                    TutorialSlothTests.AssertPostUnder(view, at);
                    for (int t = 0; t < 1000; t += PlayModeHelpers.FrameMs)
                    {
                        AssertClear(view, at + " a los " + t + " ms");
                        PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                    }

                    LaunchOptions.Reset();
                    GameScreen.TutorialBandOverrideDp = null;
                    TutorialSlothTests.Hooks();
                }
            }
        }

        [UnityTest]
        public IEnumerator WithoutTiTino_ThePresentationAsAText_AndTheGiftStillThere()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(500));
            yield return PlayModeHelpers.LoadGame(Level, new MemoryProgressStore(Level, Level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            PlayModeHelpers.TakeClock(boot);
            view.SlothEnabled = false; // ui_sloth_enabled apagado (o, en release, sin su arte)
            boot.TapAnywhere();
            string text = Loc.T(TutorialInstruction.MorphoKey);
            bool seen = false;
            for (int t = 0; t < 6000 && !seen; t += PlayModeHelpers.FrameMs)
            {
                seen = view.ToastText == text;
                Assert.That(view.SlothNoticeVisible, Is.False, "sin TiTino");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(seen, Is.True, "la presentación no se pierde: como aviso de texto");
            Assert.That(view.LastTutorialProbe, Does.Contain("\"mode\":\"toast\""));
            Assert.That(view.LastTutorialProbe, Does.Contain("\"over_hint\":false"));
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Hint), Is.EqualTo(3), "el regalo no depende de TiTino");
        }

        // ------------------------------------------------------------------------------------------- utilidades

        /// <summary>
        /// «¿Atascado? La morpho conoce el río» (04 §2.3 y 06 §2.2) tal como está en <c>strings.csv</c>: con espacios duros (U+00A0)
        /// entre las palabras de la segunda frase, para que, si no cabe en una línea, corte solo tras «¿Atascado?» (revisión de TU1b).
        /// </summary>
        internal const string CanonText = "¿Atascado? La morpho conoce el río";

        /// <summary>
        /// El sitio de 02 §13 y D-016 (T5-B), sin la excepción del 3 (04 §2.3 dice que TiTino «presenta» el botón): sobre la franja
        /// del panorama si mide ≥ 64 dp (y su alto + aire), sin cola y dentro de la franja; si no, sobre el muelle, con su aire y, si
        /// lleva cola, en el eje del botón Pista.
        /// </summary>
        internal static void AssertPlace(GameScreen view, SlothNoticeCard card, string at)
        {
            float band = view.Layout!.PanelY - view.HudBottomDp;
            bool bandFits = band >= Mathf.Max(GameScreen.SightingBandDp, card.Height + GameScreen.SightingBandAirDp);
            TestContext.WriteLine("AF_TUTORIAL " + at + ": franja de " + SlothFailTests.F(band) + " dp");
            if (bandFits)
            {
                Assert.That(view.TutorialSlothOnDock, Is.False, at + ": franja ≥ 64 dp, sobre el panorama (02 §13)");
                Assert.That(view.LastTutorialProbe, Does.Contain("\"mode\":\"band\""), at);
                Assert.That(card.TailVisible, Is.False, at + ": sin cola sobre el panorama");
                Assert.That(view.TutorialPointsAtHint, Is.False, at);
                Assert.That(view.LastTutorialProbe, Does.Contain("\"over_hint\":false"), at);
                Rect r = VictorySetup.CanvasRect(card.Root, view.NoticeLayer);
                Assert.That(r.yMin, Is.GreaterThanOrEqualTo(view.HudBottomDp - 0.5f), at + ": bajo el HUD");
                Assert.That(r.yMax, Is.LessThanOrEqualTo(view.Layout.PanelY + 0.5f), at + ": dentro de la franja, sobre el marco del tablero");
                return;
            }

            Assert.That(view.TutorialSlothOnDock, Is.True, at + ": franja < 64 dp, sobre el muelle");
            Assert.That(view.LastTutorialProbe, Does.Contain("\"mode\":\"dock\""), at);
            TutorialSlothTests.AssertDockAir(view, at);
            if (card.TailVisible)
            {
                Assert.That(view.TutorialPointsAtHint, Is.True, at + ": la cola señala Pista");
                AssertHintTail(view, at);
            }
        }

        /// <summary>En dos líneas, la primera es «¿Atascado?»: nunca deja «La» al final de la línea, lejos de «morpho».</summary>
        internal static void AssertBreakAfterTheQuestion(SlothNoticeCard card, string at)
        {
            TextMeshProUGUI label = card.Label;
            label.ForceMeshUpdate(true, true);
            TMP_TextInfo info = label.textInfo;
            if (info.lineCount < 2)
            {
                return;
            }

            TMP_LineInfo first = info.lineInfo[0];
            string line = label.text.Substring(first.firstCharacterIndex, first.lastVisibleCharacterIndex - first.firstCharacterIndex + 1);
            TestContext.WriteLine("AF_TUTORIAL " + at + ": primera línea «" + line + "»");
            Assert.That(line, Is.EqualTo("¿Atascado?"), at + ": corta tras «¿Atascado?»");
        }

        /// <summary>El 12 en su primer intento (max_level 11), con el perfil de <paramref name="store"/>, sin reloj y con la intro saltada.</summary>
        internal static IEnumerator Load(MemoryProfileStore store)
        {
            LaunchOptions.ProfileStoreOverride = store;
            yield return PlayModeHelpers.LoadGame(Level, new MemoryProgressStore(Level, Level - 1));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
        }

        /// <summary>Como <see cref="TutorialSlothTests.AssertClear"/>, con la cola medida contra el botón Pista.</summary>
        internal static void AssertClear(GameScreen view, string at)
        {
            SlothFailTests.AssertNoticeClear(view, at);
            SlothNoticeCard? card = view.SlothNoticeView;
            if (card != null && card.Visible && card.TailVisible)
            {
                AssertHintTail(view, at, false);
            }
        }

        /// <summary>
        /// La cola de la presentación de Morpho (como la del tooltip de Marea de TU1a): bajo la tarjeta, con la punta en el eje del
        /// botón Pista (a ≤ <see cref="GameScreen.TutorialPointToleranceDp"/> dp) y, con su canto, a ≥
        /// <see cref="GameScreen.TutorialTailUndoAirDp"/> dp sobre su marco y fuera de la bandeja.
        /// </summary>
        internal static void AssertHintTail(GameScreen view, string at, bool log = true)
        {
            SlothNoticeCard card = view.SlothNoticeView!;
            RectTransform space = view.NoticeLayer;
            Rect plate = VictorySetup.CanvasRect(card.Root, space);
            Rect tail = VictorySetup.CanvasRect(card.Tail, space);
            Rect hint = VictorySetup.CanvasRect((RectTransform)view.Tray.HintButton.transform, space);
            float tip = tail.yMax + SlothNoticeCard.EdgeDp;
            if (log)
            {
                TestContext.WriteLine("AF_TUTORIAL " + at + ": cola en x = " + SlothFailTests.F(tail.center.x) + " dp (Pista en "
                    + SlothFailTests.F(hint.center.x) + "), punta a " + SlothFailTests.F(hint.yMin - tip) + " dp del marco de Pista y a "
                    + SlothFailTests.F(view.TrayTopDp - tip) + " dp del borde de la bandeja");
            }

            Assert.That(tail.width, Is.EqualTo(SlothNoticeCard.TailWidthDp).Within(0.5f), at + ": ancho de la cola");
            Assert.That(tail.yMax - plate.yMax, Is.EqualTo(SlothNoticeCard.TailHeightDp).Within(0.5f), at + ": alto de la cola");
            Assert.That(tail.center.x, Is.EqualTo(card.TailCenterXDp).Within(0.5f), at + ": TailCenterXDp es el de la cola");
            Assert.That(Mathf.Abs(tail.center.x - hint.center.x), Is.LessThanOrEqualTo(GameScreen.TutorialPointToleranceDp), at + ": en el eje de Pista");
            Assert.That(hint.yMin - tip, Is.GreaterThanOrEqualTo(GameScreen.TutorialTailUndoAirDp - 0.5f), at + ": aire con el marco de Pista");
            Assert.That(tip, Is.LessThanOrEqualTo(view.TrayTopDp - 2f + 0.01f), at + ": fuera de la bandeja");
        }

        internal static void LogPlace(GameScreen view, SlothNoticeCard card, string at)
        {
            TestContext.WriteLine("AF_TUTORIAL " + at + ": «" + card.Text + "», " + (view.TutorialSlothOnDock ? "muelle" : "panorama") + ", "
                + card.Lines + " líneas a " + SlothFailTests.F(card.Label.fontSize) + " dp, " + SlothFailTests.F(card.Height) + " dp"
                + (view.TutorialPointsAtHint ? ", con la cola sobre Pista" : string.Empty) + ", " + view.LastTutorialProbe);
        }

        // Espera a que Morpho responda (su hilo), con el reloj del test.
        internal static IEnumerator WaitAnswer(GameBootstrap boot)
        {
            float start = Time.realtimeSinceStartup;
            while (boot.Flow!.HintThinking)
            {
                Assert.That(Time.realtimeSinceStartup - start, Is.LessThan(PlayModeHelpers.TimeoutSeconds), "Morpho no respondió");
                PlayModeHelpers.TickExact(boot, 4);
                yield return null;
            }
        }
    }

    /// <summary>
    /// Capturas de la presentación de Morpho en el 12 (tarea TU1b; 04 §2.3; 02 §13), en <c>Logs/snapshots/es-419/</c>:
    /// <c>tu1b_l12_{640|780}_{100|130}</c>, con TiTino en el sitio de 02 §13 y D-016 (T5-B; la franja del panorama si mide ≥ 64
    /// dp y, si no, el muelle), y <c>tu1b_l12_dock_{640|780}_{100|130}</c>, con la franja forzada por debajo de 64 dp: sobre el
    /// muelle, con su cola sobre el botón Pista (insignia «+3»). Cada captura comprueba los solapes y escribe una línea <c>AF_TUTORIAL</c>. Explícito: necesita gráficos (<c>unity test &lt;repo&gt; --mode PlayMode --filter
    /// AF.Tests.PlayMode.TutorialMorphoSnapshotTests</c> sin <c>-nographics</c>).
    /// </summary>
    [Explicit("Capturas de la presentación de Morpho en el 12 (TU1b); necesitan dispositivo gráfico.")]
    public sealed class TutorialMorphoSnapshotTests
    {
        [SetUp]
        public void SetUp()
        {
            TutorialSlothTests.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            SnapshotKit.Release();
            GameScreen.TutorialSlothOverride = null;
            GameScreen.TutorialBandOverrideDp = null;
            GameScreen.SimulatedScreen = null;
            MapArtRule.ForceProvisional = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator CaptureScreensAndTextScales()
        {
            foreach (bool small in new[] { false, true })
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    string suffix = (small ? "640" : "780") + "_" + (scale > 1.01f ? "130" : "100");
                    string dir = Prepare(small, scale);
                    yield return TutorialMorphoTests.Load(new MemoryProfileStore(FunnelSetup.Profile(500)));
                    yield return Capture(GameBootstrap.Current!, dir, "tu1b_l12_" + suffix, false);
                    Release();

                    // La otra rama de 02 §13, con la franja forzada por debajo de 64 dp: sobre el muelle, con la cola sobre Pista.
                    dir = Prepare(small, scale);
                    GameScreen.TutorialBandOverrideDp = 40f;
                    yield return TutorialMorphoTests.Load(new MemoryProfileStore(FunnelSetup.Profile(500)));
                    yield return Capture(GameBootstrap.Current!, dir, "tu1b_l12_dock_" + suffix, true);
                    GameScreen.TutorialBandOverrideDp = null;
                    Release();
                }
            }
        }

        private static IEnumerator Capture(GameBootstrap boot, string dir, string name, bool forcedDock)
        {
            GameScreen view = boot.View!;
            SlothNoticeCard card = TutorialSlothTests.WaitForInstruction(boot, name);
            SnapshotKit.Attach(boot);
            PlayModeHelpers.Step(boot, 600); // el gesto sloth_present termina de entrar (0,6 s)
            Assert.That(view.TutorialSlothVisible, Is.True, name + ": la presentación, a la vista");
            Assert.That(view.Tray.HintBadgeText, Is.EqualTo("+3"), name + ": la insignia con los 3 regalados");
            TutorialMorphoTests.AssertClear(view, name);
            TutorialMorphoTests.AssertBreakAfterTheQuestion(card, name);
            if (forcedDock)
            {
                Assert.That(view.TutorialSlothOnDock, Is.True, name + ": franja < 64 dp, sobre el muelle");
                Assert.That(view.TutorialPointsAtHint, Is.True, name + ": la cola señala Pista");
            }
            else
            {
                TutorialMorphoTests.AssertPlace(view, card, name);
            }

            if (view.TutorialSlothOnDock)
            {
                TutorialSlothTests.AssertDockAir(view, name);
                TutorialSlothTests.AssertPostUnder(view, name);
            }

            Debug.Log("AF_TUTORIAL " + name + ": «" + card.Text + "» en " + card.Lines + " líneas a " + SlothFailTests.F(card.Label.fontSize)
                + " dp, " + (view.TutorialSlothOnDock ? "sobre el muelle" : "sobre el panorama") + ", " + SlothFailTests.F(card.Height) + " dp, "
                + SlothFailTests.F(card.DurationMs) + " ms" + (view.TutorialPointsAtHint ? ", con la cola sobre Pista" : string.Empty)
                + ", " + view.LastTutorialProbe);
            yield return SnapshotKit.Wait(0.2f);
            SnapshotKit.Save(dir, name);
        }

        private static string Prepare(bool small, float scale)
        {
            TutorialSlothTests.Hooks();
            string dir = SnapshotKit.Prepare(LocLocale.SpanishLatinAmerica);
            GameScreen.SimulatedScreen = small ? HudLabelTests.Screen640 : HudLabelTests.Screen780;
            LaunchOptions.FontScaleOverride = scale;
            return dir;
        }

        private static void Release()
        {
            SnapshotKit.Release();
            LaunchOptions.Reset();
        }
    }
}
