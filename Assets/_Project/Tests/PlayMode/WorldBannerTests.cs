#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Core.Rules;
using AF.Economy;
using AF.GameFlow;
using AF.Presentation;
using AF.Save;
using AF.Solver.Search;
using AF.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Banner de entrada a un mundo en la escena (07 §2.2, regla 1; 02 §6; glosario §8 y §10, <c>world_banner</c>; decisión del
    /// dueño del 2026-10-11), con el catálogo real y el banner encendido como en el juego (<see cref="LaunchOptions.LevelCardsOverride"/>
    /// = true; los demás tests lo apagan con las tarjetas, <see cref="PlayModeHelpers"/>): sale en el 41 antes de la intro con
    /// «Mundo 3: Manaos · Brasil (Amazonas) · Negro + Solimões», Capi presentando y Rosa fuera del agua, sin cerrarse solo y con la
    /// intro retenida en t = 0; 1 toque (el velo es lo único que recibe toques) empieza su salida y la intro (o la tarjeta
    /// previa) solo arranca cuando el banner ya no se ve (07 §5.1: «antes de esta animación, no encima»); un segundo toque
    /// durante la salida la termina en el acto; no sale en el 42, ni al reintentar, ni al reanudar; si el nivel tiene tarjeta
    /// previa, sale después del banner y el regalo de la Luciérnaga espera a los dos (nivel 141 con el tablero del 100:
    /// <see cref="LaunchOptions.LevelOffsetOverride"/> = 41); quieto con Reducir movimiento; sin asignaciones mientras anima ni
    /// mientras espera el arte; el arte pintado fuera del hilo principal, como en el juego: mientras llega, el velo en el acto y
    /// los toques ignorados; si falla, no se puede encolar o no llega en su tiempo límite, la tarjeta sin arte (el respaldo de
    /// D-012) y el arte tardío descartado; el tiempo límite y el <c>wait_ms</c>, de reloj real desde la petición y no la suma
    /// del dt; pedido antes en la victoria del 10 y al ofrecer «Jugar» con un nivel que abre mundo; el origen de la superficie
    /// de madera, el hilo en que corrió el trabajo del arte (el principal en el acto; revisión de T52, U1);
    /// y las sondas <c>AF_STATE world_banner</c>, <c>world_banner_art</c> y <c>world_banner_closed</c> y el
    /// <c>AF_LAYOUT</c>. Salvo los tests del arte en otro hilo, todos lo piden en el acto
    /// (<see cref="LaunchOptions.BannerArtSyncOverride"/>, <see cref="CardSetup.Load"/>).
    /// </summary>
    public sealed class WorldBannerTests
    {
        private readonly List<string> _lines = new List<string>();

        [SetUp]
        public void SetUp()
        {
            FunnelSetup.Hooks();
            GameScreen.SimulatedScreen = HudLabelTests.Screen780; // 360 × 780 dp con zona segura
            _lines.Clear();
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        [UnityTest]
        public IEnumerator Banner_Level41_BeforeTheIntro_OneTapStartsIt_NotOn42()
        {
            yield return CardSetup.Load(41);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = view.WorldBannerView;

            // LOADING → LEVEL_CARD con el banner del mundo 3, antes de la intro y sin tarjeta previa (el 41 es normal).
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(flow.WorldBanner, Is.EqualTo(3));
            Assert.That(flow.LevelCard, Is.Null);
            Assert.That(view.LevelCard.Visible, Is.False);
            Assert.That(banner.Visible, Is.True);
            Assert.That(view.CardArt.MascotsBuilt, Is.True, "las mascotas y la viñeta se generan al abrirlo");
            Assert.That(view.CardArt.BannerSceneCount, Is.EqualTo(1), "una sola viñeta en memoria");
            Assert.That(banner.Text, Is.EqualTo("Mundo 3: Manaos · Brasil (Amazonas) · Negro + Solimões"));
            Assert.That(banner.Text, Is.EqualTo(UiText.WorldBanner(3)));
            Assert.That(banner.NameInTitle, Is.True, "«Mundo 3: Manaos» cabe en la cabecera a 24 sp");
            Assert.That(banner.TitleText, Is.EqualTo("Mundo 3: Manaos"));
            Assert.That(banner.PlaceText, Is.EqualTo("Brasil (Amazonas) · Negro + Solimões"));
            Assert.That(banner.TitleText + UiText.WorldSeparator + banner.PlaceText, Is.EqualTo(banner.Text), "la clave entera, partida en dos");
            Assert.That(view.WorldBannersShown, Is.EqualTo(1));
            Assert.That(view.LastWorldBannerProbe, Does.Contain("\"level\":41").And.Contain("\"world\":3").And.Contain("\"text\":\"Mundo 3: Manaos"));
            Assert.That(view.LastWorldBannerProbe, Does.Contain("\"then\":\"intro\"").And.Contain("\"reduce_motion\":false"), "sin tarjeta previa detrás");
            Assert.That(view.LastWorldBannerArtProbe, Does.Contain("\"level\":41").And.Contain("\"async\":false").And.Contain("\"mascots_ms\":"));
            Assert.That(banner.ArtReady, Is.True, "arte en el acto (BannerArtSyncOverride)");
            Assert.That(flow.WorldBannerExitMs, Is.EqualTo(Mathf.CeilToInt(WorldBannerView.ExitMs)), "el flujo espera la salida entera");

            // Tarjeta de 1 toque: el velo cubre la pantalla y es lo único que recibe toques, por encima de todo.
            Assert.That(banner.RaycastTargets(), Is.EqualTo(new Graphic[] { banner.Dim }));
            Assert.That(banner.Root.GetSiblingIndex(), Is.EqualTo(banner.Root.parent.childCount - 1));
            WorldBannerSetup.AssertLaidOut(view);

            // No se cierra solo; detrás, el tablero espera en el fotograma 0 de la intro. Capi presenta y Rosa está fuera.
            PlayModeHelpers.Step(boot, 3000);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(banner.Visible && !banner.Dismissing, Is.True);
            Assert.That(view.CardIntroHeld, Is.True);
            Assert.That(view.Intro.TimeMs, Is.EqualTo(0f));
            Assert.That(banner.PawAngle, Is.EqualTo(WorldBannerView.PawUpDeg).Within(0.5f), "Capi presentando con la pata (capi_present)");
            Assert.That(banner.RosaRise, Is.EqualTo(1f).Within(1e-3f), "Rosa fuera del agua (rosa_surface)");
            Assert.That(banner.Alpha, Is.EqualTo(1f));
            Assert.That(banner.Settled, Is.True, "quieto tras la entrada: Tick ya no pinta nada (guía 8)");

            // 1 toque: la salida (Rosa se sumerge y la tarjeta se funde al final), y solo después la intro de siempre (07 §5.1).
            // Mientras sale, el flujo sigue en LEVEL_CARD y el velo sigue recibiendo el toque.
            yield return null; // un fotograma de verdad: el raycast no ve un velo que el lienzo aún no ha pintado
            WorldBannerSetup.Tap(banner);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(flow.WorldBanner, Is.EqualTo(3));
            Assert.That(flow.WorldBannerClosing, Is.True);
            Assert.That(banner.Dismissing, Is.True);
            Assert.That(banner.BlocksRaycasts, Is.True, "un segundo toque termina la salida");
            Assert.That(view.LastWorldBannerClosedProbe, Is.EqualTo("{\"level\":41,\"world\":3,\"next\":\"intro\"}"), "el mismo vocabulario que then");
            PlayModeHelpers.TickExact(boot, 200);
            Assert.That(banner.RosaRise, Is.LessThan(0.6f), "se sumerge (rosa_dive)");
            Assert.That(banner.Alpha, Is.EqualTo(1f), "la tarjeta, entera mientras Rosa se sumerge");
            int frames = 0;
            while (banner.Visible)
            {
                Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "nada empieza debajo del banner");
                Assert.That(view.CardIntroHeld, Is.True);
                Assert.That(view.Intro.TimeMs, Is.EqualTo(0f), "«Llenar y teñir» espera");
                Assert.That(view.LevelCard.Visible, Is.False);
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                Assert.That(++frames, Is.LessThan(20), "la salida dura " + WorldBannerView.ExitMs + " ms");
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro), "con el banner fuera, la intro");
            Assert.That(flow.WorldBanner, Is.EqualTo(0));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.CardArt.MascotsBuilt, Is.False, "el arte del banner se suelta al cerrarlo");
            Assert.That(view.CardIntroHeld, Is.False);
            Assert.That(view.Intro.TimeMs, Is.GreaterThan(0f), "la intro de verdad");
            CardSetup.StepUntilPlaying(boot);

            // El 42 no lo lleva.
            flow.Load(42);
            PlayModeHelpers.Step(boot, 300);
            Assert.That(flow.WorldBanner, Is.EqualTo(0));
            Assert.That(banner.Visible, Is.False);
            Assert.That(view.WorldBannersShown, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Banner_SecondTapDuringTheExit_EndsItAtOnce()
        {
            // Un segundo toque mientras sale: la salida termina en el acto y la intro arranca ya (sin esperar los 340 ms).
            yield return CardSetup.Load(41);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = view.WorldBannerView;
            PlayModeHelpers.Step(boot, 1500);
            yield return null; // un fotograma de verdad: el raycast no ve un velo que el lienzo aún no ha pintado
            WorldBannerSetup.Tap(banner);
            PlayModeHelpers.TickExact(boot, 100);
            Assert.That(flow.WorldBannerClosing && banner.Dismissing, Is.True);
            Assert.That(view.Intro.TimeMs, Is.EqualTo(0f));
            yield return null;
            WorldBannerSetup.Tap(banner);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(flow.WorldBanner, Is.EqualTo(0));
            Assert.That(banner.Visible, Is.False, "fuera en el acto");
            Assert.That(view.CardArt.MascotsBuilt, Is.False);
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.Intro.TimeMs, Is.GreaterThan(0f));
            Assert.That(view.LastWorldBannerClosedProbe, Does.Contain("\"next\":\"intro\""));
            CardSetup.StepUntilPlaying(boot);
        }

        [UnityTest]
        public IEnumerator Banner_TapOnACuiaOrTheBackground_ClosesIt_WithoutSelecting()
        {
            // El flujo también lo cierra con TapCuia o el fondo (como la tarjeta previa): el banner sale sin seleccionar nada, y la
            // intro espera a que termine su salida.
            yield return CardSetup.Load(81);
            GameBootstrap boot = GameBootstrap.Current!;
            WorldBannerView banner = boot.View!.WorldBannerView;
            Assert.That(boot.Flow!.WorldBanner, Is.EqualTo(4));
            Assert.That(banner.Text, Is.EqualTo("Mundo 4: Trapecio · Colombia · Amazonas / Putumayo"));
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(boot.Flow.WorldBannerClosing, Is.True);
            Assert.That(boot.Flow.Selected, Is.EqualTo(TapController.NoSelection));
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(banner.Dismissing, Is.True);
            Assert.That(boot.View.LastWorldBannerClosedProbe, Does.Contain("\"level\":81").And.Contain("\"next\":\"intro\""));
            PlayModeHelpers.Step(boot, 400);
            Assert.That(banner.Visible, Is.False);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(boot.Flow.Selected, Is.EqualTo(TapController.NoSelection));
        }

        [UnityTest]
        public IEnumerator Banner_NotOnRetryNorResume()
        {
            // «Solo en el primer intento»: ni Volver a la orilla ni reabrir la app con el nivel en curso lo repiten (02 §14).
            var store = new MemoryProfileStore(FunnelSetup.Profile(1240));
            LaunchOptions.ProfileStoreOverride = store;
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtSyncOverride = true;
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            PlayModeHelpers.Step(boot, 300);
            WorldBannerView banner = boot.View!.WorldBannerView;
            Assert.That(banner.Visible, Is.True);
            yield return null; // un fotograma de verdad: el raycast no ve un velo que el lienzo aún no ha pintado
            WorldBannerSetup.Tap(banner);
            Assert.That(boot.TapAnywhere(), Is.True, "el segundo toque termina la salida");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(boot.TapAnywhere(), Is.True, "y el tercero salta la intro");
            CardSetup.StepUntilPlaying(boot);
            SolveResult solution = new AStarSolver().Solve(boot.Flow!.Session.Current, SolverOptions.ForPipeline(boot.Flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            Pour move = solution.GetMove(0);
            boot.TapCuia(move.Source);
            Assert.That(boot.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 4000);

            // Volver a la orilla: intro corta, sin banner.
            Assert.That(boot.PressRestart(), Is.True);
            Assert.That(boot.Flow.Phase, Is.EqualTo(FlowPhase.Intro));
            PlayModeHelpers.Step(boot, 50);
            Assert.That(boot.Flow.WorldBanner, Is.EqualTo(0));
            Assert.That(banner.Visible, Is.False);
            Assert.That(boot.View.WorldBannersShown, Is.EqualTo(1));
            Assert.That(boot.TapAnywhere(), Is.True);
            CardSetup.StepUntilPlaying(boot);
            boot.TapCuia(move.Source);
            Assert.That(boot.TapCuia(move.Target).Kind, Is.EqualTo(TapKind.Poured));
            PlayModeHelpers.StepUntilIdle(boot, 4000);

            // Reabrir la app con el nivel en curso: Playing, sin banner ni intro.
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap again = GameBootstrap.Current!;
            Assert.That(again, Is.Not.SameAs(boot));
            PlayModeHelpers.TakeClock(again);
            PlayModeHelpers.Step(again, 300);
            Assert.That(again.App!.Screen, Is.EqualTo(AppScreen.Level));
            Assert.That(again.Flow!.LevelNumber, Is.EqualTo(41));
            Assert.That(again.Flow.Phase, Is.EqualTo(FlowPhase.Playing), "sin intro ni banner (02 §14)");
            Assert.That(again.Flow.WorldBanner, Is.EqualTo(0));
            Assert.That(again.View!.WorldBannerView.Visible, Is.False);
            Assert.That(again.View.WorldBannersShown, Is.EqualTo(0));
            Assert.That(again.View.CardArt.MascotsBuilt, Is.False, "sin banner, ninguna textura de las mascotas (ni al arrancar)");
        }

        [UnityTest]
        public IEnumerator Banner_ThenTheLevelCard_ThenTheIntro_AndTheGiftWaits()
        {
            // Con af_level_offset = 41, el 141 (primero del mundo 5) se juega con el tablero del 100: Muy difícil, estreno de Aguas
            // turbias y Luciérnaga regalada al cargar (04 §2.5). Orden: el banner, la tarjeta combinada (con «+1 Luciérnaga») y la
            // intro; el aviso del regalo no sale encima de ninguno de los dos.
            LaunchOptions.LevelOffsetOverride = 41;
            yield return CardSetup.Load(141);
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(flow.Level.IntroMechanic, Is.EqualTo(LevelFlow.MurkyIntroMechanic), "el tablero del 100");
            Assert.That(boot.Wallet!.Stock.Get(BoosterKind.Reveal), Is.EqualTo(1), "la Luciérnaga ya se regaló al cargar");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(flow.WorldBanner, Is.EqualTo(5));
            Assert.That(flow.LevelCard, Is.Null, "primero el banner");
            Assert.That(banner.Visible, Is.True);
            Assert.That(banner.Text, Is.EqualTo("Mundo 5: Loreto · Perú · Marañón-Ucayali"));
            Assert.That(view.LevelCard.Visible, Is.False);
            Assert.That(view.LastWorldBannerProbe, Does.Contain("\"then\":\"level_card\""), "la tarjeta previa va detrás");
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(view.ToastText, Is.Null, "el aviso del regalo espera al banner");

            // 1 toque: primero sale el banner, sin la tarjeta previa encima (dos velos); al terminar su salida, la tarjeta. La intro
            // sigue esperando.
            yield return null; // un fotograma de verdad: el raycast no ve un velo que el lienzo aún no ha pintado
            WorldBannerSetup.Tap(banner);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(flow.WorldBannerClosing, Is.True);
            Assert.That(flow.LevelCard, Is.Null, "la tarjeta, después del banner");
            Assert.That(view.LastWorldBannerClosedProbe, Does.Contain("\"next\":\"level_card\""), "el mismo vocabulario que then");
            int frames = 0;
            while (banner.Visible)
            {
                Assert.That(view.LevelCard.Visible, Is.False, "nunca dos velos a la vez");
                Assert.That(view.Intro.TimeMs, Is.EqualTo(0f));
                Assert.That(view.ToastText, Is.Null);
                PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
                Assert.That(++frames, Is.LessThanOrEqualTo(Mathf.CeilToInt(WorldBannerView.ExitMs / PlayModeHelpers.FrameMs)), "la salida dura " + WorldBannerView.ExitMs + " ms");
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(flow.WorldBanner, Is.EqualTo(0));
            Assert.That(flow.LevelCard, Is.Not.Null);
            Assert.That(flow.LevelCard!.Kind, Is.EqualTo(LevelCardKind.Combined));
            Assert.That(flow.LevelCard.RevealGifted, Is.True);
            Assert.That(view.LevelCard.Visible, Is.True);
            Assert.That(view.LevelCard.GiftText, Is.EqualTo("+1 Luciérnaga"));
            Assert.That(view.LevelCard.Root.GetSiblingIndex(), Is.GreaterThan(banner.Root.GetSiblingIndex()), "la tarjeta, por encima");
            Assert.That(view.CardIntroHeld, Is.True);
            PlayModeHelpers.Step(boot, 400);
            Assert.That(banner.Visible, Is.False);
            Assert.That(view.ToastText, Is.Null, "ni encima de la tarjeta");

            // Otro toque: la intro. El aviso esperaba, no se perdió: sale después de la tarjeta (al empezar la intro o, con la cola
            // de avisos de A2, al terminarla), así que se busca en cada fotograma hasta PLAYING y un poco más.
            yield return null; // un fotograma de verdad: el raycast no ve un velo que el lienzo aún no ha pintado
            CardSetup.TapCard(view.LevelCard);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            bool giftShown = view.ToastText == UiText.RevealGiftToast;
            int playingMs = -1;
            for (int ms = 0; ms < 12000 && (playingMs < 0 || ms - playingMs < 1500) && !(giftShown && playingMs >= 0); ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                if (view.ToastText == UiText.RevealGiftToast)
                {
                    giftShown = true;
                }

                if (playingMs < 0 && flow.Phase == FlowPhase.Playing && view.IsViewIdle)
                {
                    playingMs = ms;
                }
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Playing));
            Assert.That(view.RevealGiftPending, Is.False, "el regalo ya no espera");
            Assert.That(giftShown, Is.True, "«+1 Luciérnaga» sale tras el banner y la tarjeta");
        }

        [UnityTest]
        public IEnumerator Banner_ReduceMotion_Still()
        {
            // 07 §7.8: con Reducir movimiento, solo el fundido: Capi ya presenta y Rosa ya está fuera, sin soplo.
            yield return CardSetup.Load(140);
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            flow.ReduceMotion = true;
            flow.Load(141);
            WorldBannerView banner = boot.View!.WorldBannerView;
            Assert.That(flow.WorldBanner, Is.EqualTo(5));
            PlayModeHelpers.TickExact(boot, PlayModeHelpers.FrameMs);
            Assert.That(banner.PawAngle, Is.EqualTo(WorldBannerView.PawUpDeg).Within(0.5f));
            Assert.That(banner.RosaRise, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(banner.Pop.localScale.x, Is.EqualTo(1f), "sin rebote");
            yield return null; // un fotograma de verdad: el raycast no ve un velo que el lienzo aún no ha pintado
            WorldBannerSetup.Tap(banner);
            Assert.That(flow.WorldBannerExitMs, Is.EqualTo(Mathf.CeilToInt(WorldBannerView.ExitReducedMs)), "el flujo espera el fundido corto");
            PlayModeHelpers.Step(boot, (int)WorldBannerView.ExitReducedMs + 20);
            Assert.That(banner.Visible, Is.False, "sale con el fundido corto");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
        }

        [UnityTest]
        public IEnumerator Banner_ArtOffTheMainThread_VeilAtOnce_TapsIgnoredUntilTheCardEnters()
        {
            // Como en el juego (sin BannerArtSyncOverride): abrir el banner no pinta su arte en el fotograma de la carga (08 §5.4).
            // Mientras se pinta en otro hilo, el velo sale en el acto, la tarjeta aún no y ningún toque lo cierra (ni el velo, ni
            // TapAnywhere, ni una cuia): no se puede saltar sin verse (revisión 3 de A4, punto 2). Cuando el arte llega, la tarjeta
            // entra desde t = 0 y un toque lo cierra como siempre. El tiempo límite se sube para no depender de la máquina (el
            // respaldo tiene sus propios tests).
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtTimeoutMsOverride = 60000f;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(42, new MemoryProgressStore(42, 41));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(flow.WorldBanner, Is.EqualTo(0), "el 42 no lo lleva");
            view.CardArt.ReleaseBannerArt(); // el arte es de toda la sesión: nada pedido de antes

            var sinceLoad = Stopwatch.StartNew(); // reloj real del test desde antes de la petición (para wait_ms)
            flow.Load(41);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(flow.WorldBanner, Is.EqualTo(3));
            Assert.That(banner.Visible && banner.AsyncArt, Is.True);
            Assert.That(banner.ArtReady, Is.False, "el arte se pinta en otro hilo");
            Assert.That(banner.Waiting && !banner.CardShown, Is.True);
            Assert.That(banner.VeilAlpha, Is.EqualTo(1f), "el velo, en el acto");
            Assert.That(banner.Dim.gameObject.activeInHierarchy, Is.True);
            Assert.That(banner.Pop.gameObject.activeSelf, Is.False, "la tarjeta, cuando llegue el arte");
            Assert.That(banner.Alpha, Is.EqualTo(0f), "sin tarjeta a la vista");
            Assert.That(flow.WorldBannerWaiting, Is.True, "ningún toque lo cierra");
            Assert.That(view.CardArt.BannerArtPending, Is.True);
            Assert.That(view.CardArt.MascotsBuilt, Is.False);
            Assert.That(view.CardArt.MascotsMainMs, Is.EqualTo(0d), "nada pintado en el hilo principal");
            Assert.That(view.CardIntroHeld, Is.True);
            Assert.That(view.LastWorldBannerProbe, Does.Contain("\"level\":41"));
            Assert.That(view.LastWorldBannerArtProbe, Is.Null, "world_banner_art, cuando entre la tarjeta");
            Assert.That(banner.RaycastTargets(), Is.EqualTo(new Graphic[] { banner.Dim }));

            // Mientras espera, ningún toque lo cierra: ni el velo, ni TapAnywhere, ni una cuia.
            yield return null; // un fotograma de verdad para el raycast (con el reloj quitado, el banner no avanza)
            Assert.That(banner.Waiting, Is.True);
            WorldBannerSetup.Tap(banner);
            Assert.That(boot.TapAnywhere(), Is.False, "TapAnywhere tampoco");
            Assert.That(boot.TapCuia(0).Kind, Is.EqualTo(TapKind.Ignored));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(flow.WorldBanner, Is.EqualTo(3));
            Assert.That(flow.WorldBannerClosing, Is.False, "ningún toque empezó la salida");
            Assert.That(banner.Visible && !banner.Dismissing, Is.True);
            Assert.That(view.LastWorldBannerClosedProbe, Is.Null);

            // El arte llega: la tarjeta entra desde t = 0 y, desde ahí, un toque lo cierra.
            var watch = Stopwatch.StartNew();
            while (!banner.CardShown && watch.ElapsedMilliseconds < 20000)
            {
                yield return null; // tiempo de verdad para el hilo que pinta
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            double loadToCardMs = sinceLoad.Elapsed.TotalMilliseconds;
            Assert.That(banner.ArtReady, Is.True, "el arte llega");
            Assert.That(banner.Fallback, Is.EqualTo(WorldBannerView.FallbackNone));
            Assert.That(banner.ArtWaitMs, Is.GreaterThan(0f).And.LessThanOrEqualTo((float)loadToCardMs + 1f),
                "wait_ms: como mucho el reloj real que midió el test entre la petición y la tarjeta (" + loadToCardMs.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ms)");
            Assert.That(banner.Pop.gameObject.activeSelf, Is.True);
            Assert.That(flow.WorldBannerWaiting, Is.False);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(view.Intro.TimeMs, Is.EqualTo(0f));
            Assert.That(view.CardArt.MascotsBuilt && !view.CardArt.BannerArtPending, Is.True);
            Assert.That(view.CardArt.MascotsMs, Is.GreaterThan(0d));
            Assert.That(view.CardArt.MascotsMainMs, Is.LessThan(view.CardArt.MascotsMs), "en el hilo principal, solo las texturas");
            Assert.That(view.LastWorldBannerArtProbe, Does.Contain("\"level\":41").And.Contain("\"async\":true").And.Contain("\"fallback\":\"none\""));
            Assert.That(banner.SceneSprite, Is.Not.Null);
            Assert.That(banner.FlatWater.gameObject.activeSelf, Is.False);
            Assert.That(banner.Rosa.sprite, Is.Not.Null);
            Assert.That(banner.Paw.sprite, Is.Not.Null);
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(banner.Alpha, Is.EqualTo(1f));
            Assert.That(banner.VeilAlpha, Is.EqualTo(1f));
            Assert.That(banner.PawAngle, Is.EqualTo(WorldBannerView.PawUpDeg).Within(0.5f));
            Assert.That(banner.Settled, Is.True);
            WorldBannerSetup.AssertLaidOut(view);

            yield return null; // un fotograma de verdad: el raycast no ve un velo que el lienzo aún no ha pintado
            WorldBannerSetup.Tap(banner);
            Assert.That(flow.WorldBannerClosing && banner.Dismissing, Is.True, "ya se ve: un toque empieza la salida");
            Assert.That(view.LastWorldBannerClosedProbe, Is.EqualTo("{\"level\":41,\"world\":3,\"next\":\"intro\"}"));
            PlayModeHelpers.Step(boot, 400);
            Assert.That(banner.Visible, Is.False);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro));
            Assert.That(view.CardArt.MascotsBuilt, Is.False);
            CardSetup.StepUntilPlaying(boot);
        }

        [UnityTest]
        public IEnumerator Banner_SurfaceOrigin_IsWhereTheArtJobRan_MainThreadInTheAct_OtherThreadInThePool([Values(true, false)] bool sync)
        {
            // Revisión de T52 (U1): el mismo trabajo del arte pinta la superficie de madera de la tarjeta en los dos caminos, y su
            // origen dice dónde corrió. En el acto (BannerArtSyncOverride, los tests que miden instantes exactos) corre en el hilo
            // principal: MainThread (antes decía OtherThread, «la pintó otro hilo»). Como en el juego, en un hilo del grupo:
            // OtherThread. La superficie se saca antes de la caché del arte compartido, así que no depende de otros tests.
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtTimeoutMsOverride = 60000f;
            if (sync)
            {
                LaunchOptions.BannerArtSyncOverride = true;
            }

            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(42, new MemoryProgressStore(42, 41));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            ProceduralArt art = view.CardArt;
            WorldBannerView banner = view.WorldBannerView;
            art.ReleaseBannerArt(); // el arte es de toda la sesión: nada pedido de antes
            var surface = new Vector2(LevelCardView.MaxWidthDp - 16f, 281f); // la compartida de 281 dp, a 360 dp de ancho
            art.ForgetModalSurface(surface.x, surface.y);
            Assert.That(art.ModalSurfaceOriginOf(surface.x, surface.y), Is.EqualTo(ModalSurfaceOrigin.None), "condición del test");

            flow.Load(41);
            Assert.That(flow.WorldBanner, Is.EqualTo(3));
            Assert.That(banner.AsyncArt, Is.EqualTo(!sync));
            if (sync)
            {
                Assert.That(banner.ArtReady, Is.True, "en el acto");
                Assert.That(art.MascotsMainMs, Is.GreaterThanOrEqualTo(art.MascotsMs), "todo en el hilo principal");
            }
            else
            {
                Assert.That(banner.ArtReady, Is.False, "se pinta en otro hilo");
                var watch = Stopwatch.StartNew();
                while (!banner.CardShown && watch.ElapsedMilliseconds < 20000)
                {
                    yield return null; // tiempo de verdad para el hilo que pinta
                    PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                }

                Assert.That(art.MascotsMainMs, Is.LessThan(art.MascotsMs), "en el hilo principal, solo las texturas");
            }

            Assert.That(banner.ArtReady, Is.True);
            Assert.That(banner.Fallback, Is.EqualTo(WorldBannerView.FallbackNone));
            Assert.That(banner.SurfaceSizeDp, Is.EqualTo(surface), "la superficie compartida de 281 dp");
            Assert.That(art.HasModalSurface(surface.x, surface.y), Is.True, "la pintó el trabajo del arte");
            ModalSurfaceOrigin expected = sync ? ModalSurfaceOrigin.MainThread : ModalSurfaceOrigin.OtherThread;
            Assert.That(art.ModalSurfaceOriginOf(surface.x, surface.y), Is.EqualTo(expected), sync ? "en el acto, en el hilo principal" : "en un hilo del grupo");
            Assert.That(banner.Surface.sprite, Is.SameAs(art.ModalSurfaceFor(surface.x, surface.y)), "la tarjeta usa la de la caché");
            Assert.That(art.ModalSurfaceOriginOf(surface.x, surface.y), Is.EqualTo(expected), "pedirla otra vez no cambia su origen");
            Assert.That(view.LastWorldBannerArtProbe, Does.Contain("\"async\":" + (sync ? "false" : "true")).And.Contain("\"fallback\":\"none\""));
            yield return WorldBannerSetup.CloseThenTheIntro(boot, 41, 3);
        }

        [UnityTest]
        public IEnumerator Banner_ArtFails_EntersWithoutIt_ATapClosesIt_AndTheIntroFollows()
        {
            // Revisión 3 de A4, punto 1: si el trabajo del arte falla en su hilo, o si el grupo de hilos no lo acepta (devuelve false
            // o lanza NotSupportedException), nada lanza en el hilo principal (un solo aviso en el log) y el banner entra sin arte:
            // la tarjeta, la placa y el texto de D-012, con la viñeta del color plano del agua y sin Rosa ni Capi. Un toque lo
            // cierra y la intro sigue.
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtFaultOverride = BannerArtFault.Throw;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(42, new MemoryProgressStore(42, 41));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = view.WorldBannerView;
            view.CardArt.ReleaseBannerArt();

            flow.Load(41);
            var watch = Stopwatch.StartNew();
            while (!banner.CardShown && watch.ElapsedMilliseconds < 10000)
            {
                yield return null;
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            WorldBannerSetup.AssertFallback(view, WorldBannerView.FallbackError);
            Assert.That(ArtWarnings("falló al pintarse"), Is.EqualTo(1), "un solo aviso");
            yield return WorldBannerSetup.CloseThenTheIntro(boot, 41, 3);

            // El grupo de hilos lo rechaza: el respaldo en el acto (no se pinta en el hilo principal).
            LaunchOptions.BannerArtFaultOverride = BannerArtFault.QueueRejected;
            flow.Load(81);
            Assert.That(banner.CardShown, Is.True, "sin cola, en el acto");
            Assert.That(view.CardArt.MascotsMainMs, Is.EqualTo(0d), "nada pintado en el hilo principal");
            WorldBannerSetup.AssertFallback(view, WorldBannerView.FallbackQueue);
            yield return WorldBannerSetup.CloseThenTheIntro(boot, 81, 4);

            // Sin grupo de hilos (NotSupportedException al encolar): igual.
            LaunchOptions.BannerArtFaultOverride = BannerArtFault.QueueNotSupported;
            flow.Load(141);
            Assert.That(banner.CardShown, Is.True);
            WorldBannerSetup.AssertFallback(view, WorldBannerView.FallbackQueue);
            yield return WorldBannerSetup.CloseThenTheIntro(boot, 141, 5);
            Assert.That(ArtWarnings("no se pudo encolar"), Is.EqualTo(2), "un aviso por banner");
        }

        [UnityTest]
        public IEnumerator Banner_ArtTimesOut_EntersWithoutIt_AndTheLateArtIsDiscarded()
        {
            // Revisión 3 de A4, puntos 1b y 5: si el arte no llega en el tiempo límite (aquí 150 ms inyectados; en el juego
            // WorldBannerView.ArtTimeoutMs = 800 ms [P], de reloj real desde la petición), el banner entra sin él, el trabajo se
            // cancela y lo que llegue después se descarta. Mientras espera, el AF_LAYOUT del nivel no publica world_banner; con la
            // tarjeta a la vista, sí.
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtFaultOverride = BannerArtFault.Stall; // un trabajo que no acaba hasta que lo cancelan
            LaunchOptions.BannerArtTimeoutMsOverride = 150f;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(42, new MemoryProgressStore(42, 41));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = view.WorldBannerView;
            view.CardArt.ReleaseBannerArt();

            var sinceLoad = Stopwatch.StartNew(); // reloj real del test desde antes de la petición (para wait_ms)
            flow.Load(41);
            Assert.That(banner.Waiting, Is.True);
            Assert.That(banner.ArtTimeoutLimitMs, Is.EqualTo(150f), "el umbral inyectado");
            Assert.That(flow.WorldBannerWaiting, Is.True);

            // Otra pantalla del mismo ancho (otra muesca) vuelve a colocarlo todo y publica el AF_LAYOUT del nivel: sin el banner.
            GameScreen.SimulatedScreen = HudLabelTests.Screen780Cutout;
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            string? waiting = LastLevelLayout(41);
            Assert.That(waiting, Is.Not.Null, "AF_LAYOUT level al cambiar la pantalla");
            Assert.That(waiting, Does.Not.Contain("\"id\":\"world_banner\""), "mientras espera, solo el velo");
            Assert.That(HasLine("AF_LAYOUT {\"screen\":\"world_banner\""), Is.False);

            var watch = Stopwatch.StartNew();
            while (!banner.CardShown && watch.ElapsedMilliseconds < 10000)
            {
                yield return null;
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            double loadToCardMs = sinceLoad.Elapsed.TotalMilliseconds;
            WorldBannerSetup.AssertFallback(view, WorldBannerView.FallbackTimeout);
            Assert.That(banner.ArtWaitMs, Is.GreaterThanOrEqualTo(150f).And.LessThanOrEqualTo((float)loadToCardMs + 1f),
                "wait_ms: al menos el límite y como mucho el reloj real que midió el test (" + loadToCardMs.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                + " ms); que no es la suma del dt lo prueba Banner_TimeoutAndWaitMs_UseTheRealClock");
            Assert.That(view.CardArt.BannerArtPending, Is.False, "el trabajo se abandona");
            Assert.That(view.CardArt.BannerArtFailure, Is.EqualTo(BannerArtFailure.None), "no es un fallo del trabajo");
            watch.Restart();
            while (ProceduralArt.BannerArtJobsRunning > 0 && watch.ElapsedMilliseconds < 5000)
            {
                yield return null;
            }

            Assert.That(ProceduralArt.BannerArtJobsRunning, Is.EqualTo(0), "cancelado, el trabajo deja de gastar CPU");
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(HasLine("AF_LAYOUT {\"screen\":\"world_banner\",\"level\":41,"), Is.True, "al terminar la entrada");
            GameScreen.SimulatedScreen = HudLabelTests.Screen780;
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(LastLevelLayout(41), Does.Contain("\"id\":\"world_banner\""), "con la tarjeta a la vista");
            yield return WorldBannerSetup.CloseThenTheIntro(boot, 41, 3);

            // Un arte de verdad que tarda más que el límite (1 ms: ningún trabajo pinta en tan poco): entra sin él y, aunque el
            // otro hilo siguiera, nada de lo que pinte se pone.
            LaunchOptions.BannerArtFaultOverride = null;
            LaunchOptions.BannerArtTimeoutMsOverride = 1f;
            flow.Load(81);
            yield return null;
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            WorldBannerSetup.AssertFallback(view, WorldBannerView.FallbackTimeout);
            watch.Restart();
            while (watch.ElapsedMilliseconds < 1000)
            {
                yield return null;
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(banner.Fallback, Is.EqualTo(WorldBannerView.FallbackTimeout), "el arte que llegue tarde se descarta");
            Assert.That(banner.ArtReady, Is.False);
            Assert.That(banner.SceneSprite, Is.Null);
            Assert.That(banner.Rosa.gameObject.activeInHierarchy, Is.False);
            Assert.That(view.CardArt.MascotsBuilt, Is.False);
            Assert.That(view.CardArt.BannerArtPending, Is.False);
            yield return WorldBannerSetup.CloseThenTheIntro(boot, 81, 4);
        }

        [UnityTest]
        public IEnumerator Banner_TimeoutAndWaitMs_UseTheRealClock()
        {
            // Revisión 3 de A4, puntos 1b y 4: el tiempo límite del arte (WorldBannerView.ArtTimeoutMs = 800 ms [P]) y el wait_ms
            // de AF_STATE world_banner_art son de reloj real desde la petición (Stopwatch), no la suma del dt de los fotogramas,
            // que en el juego llega recortado. Con un trabajo que no acaba hasta que lo cancelan y un límite de 2000 ms: 8 s de dt
            // en unos pocos ms reales no lo cumplen; esperar el límite en reloj real sin avanzar el dt, sí, y wait_ms es ese
            // tiempo real (≈ 2 s), no los ≈ 8 s de dt.
            const float limitMs = 2000f;
            const int dtMs = 8000;
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtFaultOverride = BannerArtFault.Stall;
            LaunchOptions.BannerArtTimeoutMsOverride = limitMs;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(42, new MemoryProgressStore(42, 41));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = view.WorldBannerView;
            view.CardArt.ReleaseBannerArt();

            var sinceLoad = Stopwatch.StartNew(); // reloj real del test desde antes de la petición
            flow.Load(41);
            Assert.That(flow.WorldBanner, Is.EqualTo(3));
            Assert.That(banner.Waiting && flow.WorldBannerWaiting, Is.True);
            Assert.That(banner.ArtTimeoutLimitMs, Is.EqualTo(limitMs), "el umbral inyectado");

            // (1) 8 s de dt sin esperar nada: pasan en unos pocos ms reales, así que el límite no se cumple. Si contara el dt,
            // aquí ya habría entrado el respaldo.
            PlayModeHelpers.Step(boot, dtMs);
            double stepRealMs = sinceLoad.Elapsed.TotalMilliseconds;
            Assert.That(stepRealMs, Is.LessThan(limitMs),
                "condición del test: la carga y los " + dtMs + " ms de dt tardan menos que el límite en reloj real (tardaron "
                + stepRealMs.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ms)");
            Assert.That(banner.Waiting, Is.True, "el dt no cuenta: sigue esperando el arte");
            Assert.That(flow.WorldBannerWaiting, Is.True, "y ningún toque lo cierra");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(banner.Fallback, Is.EqualTo(WorldBannerView.FallbackNone));
            Assert.That(banner.ArtWaitMs, Is.EqualTo(0f), "wait_ms, cuando entre la tarjeta");
            Assert.That(view.LastWorldBannerArtProbe, Is.Null);
            Assert.That(view.CardArt.BannerArtPending, Is.True, "el trabajo sigue");

            // (2) El límite en reloj real sin avanzar el dt (sin reloj, nadie llama al Tick del banner) y un solo fotograma de 16 ms:
            // entra el respaldo, y wait_ms es el tiempo real desde la petición, no los 8 s de dt.
            while (sinceLoad.ElapsedMilliseconds < (long)limitMs + 50)
            {
                yield return null;
            }

            Assert.That(banner.Waiting, Is.True, "sin Tick, nada cambia");
            PlayModeHelpers.Step(boot, 1);
            double loadToCardMs = sinceLoad.Elapsed.TotalMilliseconds;
            WorldBannerSetup.AssertFallback(view, WorldBannerView.FallbackTimeout);
            Assert.That(banner.ArtWaitMs, Is.InRange(limitMs, (float)loadToCardMs + 1f),
                "wait_ms: entre el límite y el reloj real que midió el test (" + loadToCardMs.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ms)");
            Assert.That(banner.ArtWaitMs, Is.LessThan(dtMs - limitMs), "no la suma del dt (" + (dtMs + PlayModeHelpers.FrameMs) + " ms)");
            Assert.That(view.LastWorldBannerArtProbe, Does.Contain("\"wait_ms\":" + banner.ArtWaitMs.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + ","),
                "la sonda publica ese mismo tiempo real");
            Assert.That(view.CardArt.BannerArtPending, Is.False, "el trabajo se abandona");
            yield return WorldBannerSetup.CloseThenTheIntro(boot, 41, 3);
        }

        [UnityTest]
        public IEnumerator Banner_NoAllocWhileWaitingForTheArt()
        {
            // 08 §5.4 (revisión 3 de A4, punto 6): mientras espera el arte (aquí, un trabajo que no acaba hasta que lo cancelan),
            // el Tick del banner y el de la pantalla y el flujo solo miran si llegó y el reloj real: sin asignaciones. Después,
            // con el límite cumplido, entra sin arte y un toque lo cierra.
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtFaultOverride = BannerArtFault.Stall;
            LaunchOptions.BannerArtTimeoutMsOverride = 60000f;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(FunnelSetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(42, new MemoryProgressStore(42, 41));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = view.WorldBannerView;
            view.CardArt.ReleaseBannerArt();
            flow.Load(41);
            Assert.That(banner.Waiting, Is.True);

            // Calentamiento: lo que asigna la pantalla en los primeros fotogramas tras cargar cualquier nivel (también el 43, con su
            // tarjeta previa y sin banner) no es de la espera.
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(banner.Waiting, Is.True, "el trabajo no acaba: sigue esperando");
            void Banner20()
            {
                for (int i = 0; i < 20; i++)
                {
                    banner.Tick(PlayModeHelpers.FrameMs);
                }
            }

            void Screen20()
            {
                for (int i = 0; i < 20; i++)
                {
                    view.Tick(PlayModeHelpers.FrameMs);
                    flow.Tick(PlayModeHelpers.FrameMs, view.IsViewIdle);
                }
            }

            Assert.That(Banner20, Is.Not.AllocatingGCMemory(), "Tick del banner esperando el arte");
            Assert.That(Banner20, Is.Not.AllocatingGCMemory(), "Tick del banner esperando el arte");
            Assert.That(Screen20, Is.Not.AllocatingGCMemory(), "Tick de la pantalla y del flujo esperando el arte");
            Assert.That(banner.Waiting && flow.WorldBannerWaiting, Is.True, "sigue esperando");

            banner.ArtTimeoutLimitMs = 0f; // se cumple el límite
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            WorldBannerSetup.AssertFallback(view, WorldBannerView.FallbackTimeout);
            yield return WorldBannerSetup.CloseThenTheIntro(boot, 41, 3);
        }

        [UnityTest]
        public IEnumerator Banner_ArtRequestedAtTheVictoryOf10_EntersAtOnceOn11()
        {
            // Revisión 3 de A4, punto 2: en la victoria del 10 (y del 40, el 80 y el 140) el arte del banner del nivel siguiente se
            // pide ya, en otro hilo; tras «Siguiente», el banner del 11 entra en el acto con su arte, sin esperar.
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtTimeoutMsOverride = 60000f;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(1240));
            yield return PlayModeHelpers.LoadGame(10, new MemoryProgressStore(10, 9));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = view.WorldBannerView;
            view.CardArt.ReleaseBannerArt();
            if (flow.Phase == FlowPhase.LevelCard)
            {
                Assert.That(boot.TapAnywhere(), Is.True, "la tarjeta previa del 10, si la hay");
            }

            Assert.That(view.CardArt.BannerArtPending, Is.False, "jugando el 10, nada pedido");
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(view.CardArt.BannerArtPending, Is.True, "en la victoria del 10, el arte del banner del 11, pedido");
            Assert.That(banner.Visible, Is.False);
            Assert.That(view.CardArt.MascotsBuilt, Is.False, "sin texturas hasta abrirlo");
            Assert.That(view.CardArt.WaitForBannerArt(30000), Is.True, "pintado en otro hilo durante la victoria");

            Assert.That(boot.PressNext(), Is.True);
            for (int ms = 0; ms < 5000 && flow.LevelNumber != 11; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.LevelNumber, Is.EqualTo(11));
            Assert.That(flow.WorldBanner, Is.EqualTo(2));
            Assert.That(banner.AsyncArt, Is.True);
            Assert.That(banner.CardShown && banner.ArtReady, Is.True, "entra en el acto, con su arte");
            Assert.That(banner.Fallback, Is.EqualTo(WorldBannerView.FallbackNone));
            Assert.That(flow.WorldBannerWaiting, Is.False);
            Assert.That(view.CardArt.MascotsMainMs, Is.LessThan(view.CardArt.MascotsMs), "en el hilo principal, solo las texturas");
            Assert.That(view.LastWorldBannerArtProbe, Does.Contain("\"level\":11").And.Contain("\"async\":true").And.Contain("\"fallback\":\"none\""));
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(banner.Alpha, Is.EqualTo(1f));
            yield return WorldBannerSetup.CloseThenTheIntro(boot, 11, 2);
        }

        [UnityTest]
        public IEnumerator Banner_ArtRequestedWhenPlayOffersAWorldLevel()
        {
            // Revisión 3 de A4, punto 2: el inicio ofrece «Jugar · Nivel 81» (primer intento del primero del mundo 4): el arte de su
            // banner se pide ya, en otro hilo, y al entrar el banner sale en el acto con su arte.
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtTimeoutMsOverride = 60000f;
            Profile profile = FunnelSetup.Profile(1240);
            profile.Progress.NextLevel = 81;
            profile.Progress.MaxLevel = 80;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.App.PlayLevel, Is.EqualTo(81));
            GameScreen view = boot.View!;
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(view.CardArt.BannerArtPending, Is.True, "«Jugar · Nivel 81»: el arte del banner del mundo 4, pedido");
            Assert.That(banner.Visible, Is.False);
            Assert.That(view.CardArt.WaitForBannerArt(30000), Is.True);

            Assert.That(boot.PressPlay(), Is.True);
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.LevelNumber, Is.EqualTo(81));
            Assert.That(flow.WorldBanner, Is.EqualTo(4));
            Assert.That(banner.CardShown && banner.ArtReady, Is.True, "entra en el acto, con su arte");
            Assert.That(flow.WorldBannerWaiting, Is.False);
            Assert.That(view.LastWorldBannerArtProbe, Does.Contain("\"level\":81").And.Contain("\"fallback\":\"none\""));
            PlayModeHelpers.Step(boot, 1500);
            yield return WorldBannerSetup.CloseThenTheIntro(boot, 81, 4);
        }

        [UnityTest]
        public IEnumerator Banner_NoAllocWhileAnimating()
        {
            // 08 §5.4: la entrada (Rosa, el soplo, la pata) y la salida no asignan memoria por fotograma.
            yield return CardSetup.Load(11);
            GameBootstrap boot = GameBootstrap.Current!;
            WorldBannerView banner = boot.View!.WorldBannerView;
            Assert.That(banner.Visible, Is.True);
            banner.Show(2, Palette.WaterClear, false, boot.View.CanvasWidth, boot.View.CardCanvasHeight, boot.View.SafeTopDp, boot.View.CardSafeBottom);
            banner.Tick(PlayModeHelpers.FrameMs);
            void Run20()
            {
                for (int i = 0; i < 20; i++)
                {
                    banner.Tick(PlayModeHelpers.FrameMs);
                }
            }

            Assert.That(Run20, Is.Not.AllocatingGCMemory(), "entrada");
            Assert.That(Run20, Is.Not.AllocatingGCMemory(), "entrada");
            Assert.That(Run20, Is.Not.AllocatingGCMemory(), "entrada y vaivén");
            banner.Dismiss();
            void Exit()
            {
                for (int i = 0; i < 10; i++)
                {
                    banner.Tick(PlayModeHelpers.FrameMs);
                }
            }

            Assert.That(Exit, Is.Not.AllocatingGCMemory(), "salida");
        }

        // Las líneas de la sonda (AF_STATE y AF_LAYOUT) y los avisos del arte, en orden.
        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (condition.StartsWith("AF_", StringComparison.Ordinal) || condition.StartsWith("[AF.ProceduralArt]", StringComparison.Ordinal))
            {
                _lines.Add(condition);
            }
        }

        private bool HasLine(string prefix)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // La última línea AF_LAYOUT del tablero del nivel (null si no hay).
        private string? LastLevelLayout(int level)
        {
            string tag = "AF_LAYOUT {\"screen\":\"level\",\"level\":" + level.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",";
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                if (_lines[i].StartsWith(tag, StringComparison.Ordinal))
                {
                    return _lines[i];
                }
            }

            return null;
        }

        // Avisos del arte del banner (un Debug.LogWarning por fallo) que contienen fragment.
        private int ArtWarnings(string fragment)
        {
            int n = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].StartsWith("[AF.ProceduralArt] El arte del banner de mundo", StringComparison.Ordinal) && _lines[i].Contains(fragment))
                {
                    n++;
                }
            }

            return n;
        }
    }

    /// <summary>Toque y comprobaciones comunes del banner de mundo (tarea A4).</summary>
    internal static class WorldBannerSetup
    {
        /// <summary>
        /// El banner abierto entró sin arte por <paramref name="reason"/> (el respaldo de D-012): la tarjeta, la placa y el texto
        /// entero, con la viñeta del color plano del agua del nivel y sin Rosa ni Capi; ya a la vista, así que un toque lo cierra;
        /// y la sonda <c>world_banner_art</c> lo dice.
        /// </summary>
        public static void AssertFallback(GameScreen view, string reason)
        {
            WorldBannerView banner = view.WorldBannerView;
            LevelFlow flow = GameBootstrap.Current!.Flow!;
            Assert.That(banner.Visible && banner.CardShown, Is.True, reason + ": la tarjeta, a la vista");
            Assert.That(banner.ArtReady, Is.False, reason);
            Assert.That(banner.Fallback, Is.EqualTo(reason));
            Assert.That(flow.WorldBannerWaiting, Is.False, reason + ": ya se ve, un toque lo cierra");
            Assert.That(banner.Pop.gameObject.activeSelf, Is.True, reason);
            Assert.That(banner.Text, Is.EqualTo(UiText.WorldBanner(flow.WorldBanner)), reason + ": el texto de D-012, entero");
            Assert.That(banner.TitleText, Does.StartWith(UiText.WorldTitle(flow.WorldBanner)), reason + ": la placa");
            Assert.That(banner.PlaceText, Does.EndWith(UiText.WorldPlace(flow.WorldBanner)), reason);
            Assert.That(banner.SceneSprite, Is.Null, reason + ": sin la viñeta pintada");
            Assert.That(banner.FlatWater.gameObject.activeInHierarchy, Is.True, reason + ": la viñeta, del color plano del agua");
            Assert.That(banner.FlatWater.color, Is.EqualTo(Palette.WaterBase(flow.Level.WaterBase)), reason);
            Assert.That(banner.Rosa.gameObject.activeInHierarchy, Is.False, reason + ": sin Rosa");
            Assert.That(banner.Paw.gameObject.activeInHierarchy, Is.False, reason + ": sin Capi");
            Assert.That(banner.Foam.gameObject.activeInHierarchy || banner.Spray.gameObject.activeInHierarchy, Is.False, reason);
            Assert.That(view.CardArt.MascotsBuilt, Is.False, reason + ": ninguna textura del banner");
            Assert.That(banner.RaycastTargets(), Is.EqualTo(new Graphic[] { banner.Dim }), reason);
            Assert.That(view.LastWorldBannerArtProbe, Does.Contain("\"level\":" + flow.LevelNumber + ",").And.Contain("\"fallback\":\"" + reason + "\""));
        }

        /// <summary>
        /// Con la tarjeta del banner a la vista (con su arte o sin él): la deja entrar, comprueba que se ve entera y colocada, la
        /// cierra con 1 toque y, tras su salida (y la tarjeta previa, si la hay), sigue la intro hasta PLAYING.
        /// </summary>
        public static IEnumerator CloseThenTheIntro(GameBootstrap boot, int level, int world)
        {
            GameScreen view = boot.View!;
            LevelFlow flow = boot.Flow!;
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(flow.LevelNumber, Is.EqualTo(level));
            Assert.That(flow.WorldBanner, Is.EqualTo(world));
            Assert.That(banner.CardShown, Is.True);
            PlayModeHelpers.Step(boot, 1500);
            Assert.That(banner.Alpha, Is.EqualTo(1f), "entera");
            Assert.That(banner.Settled, Is.True);
            AssertLaidOut(view);
            bool card = flow.WorldBannerThenCard;
            yield return null; // un fotograma de verdad: el raycast no ve un velo que el lienzo aún no ha pintado
            Tap(banner);
            Assert.That(flow.WorldBannerClosing && banner.Dismissing, Is.True, "un toque empieza la salida");
            string next = card ? GameScreen.BannerNextLevelCard : GameScreen.BannerNextIntro;
            Assert.That(view.LastWorldBannerClosedProbe, Is.EqualTo("{\"level\":" + level + ",\"world\":" + world + ",\"next\":\"" + next + "\"}"));
            PlayModeHelpers.Step(boot, 400);
            Assert.That(banner.Visible, Is.False);
            Assert.That(view.CardArt.MascotsBuilt, Is.False, "el arte del banner se suelta al cerrarlo");
            if (card)
            {
                Assert.That(view.LevelCard.Visible, Is.True, "la tarjeta previa, tras el banner");
                yield return null;
                CardSetup.TapCard(view.LevelCard);
            }

            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.Intro), "la intro sigue");
            PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            Assert.That(view.Intro.TimeMs, Is.GreaterThan(0f));
            CardSetup.StepUntilPlaying(boot);
        }

        /// <summary>
        /// Un toque en el banner por el EventSystem, como un dedo: en las esquinas, en el centro, sobre Reiniciar y sobre la pausa
        /// el primer impacto de <see cref="EventSystem.RaycastAll"/> es el velo; el clic va a ese primer impacto, en el centro.
        /// </summary>
        public static void Tap(WorldBannerView banner)
        {
            EventSystem events = EventSystem.current;
            Assert.That(events, Is.Not.Null, "EventSystem de la escena");
            Canvas canvas = banner.Root.GetComponentInParent<Canvas>().rootCanvas;
            Camera? cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            GameScreen? view = GameBootstrap.Current != null ? GameBootstrap.Current.View : null;
            float scale = canvas.scaleFactor;
            if (cam == null && view != null)
            {
                float fit = Mathf.Min(Screen.width / view.CanvasWidth, Screen.height / view.CardCanvasHeight);
                if (fit < scale)
                {
                    canvas.scaleFactor = fit; // en batchmode la pantalla real es más pequeña que la maqueta
                }
            }

            var hits = new List<RaycastResult>();
            var data = new PointerEventData(events) { button = PointerEventData.InputButton.Left };
            try
            {
                Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4];
                ((RectTransform)canvas.transform).GetWorldCorners(corners);
                Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                var inset = new Vector2(4f, 4f);
                var points = new List<(string Name, Vector2 Point)>
                {
                    ("abajo a la izquierda", min + inset),
                    ("arriba a la derecha", max - inset),
                    ("en el centro", (min + max) * 0.5f),
                };
                if (view != null && view.RestartButton.gameObject.activeInHierarchy)
                {
                    var restart = (RectTransform)view.RestartButton.transform;
                    points.Add(("sobre Reiniciar", RectTransformUtility.WorldToScreenPoint(cam, restart.TransformPoint(restart.rect.center))));
                }

                if (view != null && view.PauseButton.gameObject.activeInHierarchy)
                {
                    var pause = (RectTransform)view.PauseButton.transform;
                    points.Add(("sobre la pausa", RectTransformUtility.WorldToScreenPoint(cam, pause.TransformPoint(pause.rect.center))));
                }

                foreach ((string name, Vector2 point) in points)
                {
                    data.position = point;
                    hits.Clear();
                    events.RaycastAll(data, hits);
                    Assert.That(hits.Count, Is.GreaterThan(0), "el toque " + name + " da en algo");
                    Assert.That(hits[0].gameObject, Is.SameAs(banner.Dim.gameObject), "el toque " + name + ": el primer impacto es el velo, no " + hits[0].gameObject.name);
                }

                data.position = points[2].Point;
                hits.Clear();
                events.RaycastAll(data, hits);
                data.pointerCurrentRaycast = hits[0];
                data.pointerPressRaycast = hits[0];
            }
            finally
            {
                if (canvas.scaleFactor != scale)
                {
                    canvas.scaleFactor = scale;
                    Canvas.ForceUpdateCanvases();
                }
            }

            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        /// <summary>
        /// El banner cabe en la pantalla con su margen; la cabecera, la viñeta y la línea del lugar van dentro del marco y no se
        /// pisan; Capi y Rosa, dentro de la viñeta; los textos, enteros, sin desbordes y a ≥ 12 sp; y la superficie de madera es
        /// la de 281 dp de las tarjetas previas (no genera otra textura).
        /// </summary>
        public static void AssertLaidOut(GameScreen view)
        {
            Canvas.ForceUpdateCanvases();
            WorldBannerView banner = view.WorldBannerView;
            RectTransform root = (RectTransform)view.Canvas.transform;
            Rect panel = VictorySetup.CanvasRect(banner.Panel, root);
            Assert.That(panel.xMin, Is.GreaterThanOrEqualTo(LevelCardView.SideMarginDp - 0.5f));
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(view.CanvasWidth - LevelCardView.SideMarginDp + 0.5f));
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(view.SafeTopDp), "bajo la zona segura de arriba");
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(view.CardCanvasHeight - view.CardSafeBottom), "sobre la zona segura de abajo");
            Rect title = VictorySetup.CanvasRect(banner.TitleLabel.rectTransform, root);
            Rect scene = VictorySetup.CanvasRect(banner.Scene, root);
            Rect place = VictorySetup.CanvasRect(banner.PlaceLabel.rectTransform, root);
            foreach ((string name, Rect r) in new[] { ("cabecera", title), ("viñeta", scene), ("lugar", place) })
            {
                Assert.That(r.xMin, Is.GreaterThanOrEqualTo(panel.xMin + 7.5f), name + " dentro del marco");
                Assert.That(r.xMax, Is.LessThanOrEqualTo(panel.xMax - 7.5f), name + " dentro del marco");
                Assert.That(r.yMin, Is.GreaterThanOrEqualTo(panel.yMin + 7.5f), name + " dentro del marco");
                Assert.That(r.yMax, Is.LessThanOrEqualTo(panel.yMax - 7.5f), name + " dentro del marco");
            }

            Assert.That(title.yMax, Is.LessThanOrEqualTo(scene.yMin + 0.5f), "la cabecera, encima de la viñeta");
            Assert.That(scene.yMax, Is.LessThanOrEqualTo(place.yMin + 0.5f), "la viñeta, encima del lugar");
            Rect capi = VictorySetup.CanvasRect(banner.CapiRect, root);
            Assert.That(capi.xMin >= scene.xMin - 0.5f && capi.xMax <= scene.xMax + 0.5f && capi.yMin >= scene.yMin - 0.5f && capi.yMax <= scene.yMax + 0.5f, Is.True, "Capi, en la viñeta: " + capi + " en " + scene);
            Rect clip = VictorySetup.CanvasRect(banner.RosaClip, root);
            Assert.That(clip.yMin >= scene.yMin - 0.5f && clip.yMax <= scene.yMax + 0.5f, Is.True, "Rosa, en la viñeta");
            foreach (TextMeshProUGUI t in new[] { banner.TitleLabel, banner.PlaceLabel })
            {
                t.ForceMeshUpdate(true, true);
                Assert.That(t.fontSize, Is.GreaterThanOrEqualTo(12f), "«" + t.text + "» ≥ 12 sp");
                Assert.That(t.textInfo.characterCount, Is.EqualTo(t.text.Length), "«" + t.text + "» entero");
                Assert.That(t.preferredHeight, Is.LessThanOrEqualTo(t.rectTransform.rect.height + 0.5f), "«" + t.text + "» cabe en alto");
                Assert.That(t.textInfo.lineCount, Is.LessThanOrEqualTo(2), "«" + t.text + "» en 2 líneas como mucho");
            }

            float body = banner.Panel.rect.height - 16f;
            Sprite surface = banner.Surface.sprite;
            float surfaceDp = surface.rect.height * 100f / surface.pixelsPerUnit;
            Assert.That(surfaceDp, Is.EqualTo(ModalKit.SharedSurfaceHeight(body)).Within(0.5f));
            Assert.That(Mathf.RoundToInt(surfaceDp), Is.EqualTo(281), "el alto compartido de la tarjeta del 100");
        }
    }
}
