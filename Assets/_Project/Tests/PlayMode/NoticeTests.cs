#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Avisos legibles (D-011, punto 2; prueba en el teléfono del 2026-10-11, §P2 y §P3) en la escena real con el flujo: el
    /// regalo de Capi del 8 no sale durante la intro, sale al habilitarse el toque y sigue a la vista a los 5 s (dura
    /// <see cref="NoticeTiming.ReadMs(string)"/>: 5.455 ms); dos avisos seguidos, o un aviso y la tarjeta «¡Miquito a la
    /// vista!», no se pisan y el segundo sale en el fotograma en que se oculta el primero; la tarjeta espera a que la tropa se
    /// vaya, con las etiquetas del HUD a la vista mientras tanto; el cartel «El río bajó» sigue con sus 2,0 s, fuera de la cola;
    /// el aviso no captura toques; el aviso y la tarjeta van delante del tablero y del HUD, pero detrás del origen que vierte
    /// (D-006), de los modales y de la pausa; con el bloqueo pendiente (antes de que se abra Aguas estancadas) no empieza
    /// ninguno, y el que esperaba sale al volver a jugar; la cola se vacía al cargar otro nivel y al empezar otro intento; tras
    /// una victoria rápida en el 8 o en el 5, ni el aviso o la tarjeta que se veían ni lo que esperaba salen encima de la
    /// celebración ni de la pantalla de victoria (solo hay avisos en PLAYING sin victoria ni bloqueo pendientes); y sin
    /// asignaciones por fotograma con un aviso a la vista y otro esperando.
    /// </summary>
    public sealed class NoticeTests
    {
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
        public IEnumerator Level8_CapisGift_NotDuringTheIntro_ShowsWhenTouchIsEnabled_AndIsStillThereAt5s()
        {
            yield return LoadLevel8WithTheGift();
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(view.ToastText, Is.Null, "el regalo llega al cargar, pero no sale encima de la intro");
            Assert.That(view.NoticesQueued, Is.EqualTo(1));

            // La intro entera, sin saltarla: ningún aviso hasta que se habilita el toque.
            int t = 0;
            while (flow.Phase == FlowPhase.Intro && t < 5000)
            {
                Assert.That(view.ToastText, Is.Null, "durante la intro (t = " + t + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift), "sale en el fotograma en que se habilita el toque");
            Assert.That(view.NoticeToast.DurationMs, Is.EqualTo(5455f), "1.000 + 55 × 81 caracteres");
            Assert.That(view.NoticesQueued, Is.EqualTo(0));

            PlayModeHelpers.TickExact(boot, 5000);
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift), "sigue a la vista a los 5 s (antes se iba a los 2,6)");
            PlayModeHelpers.TickExact(boot, 454);
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift));
            PlayModeHelpers.TickExact(boot, 1);
            Assert.That(view.ToastText, Is.Null, "y se va a los 5.455 ms");
        }

        [UnityTest]
        public IEnumerator TwoNoticesInARow_DoNotOverlap_TheSecondComesWhenTheFirstHides()
        {
            yield return LoadLevel8WithTheGift();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift));
            PlayModeHelpers.TickExact(boot, 1000);

            string missing = UiText.RevealMissing(70);
            view.ShowRevealMissing(70);
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift), "el segundo no pisa al primero");
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "espera su turno");
            view.ShowRevealMissing(70);
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "el mismo aviso no se repite en la cola");

            int waited = 0;
            while (view.ToastText == UiText.ExtraCuiaGift && waited < 8000)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                waited += PlayModeHelpers.FrameMs;
            }

            Assert.That(waited + 1000, Is.InRange(5455, 5455 + PlayModeHelpers.FrameMs), "el primero dura lo suyo entero");
            Assert.That(view.ToastText, Is.EqualTo(missing), "el segundo sale en el mismo fotograma, sin hueco");
            Assert.That(view.NoticesQueued, Is.EqualTo(0));
            Assert.That(view.NoticeToast.DurationMs, Is.EqualTo((float)NoticeTiming.ReadMs(missing)));
            Assert.That(view.NoticeToast.DurationMs, Is.EqualTo(3000f), "los cortos duran el mínimo");

            // El mismo texto que ya se ve vuelve a empezar (Revelar tocado otra vez sin saldo), sin ponerse a la cola.
            PlayModeHelpers.TickExact(boot, 2000);
            view.ShowRevealMissing(70);
            Assert.That(view.NoticesQueued, Is.EqualTo(0));
            Assert.That(view.NoticeToast.LeftMs, Is.EqualTo(3000f));
            PlayModeHelpers.TickExact(boot, 3000);
            Assert.That(view.ToastText, Is.Null);
        }

        [UnityTest]
        public IEnumerator SightingCard_WaitsForTheTroopToLeave_HudLabelsStayMeanwhile_AndANoticeWaitsForTheCard()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            yield return PlayModeHelpers.LoadGame(5, new MemoryProgressStore(5, 4));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            SightingCard card = view.Sighting;
            flow.Load(5); // desde t = 0, sin saltar la intro
            int total = flow.IntroDurationMs;
            PlayModeHelpers.TickExact(boot, total);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing), "toque habilitado en T");
            Assert.That(view.Intro.TroopVisible, Is.True, "la tropa reposa en la rama, ya en juego (D-011, punto 3)");
            Assert.That(card.Visible, Is.False, "la tarjeta no sale encima de la tropa");
            Assert.That(view.SightingPending, Is.True);

            // Mientras espera, las etiquetas del HUD siguen a la vista.
            PlayModeHelpers.TickExact(boot, 600);
            Assert.That(view.Intro.TroopVisible, Is.True);
            Assert.That(card.Visible, Is.False);
            Assert.That(view.HudLabelsAlpha, Is.EqualTo(1f), "las etiquetas del HUD, a la vista mientras la tarjeta espera");

            // Sale en el fotograma en que la tropa se va; entonces las etiquetas se apartan, como siempre.
            int left = Mathf.CeilToInt(view.Intro.TroopGoneMs - view.Intro.TimeMs);
            Assert.That(left, Is.GreaterThan(0));
            PlayModeHelpers.TickExact(boot, left - 1);
            Assert.That(view.Intro.TroopVisible, Is.True);
            Assert.That(card.Visible, Is.False);
            PlayModeHelpers.TickExact(boot, 2);
            Assert.That(view.Intro.TroopVisible, Is.False, "la tropa se ha ido");
            Assert.That(card.Visible, Is.True, "y la tarjeta sale ya");
            Assert.That(view.SightingPending, Is.False);
            Assert.That(view.HudLabelsAlpha, Is.EqualTo(0f), "con la tarjeta a la vista, las etiquetas se apartan");
            Assert.That(card.DurationMs, Is.EqualTo((float)NoticeTiming.ReadMs(UiText.FaunaSightedCard, card.Name, card.Fact)));

            // Un aviso que llega con la tarjeta a la vista espera a que se vaya; nunca se ven los dos.
            PlayModeHelpers.TickExact(boot, 1000);
            string missing = UiText.RevealMissing(70);
            view.ShowRevealMissing(70);
            Assert.That(view.ToastText, Is.Null, "no pisa la tarjeta");
            Assert.That(view.NoticesQueued, Is.EqualTo(1));
            int waited = 1000;
            while (card.Visible && waited < 8000)
            {
                Assert.That(view.ToastText, Is.Null, "con la tarjeta a la vista (t = " + waited + " ms)");
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                waited += PlayModeHelpers.FrameMs;
            }

            Assert.That(card.Visible, Is.False);
            Assert.That(waited, Is.InRange(Mathf.RoundToInt(card.DurationMs), Mathf.RoundToInt(card.DurationMs) + PlayModeHelpers.FrameMs));
            Assert.That(view.ToastText, Is.EqualTo(missing), "el aviso sale en el fotograma en que se va la tarjeta");
        }

        [UnityTest]
        public IEnumerator RiverLoweredBanner_Keeps2s_OutsideTheQueue()
        {
            yield return LoadLevel8WithTheGift();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "el regalo de Capi espera a la intro");

            // La variante aliviada (02 §2 INTRO; 04 §2.7): el cartel va antes del teñido corto. Solo avanza la vista: el flujo
            // sigue en INTRO, así que la cola no puede soltar nada.
            view.OnRiverLowered("plus_empty", 2000);
            view.OnIntroStarted(2000 + GameConfig.AnimTintDurationMsLate, true);
            Assert.That(view.BannerText, Is.EqualTo(UiText.RiverLoweredCuia), "sale en el acto, sin pasar por la cola");
            Assert.That(NoticeTiming.ReadMs(UiText.RiverLoweredCuia), Is.Not.EqualTo(2000), "su duración no es la de lectura");
            for (int t = 0; t < 1990; t += 10)
            {
                view.Tick(10);
                Assert.That(view.ToastText, Is.Null, "nada encima del cartel (t = " + (t + 10) + " ms)");
            }

            Assert.That(view.BannerText, Is.EqualTo(UiText.RiverLoweredCuia), "a 1.990 ms sigue");
            view.Tick(10);
            view.Tick(PlayModeHelpers.FrameMs);
            Assert.That(view.BannerText, Is.Null, "a los 2.000 ms + 1 fotograma, ya no (anim_river_lowered_banner_ms)");
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "el regalo sigue esperando");

            // Cuando se habilita el toque, sale el regalo.
            for (int t = 0; t < 5000 && boot.Flow.Phase == FlowPhase.Intro; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift));
        }

        [UnityTest]
        public IEnumerator Toast_DoesNotCaptureTouches()
        {
            yield return LoadLevel8WithTheGift();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 300);
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift));
            Toast toast = view.NoticeToast;
            Assert.That(toast.BlocksRaycasts, Is.False, "el CanvasGroup del aviso deja pasar los toques");
            var graphics = new List<Graphic>();
            toast.CollectGraphics(graphics);
            Assert.That(graphics.Count, Is.EqualTo(3));
            foreach (Graphic g in graphics)
            {
                Assert.That(g.raycastTarget, Is.False, g.name);
            }

            int source = -1;
            for (int i = 0; i < boot.Flow!.Session.Current.Count && source < 0; i++)
            {
                source = boot.Flow.Session.Current[i].IsEmpty ? -1 : i;
            }

            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected), "se juega con el aviso a la vista");
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift), "y el aviso sigue");
        }

        [UnityTest]
        public IEnumerator Notices_InFrontOfTheBoardAndTheHud_BehindThePouringSourceAndTheModals()
        {
            // Revisión de A2: el aviso (hasta 7 s) y la tarjeta (~5 s, ya en juego) van en su capa, delante del tablero, del HUD y
            // del polen, y detrás de la capa de vertido (D-006: el origen que vierte, delante de todo), de los modales y de la
            // pausa. Mostrarlos no los saca de su capa.
            yield return LoadLevel8WithTheGift();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 300);
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift));
            RectTransform notices = view.NoticeLayer;
            Transform root = view.PourFront.parent;
            Assert.That(notices.parent, Is.SameAs(root), "la capa de avisos es hermana de la de vertido");
            Assert.That(notices.GetComponent<Graphic>(), Is.Null, "la capa no tiene Graphic");
            var graphics = new List<Graphic>();
            view.NoticeToast.CollectGraphics(graphics);
            view.Sighting.CollectGraphics(graphics);
            foreach (Graphic g in graphics)
            {
                Assert.That(g.transform.IsChildOf(notices), Is.True, g.name + ": en la capa de avisos");
            }

            AssertNoticeLayerOrder(view, "con el aviso a la vista");

            // Con el aviso a la vista, un vertido: el origen pasa a la capa de vertido, delante del aviso, y nada se reordena.
            BoardState board = boot.Flow!.Session.Current;
            int source = -1;
            int target = -1;
            for (int a = 0; a < board.Count && source < 0; a++)
            {
                for (int b = 0; b < board.Count && source < 0; b++)
                {
                    if (a != b && PourRules.IsLegal(board, a, b))
                    {
                        source = a;
                        target = b;
                    }
                }
            }

            Assert.That(source, Is.GreaterThanOrEqualTo(0), "algún vertido legal");
            Assert.That(boot.TapCuia(source).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(target).Kind, Is.EqualTo(TapKind.Poured));
            RectTransform body = view.Views[source].Body;
            for (int t = 0; t < 1000 && body.parent != view.PourFront; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(body.parent, Is.SameAs(view.PourFront), "el origen, en la capa de vertido");
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift), "con el aviso a la vista");
            AssertNoticeLayerOrder(view, "durante el vertido");
            PlayModeHelpers.StepUntilIdle(boot, 3000);
        }

        [UnityTest]
        public IEnumerator Level31_WithTheStuckPending_NoNoticeStarts_TheWaitingOneComesWhenPlayResumes()
        {
            // Revisión de A2: entre el bloqueo lógico y Aguas estancadas, la vista termina el último vertido y espera
            // StuckModalDelayMs, aún en PLAYING. En esa ventana no empieza ningún aviso: el que esperaba se leería casi entero
            // detrás del modal. Espera a que se vuelva a jugar (aquí, el rescate).
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
            Assert.That(view.ToastText, Is.Null);

            // Un aviso a la vista al que le quedan 200 ms y otro que espera su turno.
            string first = UiText.RevealMissing(70);
            string waiting = UiText.RevealMissing(50);
            view.ShowRevealMissing(70);
            view.ShowRevealMissing(50);
            Assert.That(view.ToastText, Is.EqualTo(first));
            Assert.That(view.NoticesQueued, Is.EqualTo(1));
            PlayModeHelpers.TickExact(boot, NoticeTiming.ReadMs(first) - 200);
            Assert.That(view.ToastText, Is.EqualTo(first));

            // 3 → 4 bloquea el tablero 6 del tutorial (FunnelSetup.GetStuck): bloqueo pendiente, aún en PLAYING.
            Assert.That(boot.TapCuia(3).Kind, Is.EqualTo(TapKind.Selected));
            Assert.That(boot.TapCuia(4).Kind, Is.EqualTo(TapKind.Poured));
            Assert.That(flow.IsResolving, Is.True, "bloqueo pendiente");
            Assert.That(flow.IsResolvingWin, Is.False);
            bool firstGone = false;
            int t = 0;
            while (t < 5000)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
                if (flow.Phase != FlowPhase.Playing)
                {
                    break;
                }

                Assert.That(flow.IsResolving, Is.True, "t = " + t + " ms");
                Assert.That(view.ToastText, Is.Null.Or.EqualTo(first), "con el bloqueo pendiente no empieza ninguno (t = " + t + " ms)");
                firstGone |= view.ToastText == null;
            }

            Assert.That(firstGone, Is.True, "el primero se acaba con el bloqueo pendiente");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(view.ToastText, Is.Null, "ninguno detrás de Aguas estancadas");
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "el que esperaba sigue en la cola");
            PlayModeHelpers.Step(boot, 300);
            Assert.That(view.ToastText, Is.Null);
            Assert.That(view.NoticesQueued, Is.EqualTo(1));

            // Con el rescate se vuelve a jugar: sale el que esperaba y «¡Una cuia más!» detrás (uno cada vez, por orden de
            // llegada). Sin nada en la cola, «¡Una cuia más!» sale en el acto (StuckFunnelTests).
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(flow.IsResolving, Is.False);
            Assert.That(view.ToastText, Is.EqualTo(waiting), "el que esperaba sale al volver a jugar");
            Assert.That(view.NoticesQueued, Is.EqualTo(1));
            int waited = 0;
            while (view.ToastText == waiting && waited < 8000)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                waited += PlayModeHelpers.FrameMs;
            }

            Assert.That(view.ToastText, Is.EqualTo(UiText.RescueApplied), "y después «¡Una cuia más!»");
            Assert.That(view.NoticesQueued, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Notices_AreCleared_OnRestart_AndWhenAnotherLevelLoads()
        {
            yield return LoadLevel8WithTheGift();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            boot.TapAnywhere();
            view.ShowRevealMissing(70);
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift));
            Assert.That(view.NoticesQueued, Is.EqualTo(1));

            PlayModeHelpers.Step(boot, 100);
            Assert.That(boot.PressRestart(), Is.True, "Volver a la orilla sin vertidos");
            Assert.That(view.ToastText, Is.Null, "los avisos del intento anterior se van");
            Assert.That(view.NoticesQueued, Is.EqualTo(0));
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.ToastText, Is.Null, "y no vuelven tras la intro");

            view.ShowRevealMissing(70);
            view.ShowRevealMissing(50);
            Assert.That(view.ToastText, Is.EqualTo(UiText.RevealMissing(70)));
            Assert.That(view.NoticesQueued, Is.EqualTo(1));
            boot.Flow!.Load(9);
            Assert.That(view.ToastText, Is.Null, "ningún aviso pasa al nivel siguiente");
            Assert.That(view.NoticesQueued, Is.EqualTo(0));
            boot.TapAnywhere();
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.ToastText, Is.Null);
        }

        [UnityTest]
        public IEnumerator Level8_QuickWin_NeitherCapisGiftNorTheQueuedNotice_OverTheCelebrationOrTheVictory()
        {
            yield return LoadLevel8WithTheGift();
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift));
            view.ShowRevealMissing(70);
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "«Te faltan» espera detrás del regalo");

            // Una victoria rápida: el regalo dura 5.455 ms y la vista termina los vertidos en ≤ 3 s (MaxViewDrainWaitMs).
            TapTheSolution(boot);
            Assert.That(flow.IsResolvingWin, Is.True);
            StepToVictoryWithoutNotices(boot, UiText.ExtraCuiaGift, false);
        }

        [UnityTest]
        public IEnumerator Level5_QuickWin_TheSightingCardAndTheQueuedNotice_GoWithTheCelebration()
        {
            yield return LoadLevel5FromTheStart();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro: la tropa se funde en 100 ms");
            for (int t = 0; t < 1000 && !view.Sighting.Visible; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(view.Sighting.Visible, Is.True, "la tarjeta, a la vista");
            view.ShowRevealMissing(70);
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "«Te faltan» espera a que se vaya la tarjeta");

            TapTheSolution(boot);
            Assert.That(boot.Flow!.IsResolvingWin, Is.True);
            StepToVictoryWithoutNotices(boot, null, true);
        }

        [UnityTest]
        public IEnumerator Level5_WonBeforeTheTroopLeaves_TheCardStaysPending_AndNeverShows()
        {
            yield return LoadLevel5FromTheStart();
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            PlayModeHelpers.TickExact(boot, flow.IntroDurationMs);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.SightingPending, Is.True, "la tarjeta espera a que se vaya la tropa");

            // El último fotograma con la tropa en la rama: se gana ahí, y la tropa se va mientras se anima el último vertido.
            int left = Mathf.CeilToInt(view.Intro.TroopGoneMs - view.Intro.TimeMs);
            Assert.That(left, Is.GreaterThan(PlayModeHelpers.FrameMs));
            PlayModeHelpers.TickExact(boot, left - 1);
            Assert.That(view.Intro.TroopVisible, Is.True);
            TapTheSolution(boot);
            Assert.That(flow.IsResolvingWin, Is.True);
            PlayModeHelpers.TickExact(boot, 2);
            Assert.That(view.Intro.TroopVisible, Is.False, "la tropa se ha ido");
            Assert.That(flow.IsResolvingWin, Is.True, "con la victoria aún pendiente");
            Assert.That(view.Sighting.Visible, Is.False, "con la victoria pendiente, la tarjeta no sale");
            Assert.That(view.SightingPending, Is.True, "y tampoco entra en la cola");
            Assert.That(view.NoticesQueued, Is.EqualTo(0));
            StepToVictoryWithoutNotices(boot, null, false);
        }

        [UnityTest]
        public IEnumerator NoAllocationsPerFrame_WithANoticeShownAndAnotherWaiting()
        {
            yield return LoadLevel8WithTheGift();
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            boot.TapAnywhere();
            view.ShowRevealMissing(70);
            // Ya en régimen: los ~2,2 s que siguen al salto de la intro asignan memoria también sin ningún aviso (medido en el 9;
            // ya estaba antes de la cola).
            PlayModeHelpers.TickExact(boot, 2600);
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift));
            Assert.That(view.NoticesQueued, Is.EqualTo(1));
            Assert.That(() =>
            {
                for (int f = 0; f < 60; f++)
                {
                    view.Tick(PlayModeHelpers.FrameMs);
                }
            }, Is.Not.AllocatingGCMemory(), "60 fotogramas con un aviso a la vista y otro en la cola");
            Assert.That(view.ToastText, Is.EqualTo(UiText.ExtraCuiaGift));
            Assert.That(view.NoticesQueued, Is.EqualTo(1));
        }

        // El 8 por primera vez con un perfil nuevo: Capi regala la Cuia extra (04 §2.2) al cargar, en plena LOADING.
        private static IEnumerator LoadLevel8WithTheGift()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(8, new MemoryProgressStore(8, 7));
            PlayModeHelpers.TakeClock(GameBootstrap.Current!);
        }

        // El 5 (nivel_tarjeta del mono ardilla) desde t = 0 de la intro, con la franja del panorama de un teléfono alto.
        private static IEnumerator LoadLevel5FromTheStart()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f);
            yield return PlayModeHelpers.LoadGame(5, new MemoryProgressStore(5, 4));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.Flow!.Load(5);
        }

        // Juega de golpe la solución del solver, sin dejar que la vista avance: el último vertido gana y la victoria queda
        // pendiente mientras la vista anima los vertidos.
        private static void TapTheSolution(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                Assert.That(boot.TapCuia(move.Source).Kind, Is.EqualTo(TapKind.Selected));
                Assert.That(boot.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
        }

        // Del instante de la victoria a VICTORY_SCREEN y 8 s más en ella. Mientras la victoria está pendiente (el último vertido
        // se anima) no empieza ni se acaba nada: sigue lo que se veía (el aviso <toast>, la tarjeta) y lo que esperaba (la
        // cola, la tarjeta pendiente), y así llega al primer fotograma de CELEBRATION. Desde ahí, ni aviso ni tarjeta, y nada en
        // la cola ni pendiente: ni el aviso que se veía ni el que esperaba salen encima de la celebración o de la victoria.
        private static void StepToVictoryWithoutNotices(GameBootstrap boot, string? toast, bool card)
        {
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            int queued = view.NoticesQueued;
            bool pending = view.SightingPending;
            bool celebrated = false;
            int t = 0;
            while (t < 15000 && flow.Phase != FlowPhase.VictoryScreen)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
                string when = FlowPhaseNames.Of(flow.Phase) + " a " + t + " ms de la victoria";
                if (flow.IsResolvingWin)
                {
                    Assert.That(view.ToastText, Is.EqualTo(toast), "el aviso que se veía, " + when);
                    Assert.That(view.Sighting.Visible, Is.EqualTo(card), "la tarjeta, " + when);
                    Assert.That(view.NoticesQueued, Is.EqualTo(queued), "la cola, " + when);
                    Assert.That(view.SightingPending, Is.EqualTo(pending), "la tarjeta pendiente, " + when);
                    continue;
                }

                Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Celebration).Or.EqualTo(FlowPhase.VictoryScreen), when);
                celebrated |= flow.Phase == FlowPhase.Celebration;
                AssertNoNotice(view, when);
            }

            Assert.That(celebrated, Is.True, "hubo celebración");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            for (int v = 0; v < 8000; v += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                AssertNoNotice(view, "VICTORY_SCREEN a " + (v + PlayModeHelpers.FrameMs) + " ms");
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "sin avance por tiempo");
        }

        // La capa de avisos, delante del tablero, del HUD (con la bandeja), de la cuia elevada y del polen; detrás de la capa de
        // vertido, de Aguas estancadas y de la pausa.
        private static void AssertNoticeLayerOrder(GameScreen view, string when)
        {
            Transform root = view.PourFront.parent;
            int index = view.NoticeLayer.GetSiblingIndex();
            Assert.That(index, Is.GreaterThan(view.BoardContainer.GetSiblingIndex()), "delante del tablero, " + when);
            Assert.That(index, Is.GreaterThan(view.Hud.GetSiblingIndex()), "delante del HUD y de la bandeja, " + when);
            Assert.That(index, Is.GreaterThan(view.LiftFront.GetSiblingIndex()), "delante de la cuia elevada, " + when);
            Assert.That(index, Is.GreaterThan(view.AmbientNear.GetSiblingIndex()), "delante del polen, " + when);
            Assert.That(index, Is.LessThan(view.PourFront.GetSiblingIndex()), "detrás de la capa de vertido (D-006), " + when);
            Transform stuck = view.Stuck.Panel;
            while (stuck.parent != root)
            {
                stuck = stuck.parent;
            }

            Assert.That(index, Is.LessThan(stuck.GetSiblingIndex()), "detrás de Aguas estancadas, " + when);
            Assert.That(index, Is.LessThan(view.PauseButton.transform.parent.GetSiblingIndex()), "y de la pausa, " + when);
        }

        private static void AssertNoNotice(GameScreen view, string when)
        {
            Assert.That(view.ToastText, Is.Null, "ningún aviso en " + when);
            Assert.That(view.Sighting.Visible, Is.False, "ni la tarjeta en " + when);
            Assert.That(view.NoticesQueued, Is.EqualTo(0), "ni nada en la cola en " + when);
            Assert.That(view.SightingPending, Is.False, "ni la tarjeta pendiente en " + when);
        }
    }
}
