#nullable enable
using System;
using System.Collections;
using System.Text;
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
        private static readonly DateTime T0 = new DateTime(2026, 10, 31, 15, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            LaunchOptions.Reset();
            CaptureLog.Reset();
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

        private static Profile ProfileAt(int nextLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.NextLevel = nextLevel;
            profile.Progress.MaxLevel = nextLevel - 1;
            return profile;
        }

        // El tablero del nivel 2 del tutorial (5 cuias, 3 tintes, par 6) puesto en el 17 del mundo 1.
        private static string BoardPack()
        {
            string[] palette = { "acai", "aguaje", "tapereba" };
            int[][] cuias = { new[] { 4, 8, 4, 4 }, new[] { 3, 3, 3, 4 }, new[] { 3, 8, 8, 8 }, new int[0], new int[0] };
            return PlayModeHelpers.BoardPack(1, "w01_estuario", "white", Level, palette, cuias);
        }

        private static string Base64(string json)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }
    }
}
