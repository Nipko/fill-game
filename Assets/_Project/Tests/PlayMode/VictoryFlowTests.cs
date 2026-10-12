#nullable enable
using System;
using System.Collections;
using AF.Ads;
using AF.Core.Rules;
using AF.Core.Session;
using AF.GameFlow;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Victoria con la cadena corta de D-008 en la escena (punto 3; 02 §6; tarea B2-4): la Cosecha cuenta hacia arriba en
    /// 400 ms hasta +20 y el saldo de arriba la recibe, y no antes (D-011, punto 1: ni durante el último vertido ni en la
    /// celebración); desde el 11, «Cosecha doble: x2 por video» («+20 → +40») la duplica exactamente con un anuncio simulado y
    /// la victoria pasa sola al nivel siguiente por el camino de Siguiente, sin intersticial (D-011, punto 4), con Inicio
    /// inactivo mientras espera, sin cargar dos veces si se toca Siguiente (AD_GATE cancela la espera aunque el anuncio siga
    /// abierto) y, como un toque en Siguiente, entre dos fotogramas
    /// (después del Tick del flujo); sin recompensa se queda; sin fill el botón no aparece; antes del 11 no existe; sin la
    /// Cosecha doble, Siguiente lleva a N + 1 sin auto-avance; sin solapes y con controles ≥ 48 dp.
    /// </summary>
    public sealed class VictoryFlowTests
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
        public IEnumerator Level11_TheHarvestCountsUp_AndTheDoubleHarvestDoublesItExactly()
        {
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(100));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(120), "la Cosecha ya está cobrada (eco_reward_normal = 20)");
            Assert.That(view.VictoryVisible, Is.True);
            Assert.That(view.DoubleHarvestShown, Is.True, "desde el 11, con video");
            Assert.That(view.DoubleHarvestGainText, Is.EqualTo("+20 → +40"));
            Assert.That(view.WalletBar.Visible, Is.True, "el saldo arriba en la victoria");

            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.HarvestShown, Is.EqualTo(0), "empieza a contar cuando el panel ya subió");
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(100), "el saldo recibe la Cosecha mientras cuenta");
            PlayModeHelpers.Step(boot, 350);
            Assert.That(view.HarvestShown, Is.InRange(1, 19), "a mitad de los 400 ms");
            PlayModeHelpers.Step(boot, 400);
            Assert.That(view.HarvestText, Is.EqualTo("+20"));
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(120));

            view.DoubleHarvestButton.onClick.Invoke();
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(1));
            Assert.That(FunnelSetup.AdsPresenter.Last!.Placement, Is.EqualTo(AdPlacement.DoubleHarvest));
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(140), "x2 exacto: +20 → +40");
            Assert.That(boot.Flow!.HarvestSeeds, Is.EqualTo(40));
            Assert.That(view.DoubleHarvestShown, Is.False, "una vez por victoria");
            PlayModeHelpers.Step(boot, 500);
            Assert.That(view.HarvestText, Is.EqualTo("+40"));
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(140));
            Assert.That(boot.PressDoubleHarvest(), Is.False);

            // Siguiente a los 500 ms de doblar, antes de que la victoria avance sola (400 + 600 ms, D-011 punto 4).
            Assert.That(view.AutoNextPending, Is.True);
            view.NextButton.onClick.Invoke();
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(12), "Siguiente → N + 1 (en el 11 no hay intersticial)");
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TheBalanceWaitsForTheVictoryScreen_ThenCountsUpWithTheHarvest()
        {
            // D-011, punto 1: la Cosecha se cobra en el instante lógico de la victoria (02 §14), pero el saldo de arriba no la
            // enseña ni mientras termina el último vertido (PLAYING con la victoria pendiente) ni durante la celebración: la
            // recibe en la pantalla de victoria, mientras la fila cuenta (02 §6).
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(100));
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore(1, 0));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            Pour last = VictorySetup.PlayAllButTheLastPour(boot);
            PlayModeHelpers.Step(boot, 50);
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(100), "antes de ganar");

            boot.TapCuia(last.Source);
            boot.TapCuia(last.Target);
            Assert.That(flow.IsResolvingWin, Is.True, "el último vertido gana: victoria lógica pendiente");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(120), "la Cosecha ya está cobrada (eco_reward_normal = 20)");
            int resolving = 0;
            int celebration = 0;
            for (int elapsed = 0; elapsed < 15000 && flow.Phase != FlowPhase.VictoryScreen; elapsed += PlayModeHelpers.FrameMs)
            {
                FlowPhase before = flow.Phase;
                bool pending = flow.IsResolvingWin;
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                resolving += pending ? 1 : 0;
                celebration += before == FlowPhase.Celebration ? 1 : 0;
                Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(100), "el saldo espera a la victoria (" + before + ", " + elapsed + " ms)");
                Assert.That(boot.Wallet.Seeds, Is.EqualTo(120));
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(resolving, Is.GreaterThan(0), "fotogramas con la victoria pendiente (el último vertido se anima)");
            Assert.That(celebration, Is.GreaterThan(0), "fotogramas de celebración");
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(100), "la fila aún no ha contado");

            // En la victoria, el saldo sube con la cuenta de la fila y termina en el de antes más la Cosecha.
            PlayModeHelpers.Step(boot, 100);
            Assert.That(view.HarvestShown, Is.EqualTo(0));
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(100));
            PlayModeHelpers.Step(boot, 350);
            Assert.That(view.HarvestShown, Is.InRange(1, 19), "a mitad de los 400 ms");
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(100 + view.HarvestShown), "el saldo, con lo ya contado");
            PlayModeHelpers.Step(boot, 400);
            Assert.That(view.HarvestText, Is.EqualTo("+20"));
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(120));
        }

        [UnityTest]
        public IEnumerator Level24_TheDoubleHarvest_AdvancesAlone_WithoutInterstitial()
        {
            // D-011, punto 4: tras el video de la Cosecha doble, la fila cuenta de +N a +2N (400 ms), se ve 600 ms más y la
            // victoria pasa sola al nivel siguiente por el camino de Siguiente, sin tocar nada. Nivel 24 (tablero 3 con
            // af_level_offset = 21), 3.ª victoria en 22-60 desde la 2.ª sesión: con Siguiente sin más, saldría el intersticial
            // (captura 18), pero AdPolicy lo bloquea 60 s tras un recompensado (AfterRewarded), sin forzar nada. Mientras
            // espera, Inicio y la Cosecha doble quedan inactivos, también al recolocar la pantalla.
            LaunchOptions.LevelOffsetOverride = 21;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(100, levelsSinceInterstitial: 2));
            yield return PlayModeHelpers.LoadGame(24, new MemoryProgressStore(24, 23));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(200)); // ≥ 120 s de sesión
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 900);
            int harvest = boot.Flow!.HarvestSeeds;
            Assert.That(harvest, Is.EqualTo(20), "eco_reward_normal");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(100 + harvest));
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(100 + harvest));
            Assert.That(boot.Ads!.CheckInterstitial(24, AdContext.VictoryScreen).IsAllowed, Is.True, "sin la Cosecha doble, Siguiente enseñaría el intersticial");
            Assert.That(view.DoubleHarvestShown, Is.True);
            Assert.That(view.HomeButton.interactable, Is.True);
            Assert.That(view.AutoNextPending, Is.False, "sin la Cosecha doble no avanza sola");
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(0));

            view.DoubleHarvestButton.onClick.Invoke();
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(1));
            Assert.That(FunnelSetup.AdsPresenter.Last!.Placement, Is.EqualTo(AdPlacement.DoubleHarvest));
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(100 + (2 * harvest)), "S + 2N");
            Assert.That(view.AutoNextPending, Is.True);
            Assert.That(boot.Ads.CheckInterstitial(24, AdContext.VictoryScreen).Reason, Is.EqualTo(AdBlockReason.AfterRewarded));

            // Mientras espera: la Cosecha doble ya no está, Inicio inactivo (un toque no lleva al inicio) y Siguiente activo.
            Assert.That(view.DoubleHarvestShown, Is.False);
            Assert.That(view.DoubleHarvestButton.interactable, Is.False);
            Assert.That(view.HomeButton.interactable, Is.False, "Inicio inactivo durante la espera");
            Assert.That(view.NextButton.interactable, Is.True, "Siguiente puede adelantarla");
            ExecuteEvents.Execute(view.HomeButton.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            view.HomeButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "un toque en Inicio no lleva al inicio");
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // recolocación (LayoutHud → LayoutVictory)
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.HomeButton.interactable, Is.False, "también al recolocar la pantalla");
            Assert.That(view.DoubleHarvestButton.interactable, Is.False);

            // La fila cuenta y el saldo llega a S + 2N antes de cargar.
            PlayModeHelpers.Step(boot, 800); // 816 ms desde la recompensa
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "aún se ve la victoria");
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(24));
            Assert.That(view.HarvestText, Is.EqualTo("+40"));
            Assert.That(view.WalletBar.SeedsShown, Is.EqualTo(100 + (2 * harvest)), "el saldo, en S + 2N antes de cargar");

            PlayModeHelpers.Step(boot, 300); // 1.116 ms: pasó sola
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(25), "N + 1 sin tocar nada");
            Assert.That(VictorySetup.InNewLevel(boot.Flow.Phase), Is.True, "en su INTRO, LEVEL_CARD o PLAYING: " + boot.Flow.Phase);
            Assert.That(boot.App.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(view.AutoNextPending, Is.False);
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(1), "solo el recompensado: sin intersticial");
            Assert.That(boot.AdOverlayView == null || !boot.AdOverlayView.Visible, Is.True, "ninguna pantalla de anuncio");
            Assert.That(boot.Ads.Ledger.InterstitialsToday, Is.EqualTo(0));
            Assert.That(boot.Ads.Ledger.LevelsSinceInterstitial, Is.EqualTo(3), "la cadencia sigue pendiente para la próxima victoria");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(100 + (2 * harvest)));
        }

        [UnityTest]
        public IEnumerator Level11_NextDuringTheWait_LoadsTheNextLevelOnlyOnce()
        {
            // D-011, punto 4: Siguiente puede adelantar el avance tras la Cosecha doble; N + 1 se carga una sola vez (ni N + 2
            // ni una segunda carga de N + 1). Tras el recompensado no hay intersticial (AfterRewarded), así que Siguiente carga
            // N + 1 en esa misma llamada y la espera queda sin efecto; que AD_GATE la cancele con el anuncio aún abierto lo mira
            // Level11_AnAdGateLeftOpen_CancelsTheWait_AndLoadsTheNextLevelOnce.
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(100));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 900);
            view.DoubleHarvestButton.onClick.Invoke();
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(140));
            Assert.That(view.AutoNextPending, Is.True);
            PlayModeHelpers.Step(boot, 200);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen));

            view.NextButton.onClick.Invoke();
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(12));
            Assert.That(view.AutoNextPending, Is.False, "la carga de N + 1 deja la espera sin efecto");
            LevelSession loaded = boot.Flow.Session;
            PlayModeHelpers.Step(boot, 2000);
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(12), "sin llegar a N + 2");
            Assert.That(boot.Flow.Session, Is.SameAs(loaded), "N + 1 no se vuelve a cargar");
            Assert.That(VictorySetup.InNewLevel(boot.Flow.Phase), Is.True, "en su INTRO, LEVEL_CARD o PLAYING: " + boot.Flow.Phase);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(1), "solo el recompensado");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(140));
        }

        [UnityTest]
        public IEnumerator Level11_AnAdGateLeftOpen_CancelsTheWait_AndLoadsTheNextLevelOnce()
        {
            // D-011, punto 4: si Siguiente adelanta el avance tras la Cosecha doble, AD_GATE cancela la espera (OnAdGate). En el
            // juego es una defensa: tras el recompensado el intersticial no toca (AfterRewarded) y N + 1 se carga en la misma
            // llamada a Siguiente. Para dejar AD_GATE abierto con la espera armada, el test cambia la puerta del flujo por una
            // que retiene el anuncio hasta cerrarlo (ImmediateAdPresenter no puede retenerlo y AdPolicy no se fuerza). Con el
            // anuncio abierto, la espera ya no corre ni avanza; al cerrarlo, N + 1 se carga una sola vez.
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(100));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 900);
            view.DoubleHarvestButton.onClick.Invoke();
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(140));
            Assert.That(view.AutoNextPending, Is.True);
            var gate = new HeldAdGate();
            flow.AdGate = gate; // ya contó la victoria (RecordWin) con la puerta del juego
            PlayModeHelpers.Step(boot, 200);

            view.NextButton.onClick.Invoke();
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.AdGate));
            Assert.That(gate.Runs, Is.EqualTo(1));
            Assert.That(flow.LevelNumber, Is.EqualTo(11), "el anuncio sigue abierto");
            Assert.That(view.AutoNextPending, Is.False, "AD_GATE cancela la espera");
            Assert.That(view.NextButton.interactable, Is.False);
            Assert.That(view.HomeButton.interactable, Is.False);
            Assert.That(view.DoubleHarvestButton.interactable, Is.False);

            PlayModeHelpers.Step(boot, 2000); // más allá de los 400 + 600 ms de la espera
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.AdGate), "la espera no avanza con el anuncio abierto");
            Assert.That(flow.LevelNumber, Is.EqualTo(11));
            Assert.That(view.AutoNextPending, Is.False);
            Assert.That(gate.Runs, Is.EqualTo(1), "Siguiente no se vuelve a pulsar");

            gate.Close();
            Assert.That(flow.LevelNumber, Is.EqualTo(12));
            Assert.That(VictorySetup.InNewLevel(flow.Phase), Is.True, "en su INTRO, LEVEL_CARD o PLAYING: " + flow.Phase);
            LevelSession loaded = flow.Session;
            PlayModeHelpers.Step(boot, 2000);
            Assert.That(flow.LevelNumber, Is.EqualTo(12), "sin llegar a N + 2");
            Assert.That(flow.Session, Is.SameAs(loaded), "N + 1 no se vuelve a cargar");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(1), "solo el recompensado");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(140));
        }

        [UnityTest]
        public IEnumerator Level11_TheAutoAdvance_WaitsForTheFlowTick_LikeATapOnNext()
        {
            // D-011, punto 4: al vencer la espera tras la Cosecha doble, la vista no pulsa Siguiente dentro de su fotograma (el
            // resto de GameScreen.Tick y LevelFlow.Tick correrían sobre N + 1 con el dt de N, y el flujo de N + 1 avanzaría un
            // fotograma de intro que la vista no animó): la deja vencida, con Inicio aún inactivo, y GameBootstrap.Update pulsa
            // Siguiente después de LevelFlow.Tick (GameScreen.TakeAutoNext). N + 1 se carga entre dos fotogramas, como con un
            // toque en Siguiente, y una sola vez.
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(100));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 900);
            view.DoubleHarvestButton.onClick.Invoke();
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(140));
            Assert.That(view.AutoNextPending, Is.True);
            Assert.That(view.TakeAutoNext(), Is.False, "la espera aún no venció");

            // Solo la vista, más allá de la espera (400 + 600 ms): vence, pero no carga nada.
            for (int t = 0; t < 1200; t += PlayModeHelpers.FrameMs)
            {
                view.Tick(PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "la vista no pulsa Siguiente dentro de su Tick");
            Assert.That(flow.LevelNumber, Is.EqualTo(11));
            Assert.That(view.HarvestText, Is.EqualTo("+40"));
            Assert.That(view.AutoNextPending, Is.True, "vencida, hasta que se consume");
            Assert.That(view.HomeButton.interactable, Is.False, "Inicio sigue inactivo");
            flow.Tick(PlayModeHelpers.FrameMs, view.IsViewIdle);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "ni el flujo en el suyo");
            Assert.That(flow.LevelNumber, Is.EqualTo(11));

            // Después del Tick del flujo, como GameBootstrap.Update: Siguiente, una sola vez.
            Assert.That(view.TakeAutoNext(), Is.True);
            Assert.That(view.AutoNextPending, Is.False);
            Assert.That(view.TakeAutoNext(), Is.False, "una sola vez");
            Assert.That(boot.PressNext(), Is.True);
            Assert.That(flow.LevelNumber, Is.EqualTo(12), "N + 1 (en el 11 no hay intersticial)");
            Assert.That(VictorySetup.InNewLevel(flow.Phase), Is.True, "en su INTRO, LEVEL_CARD o PLAYING: " + flow.Phase);
            LevelSession loaded = flow.Session;
            PlayModeHelpers.Step(boot, 2000);
            Assert.That(flow.LevelNumber, Is.EqualTo(12), "sin llegar a N + 2");
            Assert.That(flow.Session, Is.SameAs(loaded), "N + 1 no se vuelve a cargar");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(1), "solo el recompensado");
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(140));
        }

        [UnityTest]
        public IEnumerator Level11_DoubleHarvestVideoClosedEarly_StaysOnTheVictoryScreen()
        {
            // D-011, punto 4: sin recompensa (el video se cierra antes de tiempo) no se dobla nada y no avanza sola.
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(100));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 900);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(120));
            Assert.That(view.DoubleHarvestShown, Is.True);

            FunnelSetup.AdsPresenter.Close = SimulatedAdClose.ClosedEarly;
            view.DoubleHarvestButton.onClick.Invoke();
            Assert.That(FunnelSetup.AdsPresenter.Presented, Is.EqualTo(1), "el video se abrió");
            Assert.That(view.AutoNextPending, Is.False);
            PlayModeHelpers.Step(boot, 3000);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "se queda en la victoria");
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(11));
            Assert.That(boot.Flow.HarvestDoubled, Is.False);
            Assert.That(boot.Wallet.Seeds, Is.EqualTo(120), "el saldo de antes más N");
            Assert.That(view.HarvestText, Is.EqualTo("+20"));
            Assert.That(view.HomeButton.interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator Level1_ShowsTheHarvest_WithoutDoubleHarvest_AndDoesNotAdvanceAlone()
        {
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(1, new MemoryProgressStore(1, 0));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 800);
            Assert.That(view.HarvestText, Is.EqualTo("+20"));
            Assert.That(view.DoubleHarvestShown, Is.False, "la Cosecha doble llega en el 11");
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(20));

            PlayModeHelpers.Step(boot, 5000);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "victory_autonext_s = 0: sin auto-avance");
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Level11_WithoutFill_TheDoubleHarvestDoesNotAppear()
        {
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.AdFillOverride = FakeAdFill.NoFill;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.Flow!.DoubleHarvestOffered, Is.True, "el flujo lo ofrecería");
            Assert.That(boot.View!.DoubleHarvestShown, Is.False, "02 §6: sin anuncio precargado el botón no aparece");
            Assert.That(boot.PressDoubleHarvest(), Is.False);
            Assert.That(boot.Wallet!.Seeds, Is.EqualTo(20));
        }

        [UnityTest]
        public IEnumerator VictoryPanel_DoubleNextAndHome_DoNotOverlap_AndAreTappable()
        {
            GameScreen.SimulatedScreen = new Vector4(1080f, 2340f, 96f, 48f); // SM-S938B: 360 × 780 dp con zona segura
            LaunchOptions.LevelOffsetOverride = 10;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(0));
            yield return PlayModeHelpers.LoadGame(11, new MemoryProgressStore(11, 10));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 900);
            try
            {
                RectTransform root = (RectTransform)view.Canvas.transform;
                Rect dbl = VictorySetup.CanvasRect((RectTransform)view.DoubleHarvestButton.transform, root);
                Rect next = VictorySetup.CanvasRect((RectTransform)view.NextButton.transform, root);
                Rect home = VictorySetup.CanvasRect((RectTransform)view.HomeButton.transform, root);
                Assert.That(dbl.yMax, Is.LessThanOrEqualTo(next.yMin + 0.5f), "Cosecha doble encima de Siguiente");
                Assert.That(next.yMax, Is.LessThanOrEqualTo(home.yMin + 0.5f), "Inicio debajo de Siguiente");
                Assert.That(dbl.height, Is.GreaterThanOrEqualTo(LobbyScreen.MinTouchDp - 0.5f), "07 §7.8: ≥ 48 dp");
                Assert.That(next.height, Is.GreaterThanOrEqualTo(LobbyScreen.MinTouchDp - 0.5f));
                Assert.That(home.height, Is.GreaterThanOrEqualTo(LobbyScreen.MinTouchDp - 0.5f));
                Assert.That(dbl.yMin - view.VictoryPanelHeight, Is.GreaterThan(0f), "el panel no sube hasta la fila del saldo");
                Assert.That(home.yMin - next.yMin, Is.EqualTo(68f).Within(0.5f), "Inicio, 68 dp bajo el borde del CTA (desde B1-6)");
            }
            finally
            {
                GameScreen.SimulatedScreen = null;
            }
        }

        // AD_GATE que deja el anuncio abierto hasta Close (como un intersticial que el jugador aún no cerró).
        private sealed class HeldAdGate : IAdGate
        {
            private Action? _onDone;

            public int Runs { get; private set; }

            public void RecordWin(int level)
            {
            }

            public void Run(int wonLevel, Action onDone)
            {
                Runs++;
                _onDone = onDone;
            }

            public void Close()
            {
                Action? onDone = _onDone;
                _onDone = null;
                onDone?.Invoke();
            }
        }
    }

    /// <summary>Victorias en la escena con el reloj del test.</summary>
    internal static class VictorySetup
    {
        /// <summary>Perfil de una segunda sesión (no la de instalación) con <paramref name="seeds"/> semillas.</summary>
        public static Profile Profile(int seeds, int canoes = 5, int levelsSinceInterstitial = 0)
        {
            Profile profile = FunnelSetup.Profile(seeds, canoes);
            profile.Ads.LevelsSinceInterstitial = levelsSinceInterstitial;
            return profile;
        }

        /// <summary>Salta la intro, juega la solución del solver y avanza hasta VICTORY_SCREEN.</summary>
        public static void WinToVictoryScreen(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            if (flow.Phase == FlowPhase.Intro)
            {
                boot.TapAnywhere();
            }

            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                boot.TapCuia(move.Source);
                boot.TapCuia(move.Target);
            }

            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
            for (int elapsed = 0; elapsed < 15000 && flow.Phase != FlowPhase.VictoryScreen; elapsed += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
        }

        /// <summary>Fases de un nivel recién cargado: INTRO, LEVEL_CARD o PLAYING.</summary>
        public static bool InNewLevel(FlowPhase phase)
        {
            return phase == FlowPhase.Intro || phase == FlowPhase.LevelCard || phase == FlowPhase.Playing;
        }

        /// <summary>
        /// Salta la intro, juega la solución del solver menos el último vertido y espera a que la cola de la vista se vacíe;
        /// devuelve ese último vertido, que gana el nivel.
        /// </summary>
        public static Pour PlayAllButTheLastPour(GameBootstrap boot)
        {
            LevelFlow flow = boot.Flow!;
            if (flow.Phase == FlowPhase.Intro)
            {
                boot.TapAnywhere();
            }

            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            Assert.That(solution.MoveCount, Is.GreaterThan(0));
            for (int i = 0; i < solution.MoveCount - 1; i++)
            {
                Pour move = solution.GetMove(i);
                boot.TapCuia(move.Source);
                boot.TapCuia(move.Target);
            }

            PlayModeHelpers.StepUntilIdle(boot, 15000);
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Playing), "aún sin ganar");
            Assert.That(flow.AcceptsBoardInput, Is.True);
            return solution.GetMove(solution.MoveCount - 1);
        }

        // Rectángulo de rt en dp del lienzo root, con origen arriba a la izquierda y la y hacia abajo.
        public static Rect CanvasRect(RectTransform rt, RectTransform root)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Rect bounds = root.rect;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = root.InverseTransformPoint(corners[i]);
                float x = local.x - bounds.xMin;
                float y = bounds.yMax - local.y;
                xMin = Mathf.Min(xMin, x);
                xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y);
                yMax = Mathf.Max(yMax, y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
