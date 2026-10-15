#nullable enable
using System.Collections;
using System.Collections.Generic;
using AF.GameFlow;
using AF.Levels;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Los ganchos de los avisos que añadió T52 para la pregunta 42 (b) (decidida por el dueño el 2026-10-13; D-015), con lo
    /// que dejó sin test su revisión (U1, baja 2): el aviso y la tarjeta «¡Miquito a la vista!» a la vista se ocultan, en la
    /// misma llamada, al abrirse cualquier modal del embudo, y no vuelven.
    /// <list type="bullet">
    /// <item><description>Caminos del juego que llegan con un aviso a la vista y que el test de T52 (Aguas estancadas,
    /// <c>PhoneFixesTests</c>) no cubría: Pausa › «Salir» confirmado desde el 31 abre Nivel fallido, y Volver a la orilla con
    /// 0 canoas abre Sin canoas (<c>GameScreen.OnFunnelPhase</c>). El que esperaba en la cola sigue esperando: tras Sin canoas
    /// sale al volver al tablero, y tras Nivel fallido «Reintentar» empieza otro intento, que vacía la cola.</description></item>
    /// <item><description>Ganchos defensivos, a los que hoy no se llega con un aviso a la vista (¿Seguro? solo se abre desde
    /// Aguas estancadas, que ya lo ocultó, y al reanudar <c>OnLevelLoaded</c> ya vació la cola): <c>OnFunnelPhase</c> y
    /// <c>OnResumed</c> llamados fuera de orden, con el flujo en juego y el aviso y la tarjeta forzados a la vista a la vez (en
    /// el juego salen de uno en uno), no lanzan, los ocultan en esa llamada y no vuelven.</description></item>
    /// <item><description>La red de seguridad de <c>TickNotices</c>: un aviso y una tarjeta forzados detrás de Aguas estancadas
    /// abierto se ocultan en el fotograma siguiente.</description></item>
    /// <item><description>Sin flujo (<c>_flow</c> null, una pantalla sin <c>Bind</c>): encolar un aviso, avanzar fotogramas y
    /// pedir el arte de un banner no lanzan; el aviso espera (<c>NoticesHeld</c>) y nada se ve.</description></item>
    /// </list>
    /// </summary>
    public sealed class NoticeHooksDefensiveTests
    {
        /// <summary>Ganchos del embudo que se llaman fuera de orden.</summary>
        public enum Hook
        {
            FunnelGiveUpConfirm,
            FunnelLevelFailed,
            FunnelNoCanoes,
            ResumedStuckModal,
            ResumedGiveUpConfirm,
            ResumedLevelFailed,
        }

        private static readonly Hook[] OutOfOrderHooks =
        {
            Hook.FunnelGiveUpConfirm,
            Hook.FunnelLevelFailed,
            Hook.FunnelNoCanoes,
            Hook.ResumedStuckModal,
            Hook.ResumedGiveUpConfirm,
            Hook.ResumedLevelFailed,
        };

        private GameObject? _unboundHost;

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
        }

        [TearDown]
        public void TearDown()
        {
            if (_unboundHost != null)
            {
                Object.Destroy(_unboundHost);
                _unboundHost = null;
            }

            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator QuitFromThePause_WithANoticeVisible_LevelFailedHidesItInThatCall_NeverComesBack()
        {
            yield return Load31(1000, 5);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            // Dos avisos reales que siguen existiendo tras la pregunta 48 (b): el del rescate (sale en el 31) a la vista y «+1
            // Luciérnaga» esperando su turno.
            string shown = UiText.RescueApplied;
            string waiting = UiText.RevealGiftToast;
            view.ShowNotice(shown);
            view.ShowNotice(waiting);
            Assert.That(view.ToastText, Is.EqualTo(shown));
            Assert.That(view.NoticesQueued, Is.EqualTo(1));

            // La pausa no cambia de fase: el aviso sigue a la vista detrás del menú y de «¿Salir del nivel?».
            view.PauseButton.onClick.Invoke();
            view.Pause.QuitButton.onClick.Invoke();
            Assert.That(view.Confirm.Visible, Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.ToastText, Is.EqualTo(shown), "condición del test: el aviso se ve al confirmar «Salir»");

            view.Confirm.ConfirmButton.onClick.Invoke();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelFailed));
            Assert.That(view.Failed.Visible, Is.True);
            Assert.That(view.ToastText, Is.Null, "se oculta en la llamada que abre Nivel fallido (pregunta 42 b)");
            Assert.That(view.NoticeToast.Visible, Is.False);
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "el que esperaba sigue en la cola");
            StepAndAssertNothingShows(boot, 4000, "con Nivel fallido abierto");

            // «Reintentar»: otro intento vacía la cola (OnRestart) y el aviso ocultado no vuelve en la intro ni en juego.
            view.Failed.RetryButton.onClick.Invoke();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(view.NoticesQueued, Is.EqualTo(0));
            var seen = new List<string>();
            for (int ms = 0; ms < 8000; ms += PlayModeHelpers.FrameMs)
            {
                CollectToast(view, seen);
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(seen, Does.Not.Contain(shown).And.Not.Contain(waiting), "ninguno de los dos vuelve en el intento nuevo");
        }

        [UnityTest]
        public IEnumerator RestartWithoutCanoes_WithANoticeVisible_NoCanoesHidesItInThatCall_TheQueuedOneComesAfter()
        {
            yield return Load31(1000, 0);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            string shown = UiText.RescueApplied;
            string waiting = UiText.RevealGiftToast;
            view.ShowNotice(shown);
            view.ShowNotice(waiting);
            Assert.That(view.ToastText, Is.EqualTo(shown));
            Assert.That(view.NoticesQueued, Is.EqualTo(1));

            // Volver a la orilla desde la cabecera con 0 canoas, en juego (04 §7.1): abre Sin canoas.
            Assert.That(boot.PressRestart(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.NoCanoes));
            Assert.That(view.NoCanoes.Visible, Is.True);
            Assert.That(view.ToastText, Is.Null, "se oculta en la llamada que abre Sin canoas (pregunta 42 b)");
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "el que esperaba sigue en la cola");
            StepAndAssertNothingShows(boot, 4000, "con Sin canoas abierto");

            // La X vuelve al tablero: sale el que esperaba y el ocultado no vuelve.
            Assert.That(boot.PressNoCanoesClose(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            var seen = new List<string>();
            for (int ms = 0; ms < 8000; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                CollectToast(view, seen);
            }

            Assert.That(seen, Is.EqualTo(new[] { waiting }), "el aviso ocultado se descartó: no vuelve");
        }

        [UnityTest]
        public IEnumerator FunnelHook_OutOfOrder_DoesNotThrow_HidesTheNoticeAndTheCardInThatCall_NeitherComesBack(
            [ValueSource(nameof(OutOfOrderHooks))] Hook hook)
        {
            yield return Load31(1000, 5);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            string text = ForceNoticeAndCard(boot);

            Assert.That(() => Invoke(view, hook), Throws.Nothing, hook + " fuera de orden");
            Assert.That(view.ToastText, Is.Null, hook + ": el aviso se oculta en esa llamada");
            Assert.That(view.NoticeToast.Visible, Is.False);
            Assert.That(view.Sighting.Visible, Is.False, hook + ": la tarjeta del miquito, también");
            Assert.That(view.NoticesQueued, Is.EqualTo(0));

            for (int ms = 0; ms < 8000; ms += PlayModeHelpers.FrameMs)
            {
                Assert.That(() => PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs), Throws.Nothing, hook + " (" + ms + " ms)");
                Assert.That(view.ToastText, Is.Not.EqualTo(text), hook + ": el aviso no vuelve (" + ms + " ms)");
                Assert.That(view.Sighting.Visible, Is.False, hook + ": la tarjeta no vuelve (" + ms + " ms)");
            }
        }

        [UnityTest]
        public IEnumerator SafetyNet_NoticeAndCardForcedBehindAguasEstancadas_HiddenOnTheNextFrame_NeverComeBack()
        {
            yield return Load31(1000, 5);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            FunnelSetup.GetStuck(boot);
            view.ShowNotice(UiText.RevealGiftToast); // espera a que se vuelva a jugar
            Assert.That(view.NoticesQueued, Is.EqualTo(1));

            // Un camino que abrió el modal sin avisar a la vista: lo que se vea se oculta en el fotograma siguiente.
            string text = ForceNoticeAndCard(boot);
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.StuckModal));
            Assert.That(view.ToastText, Is.Null, "la red de seguridad oculta el aviso");
            Assert.That(view.Sighting.Visible, Is.False, "y la tarjeta");
            Assert.That(view.NoticesQueued, Is.EqualTo(1), "la cola sigue esperando");
            for (int ms = 0; ms < 4000; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(view.ToastText, Is.Null, "con Aguas estancadas abierto (" + ms + " ms)");
                Assert.That(view.Sighting.Visible, Is.False, "con Aguas estancadas abierto (" + ms + " ms)");
            }

            // Al volver a jugar (el rescate) sale el que esperaba y después «¡Una cuia más!»; el forzado no vuelve.
            view.Stuck.RescueButton.onClick.Invoke();
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Playing));
            var seen = new List<string>();
            for (int ms = 0; ms < 12000; ms += PlayModeHelpers.FrameMs)
            {
                CollectToast(view, seen);
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(seen, Is.EqualTo(new[] { UiText.RevealGiftToast, UiText.RescueApplied }));
            Assert.That(seen, Does.Not.Contain(text));
        }

        [UnityTest]
        public IEnumerator WithoutFlow_TheNoticeHooksDoNotThrow_TheNoticeWaits_NothingShows()
        {
            // Una pantalla como la de GameBootstrap, pero sin Bind: _flow es null (NoticesHeld, TickNotices, PlaceToast y
            // PrefetchWorldBanner lo admiten).
            yield return Load31(1000, 5);
            GameBootstrap boot = GameBootstrap.Current!;
            _unboundHost = new GameObject("UnboundGameScreen");
            GameScreen? screen = null;
            Transform host = _unboundHost.transform;
            Assert.That(() => { screen = new GameScreen(host, boot.View!.CardArt, new DevProbe(), boot); }, Throws.Nothing);
            GameScreen unbound = screen!;
            string text = UiText.RescueApplied;
            Assert.That(() =>
            {
                unbound.ShowNotice(text);
                for (int f = 0; f < 120; f++)
                {
                    unbound.Tick(PlayModeHelpers.FrameMs);
                }
            }, Throws.Nothing, "encolar y avanzar sin flujo");
            Assert.That(unbound.ToastText, Is.Null, "sin flujo, los avisos esperan (NoticesHeld)");
            Assert.That(unbound.NoticesQueued, Is.EqualTo(1));
            Assert.That(unbound.Sighting.Visible, Is.False);
            Assert.That(unbound.SightingPending, Is.False);
            Assert.That(unbound.PrefetchWorldBanner(41), Is.False, "sin flujo no pide el arte del banner");

            // Un aviso forzado a la vista sin flujo: la red de seguridad no lo toca (no hay modal) y nada lanza al avanzar.
            unbound.NoticeToast.Show(text, NoticeTiming.ReadMs(text));
            Assert.That(() => unbound.Tick(PlayModeHelpers.FrameMs), Throws.Nothing);
            Assert.That(unbound.ToastText, Is.EqualTo(text));
        }

        // El 31 con el tablero 6 del tutorial (3 → 4 lo bloquea; FunnelSetup.GetStuck), con seeds semillas y canoes canoas, en
        // juego tras saltar la intro.
        private static IEnumerator Load31(int seeds, int canoes)
        {
            LaunchOptions.LevelOffsetOverride = 25;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(seeds, canoes));
            yield return PlayModeHelpers.LoadGame(31, new MemoryProgressStore(31, 30));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.TapAnywhere(), Is.True, "salta la intro");
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(boot.View!.ToastText, Is.Null);
            Assert.That(boot.View.Sighting.Visible, Is.False);
        }

        // El aviso y la tarjeta del miquito (la especie del tramo, con su retrato, su nombre y su dato), a la vista a la vez: un
        // estado que el juego no produce (salen de uno en uno), para ver que el gancho oculta los dos. Devuelve el texto del aviso.
        private static string ForceNoticeAndCard(GameBootstrap boot)
        {
            GameScreen view = boot.View!;
            FaunaSpecies? species = boot.Fauna?.ForLevel(boot.Flow!.LevelNumber);
            Assert.That(species, Is.Not.Null, "condición del test: una especie para el tramo del nivel");
            view.Sighting.Show(view.CardArt.MonkeyArtFor(species!).Portrait, species!.NameIn("es"), UiText.FaunaFact(species.FactKey));
            string text = UiText.ExtraCuiaGift; // un aviso real, distinto de los que encolan los tests (el rescate y «+1 Luciérnaga»)
            view.NoticeToast.Show(text, NoticeTiming.ReadMs(text));
            Assert.That(view.ToastText, Is.EqualTo(text), "condición del test: el aviso, a la vista");
            Assert.That(view.Sighting.Visible, Is.True, "condición del test: la tarjeta, a la vista");
            return text;
        }

        private static void Invoke(GameScreen view, Hook hook)
        {
            switch (hook)
            {
                case Hook.FunnelGiveUpConfirm:
                    view.OnFunnelPhase(FlowPhase.GiveUpConfirm);
                    break;
                case Hook.FunnelLevelFailed:
                    view.OnFunnelPhase(FlowPhase.LevelFailed);
                    break;
                case Hook.FunnelNoCanoes:
                    view.OnFunnelPhase(FlowPhase.NoCanoes);
                    break;
                case Hook.ResumedStuckModal:
                    view.OnResumed(FlowPhase.StuckModal);
                    break;
                case Hook.ResumedGiveUpConfirm:
                    view.OnResumed(FlowPhase.GiveUpConfirm);
                    break;
                case Hook.ResumedLevelFailed:
                    view.OnResumed(FlowPhase.LevelFailed);
                    break;
            }
        }

        private static void StepAndAssertNothingShows(GameBootstrap boot, int totalMs, string when)
        {
            GameScreen view = boot.View!;
            for (int ms = 0; ms < totalMs; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                Assert.That(view.ToastText, Is.Null, when + " (" + ms + " ms)");
                Assert.That(view.Sighting.Visible, Is.False, when + " (" + ms + " ms)");
            }
        }

        // Los avisos que se ven, en orden y sin repetir el mismo seguido.
        private static void CollectToast(GameScreen view, List<string> seen)
        {
            string? now = view.ToastText;
            if (now != null && (seen.Count == 0 || seen[seen.Count - 1] != now))
            {
                seen.Add(now);
            }
        }
    }
}
