#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Ads;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Presentation;
using AF.Presentation.Mechanics;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Arreglos de la prueba de la tanda 2 en el teléfono (T52; hoja <c>docs/dev/traspaso/pruebas-telefono-2026-10-13.md</c> y
    /// decisiones del dueño del 2026-10-13 sobre las preguntas 41-43 de <c>docs/GDD.md</c> §6.3):
    /// <list type="bullet">
    /// <item><description>pregunta 42 (b): el aviso a la vista se oculta en el fotograma en que se abre Aguas estancadas, no
    /// vuelve al cerrarlo, y el que esperaba en la cola sale después, como antes;</description></item>
    /// <item><description>observación 1 / pregunta 41 (4): el aviso no tapa las burbujas «Te faltan {n}» de la bandeja, con 1 y
    /// con 3 filas (el 135), y sin burbujas tampoco tapa la repisa de abajo ni la ficha «?×n» del 135;</description></item>
    /// <item><description>pregunta 43: el banner de mundo admite la escala de fuente de 02 §12 hasta el 130 %, con las mismas
    /// comprobaciones que la tarjeta previa;</description></item>
    /// <item><description>observación 4: tras ver la victoria del 40, el banner del 41 entra con «Siguiente» sin esperar
    /// (<c>wait_ms</c> ≤ 16 ms y sin respaldo): la superficie de madera ya se pintó en otro hilo con su arte;</description></item>
    /// <item><description>observación 5: por el Remolino, la cuia tocada se traga el chorro (remolino en su superficie y espiral
    /// que gira, se ilumina y late) y la pareja se ilumina y late al recibir, con destellos en el fondo; dentro del presupuesto
    /// de 2.800 ms, sin asignaciones por fotograma y, con Reducir movimiento, sin giros, latidos ni partículas.</description></item>
    /// </list>
    /// </summary>
    public sealed class PhoneFixesTests
    {
        private static ProceduralArt? s_art;
        private MechanicTestKit.Rig? _rig;

        private static ProceduralArt Art => s_art ??= new ProceduralArt("white");

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            s_art?.Dispose();
            s_art = null;
        }

        // ---- 1 · Pregunta 42 (b) ----

        [UnityTest]
        public IEnumerator Notice_VisibleWhenAguasEstancadasOpens_HidesInThatFrame_NeverComesBack_TheQueuedOneComesAfter()
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1000));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            PlayModeHelpers.Step(boot, 50);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));

            // Un aviso a la vista (3 s) y otro esperando su turno.
            string shown = UiText.RevealMissing(70);
            string waiting = UiText.RevealMissing(50);
            view.ShowRevealMissing(70);
            view.ShowRevealMissing(50);
            Assert.That(view.ToastText, Is.EqualTo(shown));
            Assert.That(view.NoticesQueued, Is.EqualTo(1));

            // 3 → 4 bloquea el tablero 6 del tutorial: el bloqueo queda pendiente mientras se anima el vertido y después se abre
            // Aguas estancadas. En el fotograma en que se abre, el aviso ya no se ve.
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            string? lastPlaying = null;
            int t = 0;
            while (flow.Phase == FlowPhase.Playing && t < 5000)
            {
                lastPlaying = view.ToastText;
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(lastPlaying, Is.EqualTo(shown), "condición del test: el aviso se veía en el último fotograma en juego (" + t + " ms)");
            Assert.That(view.ToastText, Is.Null, "se oculta en el fotograma en que se abre el modal (pregunta 42 b)");
            Assert.That(view.NoticeToast.Visible, Is.False);
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "el que esperaba sigue en la cola");

            // Con el modal abierto no vuelve ni sale nada.
            for (int ms = 0; ms < 4000; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(view.ToastText, Is.Null, "con Aguas estancadas abierto (" + ms + " ms)");
            }

            // Al volver a jugar (el rescate): sale el que esperaba, después «¡Una cuia más!», y el que se ocultó no vuelve.
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.ToastText, Is.EqualTo(waiting), "el que esperaba sale al volver a jugar");
            var seen = new List<string>();
            for (int ms = 0; ms < 12000; ms += PlayModeHelpers.FrameMs)
            {
                string? now = view.ToastText;
                if (now != null && (seen.Count == 0 || seen[seen.Count - 1] != now))
                {
                    seen.Add(now);
                }

                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(seen, Is.EqualTo(new[] { waiting, UiText.RescueApplied }), "el aviso ocultado se descartó: no vuelve");
        }

        // ---- 3 · Observación 1 / pregunta 41 (4): el aviso y las burbujas «Te faltan {n}» ----

        [UnityTest]
        public IEnumerator Toast_DoesNotCoverTheMissingBubble_OneRow()
        {
            // El tablero del 6 (5 cuias en 1 fila) como nivel 46, sin semillas ni video: «Te faltan 150» sobre «+1 cuia».
            yield return LoadWithBubbles(46, 40, 0);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(1));
            Assert.That(view.Tray.ExtraShowsMissing, Is.True, "la burbuja de «+1 cuia»");
            view.ShowRevealMissing(70);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(view.ToastText, Is.EqualTo(UiText.RevealMissing(70)));
            AssertToastClearOfBubbles(view, "1 fila");
            Assert.That(view.NoticeToast.Height, Is.EqualTo(Toast.DefaultHeightDp), "con sitio, del alto de siempre");
            Assert.That(view.NoticeToast.Top, Is.GreaterThanOrEqualTo(BoardFloor(view)), "y sin tapar el tablero");
            AssertToastTextFits(view);

            // Sin burbujas, en su sitio de siempre: 8 dp sobre la bandeja, 56 dp de alto.
            yield return LoadWithBubbles(46, 40, 1000);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            Assert.That(view.Tray.ExtraShowsMissing, Is.False);
            view.ShowRevealMissing(70);
            PlayModeHelpers.Step(boot, 200);
            Rect undo = VictorySetup.CanvasRect((RectTransform)view.UndoButton.transform, Root(view));
            float trayTop = undo.yMin - 4f;
            Assert.That(view.NoticeToast.Top, Is.EqualTo(trayTop - 64f).Within(0.05f), "como antes: 64 dp sobre la bandeja");
            Assert.That(view.NoticeToast.Height, Is.EqualTo(Toast.DefaultHeightDp));
        }

        [UnityTest]
        public IEnumerator Toast_DoesNotCoverTheMissingBubbles_ThreeRows_Level135()
        {
            // El 135 (3 filas) sin semillas ni video: «Te faltan 150» y «Te faltan 120», en dos líneas (las dos ranuras vecinas),
            // como en el teléfono.
            yield return LoadWithBubbles(135, 0, 0);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.Layout!.Rows, Is.EqualTo(3), "el 135, de 3 filas");
            Assert.That(view.Tray.ExtraShowsMissing && view.Tray.RevealShowsMissing, Is.True, "las dos burbujas");
            view.ShowRevealMissing(120);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(view.ToastText, Is.EqualTo(UiText.RevealMissing(120)));
            AssertToastClearOfBubbles(view, "3 filas");
            AssertToastTextFits(view);

            // No tapa ninguna cuia: queda bajo el pie de la última fila. El compromiso (capturas 92 de T52): con las dos burbujas
            // de dos líneas no cabe entre ellas y la repisa, así que tapa lo menos posible del canto de la repisa de abajo.
            BoardLayoutResult l = view.Layout;
            float foot = l.RowFootY[l.Rows - 1];
            Assert.That(view.NoticeToast.Top, Is.GreaterThan(foot), "no tapa las cuias de la fila de abajo");
            float covered = Mathf.Max(0f, BoardFloor(view) - view.NoticeToast.Top);
            Assert.That(view.NoticeToast.Height, Is.EqualTo(view.NoticeToast.MinHeightDp(view.CanvasWidth)).Within(0.05f), "encogido a su texto");
            TestContext.WriteLine("135 con burbujas: el aviso tapa " + covered.ToString("0.0") + " dp de la repisa de abajo (con su sombra).");

            // Sin burbujas, encogido a su texto entre la repisa (con su sombra y la ficha «?×n») y la bandeja: no la tapa.
            yield return LoadWithBubbles(135, 0, 1000);
            boot = GameBootstrap.Current!;
            view = boot.View!;
            Assert.That(view.Tray.ExtraShowsMissing || view.Tray.RevealShowsMissing, Is.False);
            view.ShowRevealMissing(120);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(view.NoticeToast.Top, Is.GreaterThanOrEqualTo(BoardFloor(view) + GameScreen.NoticeBoardGapDp - 0.05f), "sin tapar la repisa ni la ficha «?×n»");
            Assert.That(view.NoticeToast.Height, Is.LessThan(Toast.DefaultHeightDp).And.GreaterThanOrEqualTo(view.NoticeToast.MinHeightDp(view.CanvasWidth) - 0.05f));
            AssertToastTextFits(view);
        }

        // ---- 2 · Pregunta 43: el banner de mundo admite la escala de fuente de 02 §12 ----

        [UnityTest]
        public IEnumerator Banner_FontScale100And130_NoOverflowNorOverlap()
        {
            // 02 §12: hasta el 130 % sin desbordes ni recortes; los contenedores crecen. El 41 a 100 % y a 130 % a 360 × 780 dp y
            // en 16:9 (438,75 × 780 dp), y el 11, el 81 y el 141 a 130 %.
            foreach (Vector4 screen in new[] { HudLabelTests.Screen780, HudLabelTests.Screen640 })
            {
                GameScreen.SimulatedScreen = screen;
                yield return CardSetup.Load(41);
                GameScreen view = GameBootstrap.Current!.View!;
                string where = "41 a " + screen.x + " × " + screen.y;
                Assert.That(view.WorldBannerView.Visible, Is.True, where);
                view.BannerFontScale = 1f;
                PlayModeHelpers.Step(GameBootstrap.Current!, 50);
                AssertBannerFits(view, 1f, where + " al 100 %");
                float baseHeight = view.WorldBannerView.Panel.rect.height;
                view.BannerFontScale = 1.3f;
                PlayModeHelpers.Step(GameBootstrap.Current!, 50);
                Assert.That(view.BannerFontScale, Is.EqualTo(1.3f));
                AssertBannerFits(view, 1.3f, where + " al 130 %");
                Assert.That(view.WorldBannerView.Panel.rect.height, Is.GreaterThan(baseHeight), "la tarjeta crece: " + where);
                view.BannerFontScale = 2f;
                Assert.That(view.BannerFontScale, Is.EqualTo(LevelCardView.MaxFontScale), "por encima del 130 %, el tope (02 §12)");
            }

            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            foreach (int level in new[] { 11, 81, 141 })
            {
                yield return CardSetup.Load(level);
                GameScreen view = GameBootstrap.Current!.View!;
                Assert.That(view.WorldBannerView.Visible, Is.True, "nivel " + level);
                view.BannerFontScale = 1.3f;
                PlayModeHelpers.Step(GameBootstrap.Current!, 50);
                AssertBannerFits(view, 1.3f, "nivel " + level + " al 130 %");
            }
        }

        // ---- 4 · Observación 4: wait_ms del banner al entrar con «Siguiente» ----

        [UnityTest]
        public IEnumerator Banner_AfterSeeingTheVictoryOf40_Next_EntersWithoutWaiting()
        {
            // La hoja de la tanda 2 esperaba wait_ms ≈ 0 entrando con «Siguiente» y el teléfono midió 66-111 ms: el arte se pedía
            // en la victoria, pero la superficie de madera de la tarjeta se pintaba en el hilo principal al abrir el banner (unos
            // 220 ms en el Editor) y wait_ms lo contaba. Ahora la pinta el mismo trabajo del arte, en otro hilo.
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtTimeoutMsOverride = 60000f;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            yield return PlayModeHelpers.LoadGame(40, new MemoryProgressStore(40, 39));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            view.CardArt.ReleaseBannerArt(); // el arte es compartido entre escenas: nada de un banner de otro test
            if (flow.Phase == FlowPhase.LevelCard)
            {
                Assert.That(boot.TapAnywhere(), Is.True, "la tarjeta previa del 40, si la hay");
            }

            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(view.CardArt.BannerArtPending, Is.True, "en la victoria del 40, el arte del banner del 41, pedido");
            Assert.That(view.CardArt.WaitForBannerArt(30000), Is.True, "pintado en otro hilo mientras se ve la victoria");
            var surface = new Vector2(LevelCardView.MaxWidthDp - 16f, 281f);

            Assert.That(boot.PressNext(), Is.True);
            for (int ms = 0; ms < 5000 && flow.LevelNumber != 41; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            WorldBannerView banner = view.WorldBannerView;
            Assert.That(flow.LevelNumber, Is.EqualTo(41));
            Assert.That(flow.WorldBanner, Is.EqualTo(3));
            Assert.That(banner.AsyncArt, Is.True);
            Assert.That(banner.CardShown && banner.ArtReady, Is.True, "entra en el acto, con su arte");
            Assert.That(banner.SurfaceSizeDp, Is.EqualTo(surface), "la superficie compartida de 281 dp");
            Assert.That(view.CardArt.HasModalSurface(surface.x, surface.y), Is.True, "la superficie llegó con el arte");
            Assert.That(banner.ArtWaitMs, Is.LessThanOrEqualTo(16f), "wait_ms ≈ 0 (≤ 16 ms en el Editor)");
            Assert.That(view.LastWorldBannerArtProbe, Does.Contain("\"level\":41").And.Contain("\"fallback\":\"none\""));
            TestContext.WriteLine("AF_STATE world_banner_art tras «Siguiente»: " + view.LastWorldBannerArtProbe);
            PlayModeHelpers.Step(boot, 1500);
            yield return WorldBannerSetup.CloseThenTheIntro(boot, 41, 3);
        }

        // ---- 5 · Observación 5: el Remolino se lee ----

        [UnityTest]
        public IEnumerator Remolino103_TheTouchedCuiaSwallowsTheStream_ThePartnerLightsUp_WithinTheBudget()
        {
            yield return MechanicTestKit.LoadPlaying(103);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            BoardAnimator animator = view.Animator;
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.ThroughRemolino);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            Pour pour = path![path.Length - 1];
            BoardState before = boot.Flow!.Session.Current;
            float tapAt = animator.Clock;
            PourVisual visual = MechanicTestKit.TapPour(boot, pour, out TapResult poured);
            int touched = pour.Target;
            int partner = poured.Pour.Receiver;
            Assert.That(partner, Is.Not.EqualTo(touched));
            RemolinoOverlay intake = Whirl(view.Views[touched]);
            RemolinoOverlay receive = Whirl(view.Views[partner]);

            // La cabeza del chorro llega a la cuia tocada: se lo traga.
            int t = 0;
            while (!intake.IntakeActive && t < 2000)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
            }

            float arrive = (view.Timings.TravelMs * BoardAnimator.StreamStartFraction) + BoardAnimator.StreamGrowMs;
            Assert.That(intake.IntakeActive, Is.True, "la cuia tocada se traga el chorro");
            Assert.That(t, Is.InRange(Mathf.FloorToInt(arrive), Mathf.CeilToInt(arrive) + PlayModeHelpers.FrameMs), "cuando la cabeza del chorro llega");
            Assert.That(intake.IntakeSwirlVisible, Is.True, "el remolino de su superficie");
            Assert.That(intake.IntakeSwirlCenterU.x, Is.EqualTo(0f).Within(1e-3f), "en su eje, donde acaba el chorro");
            Assert.That(intake.IntakeSwirlCenterU.y, Is.EqualTo(CuiaGeometry.CavityBottomU + before[touched].Count).Within(0.01f), "sobre su líquido");
            Assert.That(intake.IntakeSwirlSizeU.x, Is.LessThanOrEqualTo(2f * CuiaGeometry.CavityHalfWidthU), "dentro de la cavidad");
            float angle = intake.SpiralAngle;
            PlayModeHelpers.Step(boot, 160);
            Assert.That(intake.IntakeActive, Is.True);
            Assert.That(intake.SpiralFoamAlpha, Is.GreaterThan(RemolinoOverlay.FoamAlpha + 0.2f), "la espiral de la base se ilumina");
            Assert.That(intake.SpiralScale, Is.GreaterThan(1.03f), "y late");
            Assert.That(intake.SpiralAngle, Is.Not.EqualTo(angle), "y gira deprisa");
            Assert.That(intake.IntakeSwirlAlpha, Is.GreaterThan(0.5f));
            Assert.That(intake.IsAnimating, Is.False, "acompaña al vertido: no es un paso");
            Assert.That(animator.ShownOf(touched), Is.EqualTo(before[touched]), "la cuia tocada no cambia");

            // La pareja recibe por el fondo: se ilumina y late; la cuia tocada ya se apagó.
            MechanicTestKit.StepUntilRunning(boot, MechanicStepKind.Receive, partner, 4000);
            MechanicTestKit.StepToFraction(boot, MechanicStepKind.Receive, partner, 0.6f);
            Assert.That(receive.SpiralFoamAlpha, Is.GreaterThan(RemolinoOverlay.FoamAlpha + 0.2f), "la pareja se ilumina al recibir");
            Assert.That(receive.SpiralScale, Is.GreaterThan(1.03f), "y late");
            Assert.That(intake.IntakeActive || intake.IntakeSwirlVisible, Is.False, "la cuia tocada ya se apagó");
            Assert.That(intake.SpiralFoamAlpha, Is.EqualTo(RemolinoOverlay.FoamAlpha).Within(1e-3f));
            Assert.That(intake.SpiralScale, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(intake.SpiralAngle, Is.EqualTo(0f).Within(0.01f));

            MechanicTestKit.StepToIdle(boot, 6000);
            float elapsed = animator.Clock - tapAt;
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta (2.800 ms)");
            MechanicStepRecord record = MechanicTestKit.Record(animator, MechanicStepKind.Receive, partner);
            Assert.That(record.EndMs - record.StartMs, Is.EqualTo(RemolinoOverlay.ReceiveMs * record.SpeedFactor).Within(1f), "la recepción dura lo mismo");
            MechanicTestKit.AssertLogFollowsPlan(animator, in visual);
            MechanicTestKit.AssertViewMatchesModel(boot);
            Assert.That(receive.SpiralFoamAlpha, Is.EqualTo(RemolinoOverlay.FoamAlpha).Within(1e-3f), "en reposo, la espuma de siempre");
            Assert.That(receive.SpiralScale, Is.EqualTo(1f).Within(1e-4f));
            TestContext.WriteLine("Nivel 103: " + elapsed + " ms del toque a la vista quieta.");
        }

        [UnityTest]
        public IEnumerator Remolino140_FromTheTapToIdle_WithinTheBudget()
        {
            yield return MechanicTestKit.LoadPlaying(140);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Pour[]? path = MechanicTestKit.FindTrigger(boot.Flow!, (ev, b, a) => ev.ThroughRemolino);
            Assert.That(path, Is.Not.Null);
            MechanicTestKit.PlayPrefix(boot, path!);
            Pour pour = path![path.Length - 1];
            float tapAt = view.Animator.Clock;
            PourVisual visual = MechanicTestKit.TapPour(boot, pour, out _);
            bool swallowed = false;
            for (int t = 0; t < 6000 && !view.IsViewIdle; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                swallowed |= Whirl(view.Views[pour.Target]).IntakeActive;
            }

            float elapsed = view.Animator.Clock - tapAt;
            Assert.That(swallowed, Is.True, "la cuia tocada se tragó el chorro");
            Assert.That(elapsed, Is.LessThanOrEqualTo(MechanicTestKit.BudgetMs), "del toque a la vista quieta (2.800 ms)");
            MechanicTestKit.AssertLogFollowsPlan(view.Animator, in visual);
            MechanicTestKit.AssertViewMatchesModel(boot);
            TestContext.WriteLine("Nivel 140: " + elapsed + " ms del toque a la vista quieta.");
        }

        [UnityTest]
        public IEnumerator Remolino_SparklesAtTheBottom_AndReduceMotion_WithoutSpinsNorParticles()
        {
            yield return null;
            BoardState board = PairBoard();
            for (int pass = 0; pass < 2; pass++)
            {
                bool reduce = pass == 1;
                _rig?.Dispose();
                _rig = new MechanicTestKit.Rig(Art, board);
                var fx = new CountingFx { ReduceMotion = reduce };
                _rig.Animator.Fx = fx;
                _rig.Animator.ReduceMotion = reduce;
                RemolinoOverlay intake = Whirl(_rig.Views[0]);
                BoardState after = _rig.Pour(board, 1, 0, out PourVisual v);
                Assert.That(v.Receiver, Is.EqualTo(2));
                bool lit = false;
                bool spun = false;
                bool pulsed = false;
                bool swirl = false;
                for (int t = 0; t < 4000 && !_rig.Animator.IsIdle; t += PlayModeHelpers.FrameMs)
                {
                    _rig.Tick(PlayModeHelpers.FrameMs);
                    lit |= intake.SpiralFoamAlpha > RemolinoOverlay.FoamAlpha + 0.1f;
                    spun |= Mathf.Abs(intake.SpiralAngle) > 0.01f;
                    pulsed |= intake.SpiralScale > 1.001f;
                    swirl |= intake.IntakeSwirlVisible && intake.IntakeSwirlAlpha > 0.3f;
                }

                string what = reduce ? "con Reducir movimiento" : "normal";
                Assert.That(_rig.Animator.IsIdle, Is.True, what);
                Assert.That(_rig.Animator.ShownOf(2), Is.EqualTo(after[2]), what);
                Assert.That(lit && swirl, Is.True, what + ": la espuma se ilumina y el remolino de la superficie se funde");
                Assert.That(spun, Is.EqualTo(!reduce), what + ": la espiral gira solo sin Reducir movimiento");
                Assert.That(pulsed, Is.EqualTo(!reduce), what + ": y late");
                Assert.That(intake.IntakeActive || intake.IntakeSwirlVisible, Is.False, what + ": al terminar, en reposo");
                if (reduce)
                {
                    Assert.That(fx.Sparkles + fx.Bubbles, Is.EqualTo(0), "con Reducir movimiento, sin partículas");
                }
                else
                {
                    Assert.That(fx.Sparkles, Is.EqualTo(RemolinoOverlay.SparklesBottom), "destellos en el fondo de la pareja");
                    CuiaView rv = _rig.Views[2];
                    Vector2 bottom = rv.FootPosition + new Vector2(0f, CuiaGeometry.CavityBottomU * rv.Unit);
                    Assert.That(Vector2.Distance(fx.FirstSparkleAt, bottom), Is.LessThan(0.5f * rv.Unit), "en el fondo de la cavidad");
                    Assert.That(fx.Sparkles + fx.Bubbles, Is.LessThanOrEqualTo(FxLayer.MaxParticles), "dentro del tope");
                    Assert.That(fx.Splashes, Is.EqualTo(0), "el chorro no salpica en la cuia tocada: se lo traga");
                }
            }
        }

        [UnityTest]
        public IEnumerator Remolino_NoAllocationsPerFrame_WhileSwallowingAndReceiving()
        {
            yield return null;
            BoardState board = PairBoard();

            // Calentamiento en un banco (JIT y sprites) y medida en otro nuevo, como RemolinoBarroTests.NoAllocDuringSteps.
            for (int pass = 0; pass < 2; pass++)
            {
                _rig?.Dispose();
                _rig = new MechanicTestKit.Rig(Art, board);
                MechanicTestKit.Rig rig = _rig;
                rig.Pour(board, 1, 0, out _);
                if (pass == 0)
                {
                    rig.RunUntilIdle(4000);
                    continue;
                }

                Assert.That(() => { rig.Run(60 * PlayModeHelpers.FrameMs); }, Is.Not.AllocatingGCMemory(), "60 fotogramas desde el toque (el chorro y la recepción)");
                Assert.That(() => { rig.RunUntilIdle(4000); }, Is.Not.AllocatingGCMemory(), "hasta la vista quieta");
                Assert.That(rig.Animator.IsIdle, Is.True);
            }
        }

        // ---- Ayudas ----

        // Nivel level (con LaunchOptions.LevelOffsetOverride = offset si no es 0) con seeds semillas, sin video ni Luciérnagas,
        // en juego con la vista quieta.
        private static IEnumerator LoadWithBubbles(int level, int offset, int seeds)
        {
            LaunchOptions.Reset();
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            if (offset != 0)
            {
                LaunchOptions.LevelOffsetOverride = offset;
            }

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

        private static RectTransform Root(GameScreen view)
        {
            return (RectTransform)view.Canvas.transform;
        }

        // Lo más bajo del tablero (dp desde arriba): la sombra de la repisa de la última fila.
        private static float BoardFloor(GameScreen view)
        {
            BoardLayoutResult l = view.Layout!;
            return l.RowFootY[l.Rows - 1] + GameScreen.ShelfShadowBelowFootDp;
        }

        // El aviso, con su canto de abajo, no se solapa con ninguna burbuja «Te faltan {n}» a la vista, ni va detrás de ellas.
        private static void AssertToastClearOfBubbles(GameScreen view, string what)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform root = Root(view);
            Rect toast = VictorySetup.CanvasRect(view.NoticeToast.Root, root);
            toast.yMax += Toast.EdgeDp;
            BoosterTray tray = view.Tray;
            int bubbles = 0;
            foreach (RectTransform bubble in new[] { tray.ExtraMissingRect, tray.RevealMissingRect })
            {
                if (!bubble.gameObject.activeInHierarchy)
                {
                    continue;
                }

                bubbles++;
                Rect r = VictorySetup.CanvasRect(bubble, root);
                Assert.That(toast.Overlaps(r), Is.False, what + ": el aviso " + toast + " no tapa la burbuja " + r);
                Assert.That(toast.yMax, Is.LessThanOrEqualTo(r.yMin - GameScreen.NoticeBubbleGapDp + 0.05f), what + ": queda por encima, con aire");
            }

            Assert.That(bubbles, Is.GreaterThan(0), what + ": alguna burbuja a la vista");
            Assert.That(tray.TryGetMissingTop(out float top), Is.True);
            Assert.That(view.NoticeToast.Top + view.NoticeToast.Height + Toast.EdgeDp, Is.LessThanOrEqualTo(top - GameScreen.NoticeBubbleGapDp + 0.05f));
        }

        // El texto del aviso cabe entero, a su tamaño de siempre (15 sp), sin desbordes.
        private static void AssertToastTextFits(GameScreen view)
        {
            Canvas.ForceUpdateCanvases();
            TextMeshProUGUI t = view.NoticeToast.Label;
            t.ForceMeshUpdate(true, true);
            Rect r = t.rectTransform.rect;
            string what = "«" + t.text + "»";
            Assert.That(t.fontSize, Is.EqualTo(15f).Within(0.01f), what + " a 15 sp: no se encoge");
            Assert.That(t.textInfo.characterCount, Is.EqualTo(t.GetParsedText().Length), what + " entero");
            Assert.That(t.preferredHeight, Is.LessThanOrEqualTo(r.height + 0.5f), what + " cabe en alto");
            Bounds b = t.textBounds;
            Assert.That(b.min.y, Is.GreaterThanOrEqualTo(r.yMin - 0.5f), what + " por abajo");
            Assert.That(b.max.y, Is.LessThanOrEqualTo(r.yMax + 0.5f), what + " por arriba");
        }

        // El banner a la escala scale: dentro de la pantalla y de su marco, la cabecera sobre la viñeta y la viñeta sobre el lugar,
        // Capi y Rosa en la viñeta (no tocan los textos), y los textos a su tamaño por la escala, enteros y sin desbordes (las
        // comprobaciones de la tarjeta previa: CardSetup.AssertNoOverflow, AssertInsidePanel y AssertPanelOnScreen).
        private static void AssertBannerFits(GameScreen view, float scale, string where)
        {
            Canvas.ForceUpdateCanvases();
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(banner.FontScale, Is.EqualTo(scale).Within(1e-4f), where);
            RectTransform root = Root(view);
            Rect panel = VictorySetup.CanvasRect(banner.Panel, root);
            Assert.That(panel.xMin, Is.GreaterThanOrEqualTo(LevelCardView.SideMarginDp - 0.5f), where);
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(view.CanvasWidth - LevelCardView.SideMarginDp + 0.5f), where);
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp), where + ": bajo la zona segura de arriba");
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(view.CardCanvasHeight - view.CardSafeBottom), where + ": sobre la zona segura de abajo");
            Rect title = VictorySetup.CanvasRect(banner.TitleLabel.rectTransform, root);
            Rect scene = VictorySetup.CanvasRect(banner.Scene, root);
            Rect place = VictorySetup.CanvasRect(banner.PlaceLabel.rectTransform, root);
            foreach ((string name, Rect r) in new[] { ("cabecera", title), ("viñeta", scene), ("lugar", place) })
            {
                Assert.That(r.xMin, Is.GreaterThanOrEqualTo(panel.xMin + 7.5f), where + ": " + name + " dentro del marco");
                Assert.That(r.xMax, Is.LessThanOrEqualTo(panel.xMax - 7.5f), where + ": " + name + " dentro del marco");
                Assert.That(r.yMin, Is.GreaterThanOrEqualTo(panel.yMin + 7.5f), where + ": " + name + " dentro del marco");
                Assert.That(r.yMax, Is.LessThanOrEqualTo(panel.yMax - 7.5f), where + ": " + name + " dentro del marco");
            }

            Assert.That(title.yMax, Is.LessThanOrEqualTo(scene.yMin + 0.5f), where + ": la cabecera, encima de la viñeta");
            Assert.That(scene.yMax, Is.LessThanOrEqualTo(place.yMin + 0.5f), where + ": la viñeta, encima del lugar");
            Rect capi = VictorySetup.CanvasRect(banner.CapiRect, root);
            Assert.That(capi.xMin >= scene.xMin - 0.5f && capi.xMax <= scene.xMax + 0.5f && capi.yMin >= scene.yMin - 0.5f && capi.yMax <= scene.yMax + 0.5f, Is.True, where + ": Capi, en la viñeta");
            Rect rosa = VictorySetup.CanvasRect(banner.RosaClip, root);
            Assert.That(rosa.xMin >= scene.xMin - 0.5f && rosa.xMax <= scene.xMax + 0.5f && rosa.yMin >= scene.yMin - 0.5f && rosa.yMax <= scene.yMax + 0.5f, Is.True, where + ": Rosa, en la viñeta");
            Assert.That(capi.Overlaps(title) || capi.Overlaps(place) || rosa.Overlaps(title) || rosa.Overlaps(place), Is.False, where + ": Capi y Rosa no tocan los textos");

            float titleSp = WorldBannerView.TitleSp * scale;
            Assert.That(banner.TitleLabel.fontSize, Is.EqualTo(titleSp).Within(0.01f), where + ": la cabecera crece, no se encoge");
            Assert.That(banner.PlaceLabel.fontSize, Is.InRange((WorldBannerView.PlaceMinSp * scale) - 0.01f, (WorldBannerView.PlaceSp * scale) + 0.01f), where + ": el lugar, de 18 a 15 sp por la escala");
            foreach (TextMeshProUGUI t in new[] { banner.TitleLabel, banner.PlaceLabel })
            {
                t.ForceMeshUpdate(true, true);
                Rect r = t.rectTransform.rect;
                string what = where + ": «" + t.text + "»";
                Assert.That(t.fontSize, Is.GreaterThanOrEqualTo(12f), what + " ≥ 12 sp");
                Assert.That(t.overflowMode, Is.EqualTo(TextOverflowModes.Overflow), what + " nunca se trunca");
                Assert.That(t.textInfo.characterCount, Is.EqualTo(t.GetParsedText().Length), what + " entero");
                Assert.That(t.preferredHeight, Is.LessThanOrEqualTo(r.height + 0.5f), what + " cabe en alto");
                Bounds b = t.textBounds;
                Assert.That(b.min.x, Is.GreaterThanOrEqualTo(r.xMin - 0.5f), what + " por la izquierda");
                Assert.That(b.max.x, Is.LessThanOrEqualTo(r.xMax + 0.5f), what + " por la derecha");
                Assert.That(b.min.y, Is.GreaterThanOrEqualTo(r.yMin - 0.5f), what + " por abajo");
                Assert.That(b.max.y, Is.LessThanOrEqualTo(r.yMax + 0.5f), what + " por arriba");
            }

            Assert.That(banner.Text, Is.EqualTo(UiText.WorldBanner(banner.World)), where + ": la clave entera");
        }

        // Banco de 4 cuias: la 0 y la 2, pareja de Remolino; verter la 1 en la 0 entra por el fondo de la 2.
        private static BoardState PairBoard()
        {
            int[][] cuias = { new[] { 1, 1 }, new[] { 3, 3, 3 }, new[] { 2 }, new int[0] };
            MechanicSpec[] attrs =
            {
                new MechanicSpec(MechanicType.Remolino, partner: 2), MechanicSpec.None, new MechanicSpec(MechanicType.Remolino, partner: 0),
                MechanicSpec.None,
            };
            BoardBuildResult result = BoardBuilder.Build(new[] { 4, 4, 4, 4 }, cuias, attrs);
            Assert.That(result.Error, Is.Null);
            return result.State!;
        }

        private static RemolinoOverlay Whirl(CuiaView view)
        {
            CuiaOverlay? overlay = view.OverlayOf(MechanicType.Remolino);
            Assert.That(overlay, Is.InstanceOf<RemolinoOverlay>(), "la cuia " + view.name + " tiene Remolino");
            return (RemolinoOverlay)overlay!;
        }

        // Efectos de prueba: cuenta lo que se pide (con Reducir movimiento, nada, como FxLayer).
        private sealed class CountingFx : IBoardFx
        {
            public int Sparkles;
            public int Bubbles;
            public int Splashes;
            public Vector2 FirstSparkleAt;

            public bool ReduceMotion { get; set; }

            public void Splash(Vector2 surface, Color tint, float unitDp)
            {
                if (!ReduceMotion)
                {
                    Splashes++;
                }
            }

            public void SealDrops(Vector2 rim, Color tint, float unitDp)
            {
            }

            void IBoardFx.Sparkles(Vector2 pos, Color color, int count, float unitDp)
            {
                if (ReduceMotion)
                {
                    return;
                }

                if (Sparkles == 0)
                {
                    FirstSparkleAt = pos;
                }

                Sparkles += count;
            }

            public void Shards(Vector2 pos, Color color, int count, float unitDp)
            {
            }

            void IBoardFx.Bubbles(Vector2 pos, int count, float unitDp)
            {
                if (!ReduceMotion)
                {
                    Bubbles += count;
                }
            }
        }
    }
}
