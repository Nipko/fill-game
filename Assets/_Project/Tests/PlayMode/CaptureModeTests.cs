#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AF.Core.Session;
using AF.Economy;
using AF.GameFlow;
using AF.Levels;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El modo de captura en la escena <c>Game</c> real (D-066; spark-marketing §6 E1; <see cref="CaptureOptions"/>, forzado con
    /// <see cref="LaunchOptions.CaptureOverride"/> porque en el Editor no hay intent):
    /// <list type="bullet">
    /// <item>un paquete <c>af-level/1</c> en base64 sustituye a los de la escena y la app arranca en su nivel; uno que no valida
    /// escribe <c>AF_CAPTURE error</c> y se juega con los de siempre;</item>
    /// <item>la solución del solver se reproduce hasta la victoria con los toques del juego, o se para a N vertidos del final;</item>
    /// <item>el HUD se oculta (alfa 0, sin toques) y, sin el extra, sigue a la vista;</item>
    /// <item>la pantalla pedida se abre al llegar al inicio, y una que no está disponible acaba en error;</item>
    /// <item>la skin y el fondo forzados son solo de la vista, y el perfil guardado no cambia aunque se gane el nivel.</item>
    /// </list>
    /// </summary>
    public sealed class CaptureModeTests
    {
        private const int Level = 17;
        private const int BannerLevel = 81;
        private const int BannerWorld = 4;
        private static readonly string[] BoardPalette = { "acai", "aguaje", "tapereba" };
        private static readonly DateTime T0 = new DateTime(2026, 10, 31, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            CaptureLog.Reset();
            GameBootstrap.ResetCloudOverrides();
            GameBootstrap.AnalyticsResetOverride = null;
            GameScreen.BannerTravelOverride = null;
        }

        [UnityTest]
        public IEnumerator Base64Board_ReplacesTheScenePacks_AndStartsInIt()
        {
            yield return LoadCapture(new CaptureOptions { Active = true, BoardBase64 = Base64(BoardPack()) }, ProfileAt(Level));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.CaptureBoardLoaded, Is.True);
            Assert.That(boot.Flow!.Catalog.Count, Is.EqualTo(1), "solo el tablero de la toma");
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Level), "arranca directamente en el tablero");
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(Level));
            Assert.That(boot.Flow.Level.Id, Is.EqualTo("m01_v1_n0017"));
            Assert.That(boot.ProfileStore, Is.InstanceOf<CaptureProfileStore>(), "la toma no guarda el perfil");
        }

        [UnityTest]
        public IEnumerator InvalidBoard_LogsTheError_AndKeepsTheScenePacks()
        {
            // Un color con 3 dosis en vez de 4: no pasa LevelValidation del núcleo.
            string broken = BoardPack().Replace("[4, 8, 4, 4]", "[4, 8, 4, 3]");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[AF\.Capture\] board: el paquete no es válido"));
            yield return LoadCapture(new CaptureOptions { Active = true, BoardBase64 = Base64(broken) }, ProfileAt(Level));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.CaptureBoardLoaded, Is.False);
            Assert.That(boot.Flow!.Catalog.Count, Is.GreaterThan(1), "los paquetes de la escena");
            Assert.That(boot.App!.Screen, Is.Not.EqualTo(AppScreen.Boot), "el juego arranca igual");
        }

        [UnityTest]
        public IEnumerator Autoplay_ReachesTheVictory_WithTheGameTaps()
        {
            var store = new MemoryProfileStore(ProfileAt(Level));
            string? before = store.Json;
            var options = new CaptureOptions { Active = true, BoardBase64 = Base64(BoardPack()), Autoplay = true, StartMs = 100, PauseMs = 120, JitterMs = 40, SelectMs = 80 };
            yield return LoadCapture(options, store);
            GameBootstrap boot = GameBootstrap.Current!;
            CaptureAutoplay autoplay = Run(boot, 120000);
            Assert.That(autoplay.EndReason, Is.EqualTo("won"), CaptureLog.LastLine);
            Assert.That(boot.Flow!.Session.Status, Is.EqualTo(LevelStatus.Won));
            Assert.That(autoplay.Pours, Is.EqualTo(autoplay.PlanLength), "la solución entera, sin vertidos de más");
            Assert.That(autoplay.Pours, Is.GreaterThan(0));
            StringAssert.Contains("\"event\":\"autoplay_end\"", CaptureLog.LastLine);

            // El perfil guardado no cambió aunque se ganó el nivel: la toma vive en memoria.
            Assert.That(boot.Profile!.Progress.MaxLevel, Is.EqualTo(Level), "en memoria, la victoria cuenta");
            Assert.That(store.SaveCount, Is.EqualTo(0));
            Assert.That(store.Json, Is.EqualTo(before), "el perfil guardado sigue como estaba");
            Assert.That(((CaptureProfileStore)boot.ProfileStore!).DiscardedSaves, Is.GreaterThan(0), "lo que se habría guardado se descartó");
        }

        [UnityTest]
        public IEnumerator Autoplay_StopsOneBeforeTheEnd()
        {
            var options = new CaptureOptions { Active = true, BoardBase64 = Base64(BoardPack()), Autoplay = true, StopBefore = 1, StartMs = 0, PauseMs = 50, JitterMs = 0, SelectMs = 50 };
            yield return LoadCapture(options, ProfileAt(Level));
            GameBootstrap boot = GameBootstrap.Current!;
            CaptureAutoplay autoplay = Run(boot, 120000);
            Assert.That(autoplay.EndReason, Is.EqualTo("stopped"), CaptureLog.LastLine);
            Assert.That(autoplay.Left, Is.EqualTo(1), "«falta uno»");
            Assert.That(autoplay.Pours, Is.EqualTo(autoplay.PlanLength - 1));
            Assert.That(boot.Flow!.Session.Status, Is.EqualTo(LevelStatus.Playing));
            Assert.That(boot.Flow.AcceptsBoardInput, Is.True, "el último vertido queda para la toma");
        }

        [UnityTest]
        public IEnumerator Autoplay_DismissesTheDifficultyCard_AfterReading_AndPours()
        {
            // Observación 3 de la prueba del Moto G23 (2026-11-15): en un Muy difícil, con el HUD a la vista, la tarjeta previa
            // tapaba el tablero y el juego automático no vertía nunca.
            LaunchOptions.LevelCardsOverride = true;
            var options = new CaptureOptions { Active = true, BoardBase64 = Base64(BoardPack(Level, LevelDefinition.LabelVeryHard)), Autoplay = true, StartMs = 100, PauseMs = 120, JitterMs = 0, SelectMs = 80 };
            yield return LoadCapture(options, ProfileAt(Level));
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            Assert.That(flow.Level.DifficultyLabel, Is.EqualTo(LevelDefinition.LabelVeryHard));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "la tarjeta de Muy difícil tapa el tablero");
            Assert.That(boot.View!.LevelCard.Visible, Is.True);

            // Antes del tiempo de lectura, la tarjeta sigue.
            var lines = new List<string>();
            Advance(boot, (int)GameBootstrap.CaptureCardReadMs - 500, lines, null);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "se deja leer");
            Assert.That(boot.CaptureAutoplayRun!.Pours, Is.EqualTo(0));
            Assert.That(lines, Is.Empty);

            // Sale de LEVEL_CARD poco después: como mucho 500 ms más tarde que el tiempo de lectura.
            int more = Advance(boot, 1000, lines, () => flow.Phase != FlowPhase.LevelCard);
            Assert.That(flow.Phase, Is.Not.EqualTo(FlowPhase.LevelCard), "la tarjeta se cerró sola");
            Assert.That(more, Is.LessThanOrEqualTo(1000), "≤ CaptureCardReadMs + 500 ms");
            Assert.That(lines, Has.Count.EqualTo(1));
            StringAssert.Contains("\"card\":\"level_card\"", lines[0]);
            Assert.That(ReadMs(lines[0]), Is.GreaterThanOrEqualTo((int)GameBootstrap.CaptureCardReadMs), lines[0]);

            CaptureAutoplay autoplay = Run(boot, 120000);
            Assert.That(autoplay.EndReason, Is.EqualTo("won"), CaptureLog.LastLine);
            Assert.That(autoplay.Pours, Is.GreaterThan(0), "el juego automático vierte tras cerrar la tarjeta");
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [UnityTest]
        public IEnumerator WithoutAutoplay_TheCardWaitsForTheFinger()
        {
            LaunchOptions.LevelCardsOverride = true;
            yield return LoadCapture(new CaptureOptions { Active = true, BoardBase64 = Base64(BoardPack(Level, LevelDefinition.LabelVeryHard)) }, ProfileAt(Level));
            GameBootstrap boot = GameBootstrap.Current!;
            var lines = new List<string>();
            Advance(boot, 5000, lines, null);
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.LevelCard), "sin el juego automático, la tarjeta espera al dedo");
            Assert.That(boot.View!.LevelCard.Visible, Is.True);
            Assert.That(lines, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Autoplay_WorldBannerThenCard_ReadsEach_AndWins()
        {
            // El 81 abre el mundo 4: banner de mundo con el viaje (M3.6) y, detrás, la tarjeta de Difícil.
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtSyncOverride = true;
            GameScreen.BannerTravelOverride = true;
            var options = new CaptureOptions { Active = true, BoardBase64 = Base64(BannerBoardPack()), Autoplay = true, StartMs = 100, PauseMs = 120, JitterMs = 0, SelectMs = 80 };
            yield return LoadCapture(options, ProfileAt(BannerLevel));
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            WorldBannerView banner = view.WorldBannerView;
            Assert.That(flow.LevelNumber, Is.EqualTo(BannerLevel));
            Assert.That(flow.WorldBanner, Is.EqualTo(BannerWorld));
            Assert.That(flow.WorldBannerThenCard, Is.True, "detrás va la tarjeta de Difícil");
            Assert.That(banner.Visible && banner.CardShown, Is.True);
            Assert.That(banner.Travel, Is.Not.Null, "el banner del 81 lleva el viaje");

            // El banner se lee: sigue en pantalla durante el tiempo de lectura.
            var lines = new List<string>();
            Advance(boot, (int)GameBootstrap.CaptureCardReadMs - 300, lines, null);
            Assert.That(lines, Is.Empty);
            Assert.That(flow.WorldBanner, Is.EqualTo(BannerWorld));
            Assert.That(banner.Visible && !banner.Dismissing, Is.True, "el banner, a la vista");
            Assert.That(banner.TravelRunning, Is.True, "el viaje aún va");

            // Primer toque: solo completa el viaje; el banner sigue.
            Advance(boot, 1000, lines, () => lines.Count > 0);
            Assert.That(lines, Has.Count.EqualTo(1));
            StringAssert.Contains("\"card\":\"world_banner_travel\"", lines[0]);
            Assert.That(ReadMs(lines[0]), Is.GreaterThanOrEqualTo((int)GameBootstrap.CaptureCardReadMs));
            Assert.That(banner.TravelDone, Is.True);
            Assert.That(banner.Visible && !banner.Dismissing, Is.True, "el primer toque no cierra");
            Assert.That(flow.WorldBannerClosing, Is.False);

            // Otra lectura y el segundo toque lo cierra.
            Advance(boot, (int)GameBootstrap.CaptureCardReadMs - 300, lines, null);
            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(flow.WorldBanner, Is.EqualTo(BannerWorld));
            Advance(boot, 1000, lines, () => lines.Count > 1);
            Assert.That(lines, Has.Count.EqualTo(2));
            StringAssert.Contains("\"card\":\"world_banner\"", lines[1]);

            // Tras la salida, la tarjeta previa, que también se lee entera.
            Advance(boot, 2000, lines, () => flow.WorldBanner == 0);
            Assert.That(flow.WorldBanner, Is.EqualTo(0), "el banner salió");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(flow.LevelCard, Is.Not.Null, "la tarjeta de Difícil");
            Assert.That(view.LevelCard.Visible, Is.True);
            Advance(boot, (int)GameBootstrap.CaptureCardReadMs - 300, lines, null);
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard), "la tarjeta se lee entera");
            Assert.That(lines, Has.Count.EqualTo(2));
            Advance(boot, 1000, lines, () => flow.Phase != FlowPhase.LevelCard);
            Assert.That(lines, Has.Count.EqualTo(3));
            StringAssert.Contains("\"card\":\"level_card\"", lines[2]);
            Assert.That(ReadMs(lines[2]), Is.GreaterThanOrEqualTo((int)GameBootstrap.CaptureCardReadMs));

            CaptureAutoplay autoplay = Run(boot, 120000);
            Assert.That(autoplay.EndReason, Is.EqualTo("won"), CaptureLog.LastLine);
            Assert.That(autoplay.Pours, Is.GreaterThan(0));
            Assert.That(flow.Session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [UnityTest]
        public IEnumerator CaptureTapCard_WithoutTheViewCards_GoesStraightToTheFlow()
        {
            // Respaldo: si la vista no enseña el banner ni la tarjeta, el toque del juego automático va directo al flujo.
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtSyncOverride = true;
            GameScreen.BannerTravelOverride = false;
            yield return LoadCapture(new CaptureOptions { Active = true, BoardBase64 = Base64(BannerBoardPack()) }, ProfileAt(BannerLevel));
            GameBootstrap boot = GameBootstrap.Current!;
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            Assert.That(flow.WorldBanner, Is.EqualTo(BannerWorld));

            view.WorldBannerView.HideNow();
            Assert.That(view.CaptureTapCard(flow), Is.EqualTo("world_banner"), "DismissWorldBanner");
            for (int t = 0; t < 3000 && flow.WorldBanner != 0; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.WorldBanner, Is.EqualTo(0));
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.LevelCard));
            Assert.That(flow.LevelCard, Is.Not.Null);

            view.LevelCard.HideNow();
            Assert.That(view.CaptureTapCard(flow), Is.EqualTo("level_card"), "DismissLevelCard");
            Assert.That(flow.Phase, Is.Not.EqualTo(FlowPhase.LevelCard), "arranca la intro");
            Assert.That(view.CaptureTapCard(flow), Is.Null, "fuera de LEVEL_CARD no hace nada");
        }

        [UnityTest]
        public IEnumerator HideHud_HidesTheHudLayers_AndKeepsTheBoard()
        {
            yield return LoadCapture(new CaptureOptions { Active = true, HideHud = true, BoardBase64 = Base64(BoardPack()) }, ProfileAt(Level));
            GameBootstrap boot = GameBootstrap.Current!;
            GameScreen view = boot.View!;
            Assert.That(view.CaptureHudHidden, Is.True);
            CanvasGroup? hud = view.Hud.GetComponent<CanvasGroup>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud!.alpha, Is.EqualTo(0f));
            Assert.That(hud.blocksRaycasts, Is.False);
            Assert.That(view.PauseButton.transform.GetComponentInParent<CanvasGroup>()!.alpha, Is.EqualTo(0f), "el engranaje tampoco se ve");
            Assert.That(boot.Flow!.WorldBannersEnabled || boot.Flow.LevelCardsEnabled, Is.False, "ni banner de mundo ni tarjeta previa");
            Assert.That(view.BoardContainer.gameObject.activeInHierarchy, Is.True, "el tablero sigue");
            Assert.That(view.BoardContainer.GetComponent<CanvasGroup>() == null || view.BoardContainer.GetComponent<CanvasGroup>().alpha > 0f, Is.True);

            view.SetCaptureHudHidden(false);
            Assert.That(hud.alpha, Is.EqualTo(1f));
            Assert.That(hud.blocksRaycasts, Is.True);
        }

        [UnityTest]
        public IEnumerator WithoutTheExtra_TheHudStaysVisible()
        {
            yield return LoadCapture(new CaptureOptions { Active = true, BoardBase64 = Base64(BoardPack()) }, ProfileAt(Level));
            GameScreen view = GameBootstrap.Current!.View!;
            Assert.That(view.CaptureHudHidden, Is.False);
            CanvasGroup? hud = view.Hud.GetComponent<CanvasGroup>();
            Assert.That(hud == null || hud.alpha > 0f, Is.True);
        }

        [UnityTest]
        public IEnumerator Screen_Map_OpensWhenTheLobbyIsClear()
        {
            Profile profile = ProfileAt(31);
            profile.Progress.MapSeenLevel = 30; // sin la secuencia del Mapa del 10
            yield return LoadCapture(new CaptureOptions { Active = true, Screen = CaptureScreen.Map }, profile);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            for (int t = 0; t < 3000 && boot.CaptureScreenPending != CaptureScreen.None; t += PlayModeHelpers.FrameMs)
            {
                boot.TickCapture(PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.CaptureScreenPending, Is.EqualTo(CaptureScreen.None));
            Assert.That(boot.RiverMapView!.Visible, Is.True, "el Mapa del río, directo");
            StringAssert.Contains("\"event\":\"screen_open\"", CaptureLog.LastLine);
            StringAssert.Contains("\"screen\":\"map\"", CaptureLog.LastLine);
        }

        [UnityTest]
        public IEnumerator Screen_Unavailable_EndsWithAnError()
        {
            // Con el nivel 5 el Mapa aún no se abre (pestaña desde max_level ≥ 10): tras el tiempo de espera, error y nada pendiente.
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[AF\.Capture\] screen: map no se pudo abrir"));
            yield return LoadCapture(new CaptureOptions { Active = true, Screen = CaptureScreen.Map }, ProfileAt(6));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            for (int t = 0; t <= GameBootstrap.CaptureScreenTimeoutMs + 1000 && boot.CaptureScreenPending != CaptureScreen.None; t += 500)
            {
                boot.TickCapture(500);
            }

            Assert.That(boot.CaptureScreenPending, Is.EqualTo(CaptureScreen.None));
            Assert.That(boot.RiverMapView == null || !boot.RiverMapView.Visible, Is.True);
            StringAssert.Contains("\"event\":\"error\"", CaptureLog.LastLine);
        }

        [UnityTest]
        public IEnumerator SkinAndBackdrop_AreForcedInTheView_WithoutTouchingTheProfile()
        {
            string skin = SkinRewards.Milestones[0].SkinId;
            Profile profile = ProfileAt(Level);
            Assert.That(profile.Entitlements.SkinEquipped, Is.Null);
            var options = new CaptureOptions { Active = true, BoardBase64 = Base64(BoardPack()), Skin = skin, World = 3 };
            yield return LoadCapture(options, profile);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.View!.Views[0].Skin.Id, Is.EqualTo(skin), "las cuias del tablero llevan la skin de la toma");
            Assert.That(boot.Profile!.Entitlements.SkinEquipped, Is.Null, "el perfil no cambia");
            Assert.That(boot.Profile.Entitlements.Skins, Does.Not.Contain(skin), "ni se la regala");
            Assert.That(boot.View.CaptureBackdropLevel, Is.EqualTo(WorldTable.FirstLevel(3)));
            Assert.That(boot.View.StopBackdrop.Stop, Is.Not.Null);
            Assert.That(boot.View.StopBackdrop.Stop!.World, Is.EqualTo(3), "el fondo de la gran parada del mundo 3");
        }

        [UnityTest]
        public IEnumerator DeleteAccount_IsBlocked_AndTouchesNothing()
        {
            var flags = new MemoryCloudSaveFlags();
            GameBootstrap.CloudFlagsOverride = flags;
            int wipes = 0;
            int analyticsResets = 0;
            GameBootstrap.WipePlayerPrefsForTests = () => wipes++;
            GameBootstrap.AnalyticsResetOverride = () => analyticsResets++;
            var store = new MemoryProfileStore(ProfileAt(40));
            string? before = store.Json;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[AF\.Capture\] account:"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[AF\.Capture\] account:"));
            yield return LoadCapture(new CaptureOptions { Active = true }, store);
            GameBootstrap boot = GameBootstrap.Current!;
            var server = new DeleteAccountWaitTests.SlowServer();
            boot.ServerAccountDelete = server.Ask;
            Assert.That(boot.PressSettings(), Is.True);

            Assert.That(boot.OpenDeleteAccount(), Is.False, "el diálogo no se abre durante una toma");
            Assert.That(boot.DeleteAccountOpen, Is.False);
            StringAssert.Contains("\"what\":\"account\"", CaptureLog.LastLine);
            Assert.That(boot.ConfirmDeleteAccount(), Is.False, "ni confirmando directamente");
            Assert.That(server.Calls, Is.EqualTo(0), "no se llama a deleteAccount");
            Assert.That(wipes, Is.EqualTo(0), "PlayerPrefs intactas");
            Assert.That(analyticsResets, Is.EqualTo(0), "la analítica, también");
            Assert.That(flags.DeletePending, Is.False, "sin borrado pendiente de la nube");
            Assert.That(boot.AccountDeleted, Is.False);
            Assert.That(store.SaveCount, Is.EqualTo(0));
            Assert.That(store.Json, Is.EqualTo(before), "el perfil guardado sigue igual");
        }

        [UnityTest]
        public IEnumerator Capture_AlwaysUsesTheFakeStore()
        {
            yield return LoadCapture(new CaptureOptions { Active = true }, ProfileAt(Level));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.Backend, Is.InstanceOf<AF.Backend.FakeBackendClient>(), "la verificación falsa, como af_fake_store");
        }

        // Carga la escena con el modo de captura y el perfil dado (en un almacén en memoria del test).
        private static IEnumerator LoadCapture(CaptureOptions options, Profile profile)
        {
            return LoadCapture(options, new MemoryProfileStore(profile));
        }

        private static IEnumerator LoadCapture(CaptureOptions options, MemoryProfileStore store)
        {
            LaunchOptions.CaptureOverride = options;
            CaptureLog.Reset();
            yield return PlayModeHelpers.LoadApp(store);
            PlayModeHelpers.TakeClock(GameBootstrap.Current!);
        }

        // Avanza la vista, el flujo y el modo de captura con el reloj del test hasta que la reproducción acaba.
        private static CaptureAutoplay Run(GameBootstrap boot, int maxMs)
        {
            CaptureAutoplay autoplay = boot.CaptureAutoplayRun!;
            Assert.That(autoplay, Is.Not.Null);
            for (int t = 0; t < maxMs && !autoplay.Done; t += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                boot.TickCapture(PlayModeHelpers.FrameMs);
            }

            Assert.That(autoplay.Done, Is.True, "la reproducción acaba: " + CaptureLog.LastLine);
            return autoplay;
        }

        // Avanza la vista, el flujo y el modo de captura como Run, hasta maxMs o hasta stop; apunta cada AF_CAPTURE card_dismissed.
        // Devuelve los ms avanzados.
        private static int Advance(GameBootstrap boot, int maxMs, List<string> cardLines, Func<bool>? stop)
        {
            int t = 0;
            while (t < maxMs && (stop == null || !stop()))
            {
                string? before = CaptureLog.LastLine;
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
                boot.TickCapture(PlayModeHelpers.FrameMs);
                t += PlayModeHelpers.FrameMs;
                string? line = CaptureLog.LastLine;
                if (line != null && !ReferenceEquals(line, before) && line.Contains("\"event\":\"card_dismissed\""))
                {
                    cardLines.Add(line);
                }
            }

            return t;
        }

        // El read_ms de una línea card_dismissed.
        private static int ReadMs(string line)
        {
            Match m = Regex.Match(line, "\"read_ms\":(\\d+)");
            Assert.That(m.Success, Is.True, line);
            return int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static Profile ProfileAt(int nextLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        // El tablero del nivel 2 del tutorial (5 cuias, 3 tintes, par 6) puesto en el 17 del mundo 1, con la etiqueta pedida.
        private static string BoardPack(int level = Level, string? label = null)
        {
            return PlayModeHelpers.BoardPack(1, "w01_estuario", "white", level, BoardPalette, BoardCuias(), label);
        }

        // El mismo tablero en el 81, el primero del mundo 4 (banner de mundo), con la etiqueta Difícil (tarjeta previa detrás).
        private static string BannerBoardPack()
        {
            return PlayModeHelpers.BoardPack(BannerWorld, "w04_trapecio", "white", BannerLevel, BoardPalette, BoardCuias(), LevelDefinition.LabelHard);
        }

        private static int[][] BoardCuias()
        {
            return new[] { new[] { 4, 8, 4, 4 }, new[] { 3, 3, 3, 4 }, new[] { 3, 8, 8, 8 }, new int[0], new int[0] };
        }

        private static string Base64(string json)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }
    }
}
