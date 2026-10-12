#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Save;
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
    /// el aviso no captura toques; la cola se vacía al cargar otro nivel y al empezar otro intento; y sin asignaciones por
    /// fotograma con un aviso a la vista y otro esperando.
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
    }
}
