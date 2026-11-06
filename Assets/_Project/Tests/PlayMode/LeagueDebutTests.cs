#nullable enable
using System;
using System.Collections;
using AF.Analytics;
using AF.Audio;
using AF.Economy;
using AF.GameFlow;
using AF.LiveOps;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// El estreno de la Liga del río (añadido de D-046 del 2026-11-06; D-059, añadido del 2026-10-31, punto 3) en la escena
    /// <c>Game</c> real con la API falsa: sale una sola vez (también tras volver a abrir la app), en el orden correcto tras el banner
    /// de mundo del 41 y la victoria del 41 (al dejarla, nunca encima de ellos), «Ver la Liga» abre la Liga, «Luego» y Atrás dejan
    /// «Nueva» en la tarjeta del inicio hasta abrirla, no sale con la Liga apagada por Remote Config ni con &lt; 13, suena la
    /// fanfarria con los efectos del proyecto y, con Reducir movimiento, sale entera y quieta.
    /// </summary>
    public sealed class LeagueDebutTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 11, 6, 15, 0, 0, DateTimeKind.Utc);

        private ManualClock _clock = null!;
        private FakeLeagueApi _api = null!;
        private FakeAudioService _audio = null!;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(T0);
            _api = new FakeLeagueApi(_clock) { StartPoints = 140 };
            GameBootstrap.LeagueApiOverride = _api;
            GameBootstrap.LeagueClockOverride = _clock;
            GameBootstrap.LeagueConfigOverride = new LeagueConfig { LiveLeagueEnabled = true };
            GameBootstrap.LeagueDebutOverride = true;
            _audio = new FakeAudioService();
            AudioCues.Override = _audio;
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetLeagueOverrides();
            GameBootstrap.ResetTelemetryOverrides();
            GameBootstrap.AgeUnder13Override = null;
            GameBootstrap.LogbookOverride = null;
            AudioCues.Override = null;
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
        }

        private static Profile ProfileAt(int maxLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.MaxLevel = maxLevel;
            profile.Progress.NextLevel = maxLevel + 1;
            return profile;
        }

        private static IEnumerator Lobby(MemoryProfileStore store)
        {
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<LeagueRunner>().enabled = false; // el test avanza la Liga a mano
            boot.TickLeague(16f);
        }

        private static void Tick(GameBootstrap boot, float ms)
        {
            for (float t = 0f; t < ms; t += 16f)
            {
                boot.TickLeague(16f);
            }
        }

        [UnityTest]
        public IEnumerator Lobby_Past41_ShowsOnce_LaterLeavesNew_UntilOpened()
        {
            var store = new MemoryProfileStore(ProfileAt(45));
            yield return Lobby(store);
            GameBootstrap boot = GameBootstrap.Current!;
            LeagueDebutScreen debut = boot.LeagueDebutView!;
            Assert.That(boot.LeagueDebutVisible, Is.True, "al entrar al inicio con la Liga activa y sin estreno visto");
            Assert.That(debut.TitleText, Is.EqualTo("¡Llegó la Liga del río!"));
            Assert.That(debut.BodyText, Is.EqualTo(LeagueText.DebutBody));
            Assert.That(debut.OpenText, Is.EqualTo("Ver la Liga"));
            Assert.That(debut.LaterText, Is.EqualTo("Luego"));
            Assert.That(boot.Profile!.Progress.LeagueDebutSeen, Is.True, "apuntado al salir: un cierre de la app no lo repite");
            Assert.That(store.Json, Does.Contain("\"league_debut_seen\": true"), "y guardado");
            Assert.That(boot.LeagueButton!.Shown, Is.False, "el estreno tapa el inicio");
            Assert.That(_audio.Requested(SfxId.StreakUp) && _audio.Requested(SfxId.Confetti), Is.True, "la fanfarria con los efectos del proyecto");
            Assert.That(_audio.StingerCount(StingerId.Arrival, false), Is.EqualTo(1), "con su remate");

            // La entrada: el emblema entra con escala y rebote, los rayos giran, la lluvia cae y los botones esperan a verse.
            Assert.That(debut.ButtonsLive, Is.False, "los botones aún no reciben toques en t = 0");
            Assert.That(debut.EmblemScale, Is.LessThan(0.5f), "el emblema entra desde pequeño");
            Tick(boot, 200f);
            Assert.That(debut.EmblemScale, Is.GreaterThan(0.5f));
            Tick(boot, 900f);
            Assert.That(debut.Settled, Is.True);
            Assert.That(debut.ButtonsLive, Is.True);
            Assert.That(debut.EmblemScale, Is.EqualTo(1f).Within(0.01f));
            Assert.That(debut.ConfettiVisible, Is.GreaterThan(0), "las hojas caen");
            Assert.That(debut.RaysAngle, Is.Not.EqualTo(0f).Within(0.01f), "los rayos giran");
            AssertInsideAndReadable(debut);

            Assert.That(boot.PressAndroidBack(), Is.True, "Atrás es «Luego»");
            Assert.That(boot.LastBackScreen, Is.EqualTo("league_debut"));
            Assert.That(boot.LeagueDebutVisible, Is.False);
            Assert.That(boot.LeagueOpen, Is.False, "«Luego» no abre la Liga");
            boot.TickLeague(16f);
            Assert.That(boot.LeagueButton.Shown, Is.True);
            Assert.That(boot.LeagueButton.NewShown, Is.True, "«Nueva» en la tarjeta hasta abrirla");
            Assert.That(boot.LeagueButton.Card.BadgeText.text, Is.EqualTo("Nueva"));
            Assert.That(boot.LeagueDebutView!.ShownCount, Is.EqualTo(1));

            // Otra vez la app (el perfil del disco): ni estreno ni nada nuevo, y «Nueva» sigue.
            yield return Lobby(store);
            boot = GameBootstrap.Current!;
            Tick(boot, 100f);
            Assert.That(boot.LeagueDebutVisible, Is.False, "una sola vez por jugador");
            Assert.That(boot.LeagueButton!.NewShown, Is.True);

            Assert.That(boot.OpenLeague(), Is.True);
            Assert.That(boot.Profile!.Progress.LeagueOpened, Is.True);
            boot.CloseLeague();
            boot.TickLeague(16f);
            Assert.That(boot.LeagueButton.NewShown, Is.False, "abierta la Liga, fuera «Nueva»");
            Assert.That(boot.LeagueButton.Card.BadgeText.text, Is.Empty);
            Assert.That(store.Json, Does.Contain("\"league_opened\": true"));
        }

        [UnityTest]
        public IEnumerator Lobby_SeeTheLeague_OpensIt()
        {
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
            yield return Lobby(new MemoryProfileStore(ProfileAt(57)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.LeagueDebutVisible, Is.True);
            boot.LeagueDebutView!.OpenButton.onClick.Invoke();
            Assert.That(boot.LeagueDebutVisible, Is.False);
            Assert.That(boot.LeagueOpen, Is.True, "«Ver la Liga» abre la Liga");
            Assert.That(sink.Last("af_league_open")!.Find("scope")!.Value.Text, Is.EqualTo("global"), "con el evento de siempre (08 §8.2)");
            Assert.That(boot.Profile!.Progress.LeagueOpened, Is.True);
            boot.CloseLeague();
            boot.TickLeague(16f);
            Assert.That(boot.LeagueButton!.NewShown, Is.False, "abierta: sin «Nueva»");
        }

        [UnityTest]
        public IEnumerator NotShown_WithTheLeagueOffByRemoteConfig_Under13_OrBefore41()
        {
            GameBootstrap.LeagueConfigOverride = new LeagueConfig { LiveLeagueEnabled = false };
            yield return Lobby(new MemoryProfileStore(ProfileAt(60)));
            GameBootstrap boot = GameBootstrap.Current!;
            Tick(boot, 100f);
            Assert.That(boot.LeagueDebutVisible, Is.False, "live_league_enabled apagada: sin estreno");
            Assert.That(boot.LeagueDebutPending, Is.False);
            Assert.That(boot.LeagueButton!.NewShown, Is.False);
            Assert.That(boot.Profile!.Progress.LeagueDebutSeen, Is.False, "queda pendiente para cuando se encienda");

            GameBootstrap.LeagueConfigOverride = new LeagueConfig { LiveLeagueEnabled = true };
            GameBootstrap.AgeUnder13Override = true;
            yield return Lobby(new MemoryProfileStore(ProfileAt(60)));
            boot = GameBootstrap.Current!;
            Tick(boot, 100f);
            Assert.That(boot.LeagueDebutVisible, Is.False, "con < 13 no hay Liga (D-033)");

            GameBootstrap.AgeUnder13Override = null;
            yield return Lobby(new MemoryProfileStore(ProfileAt(40)));
            boot = GameBootstrap.Current!;
            Tick(boot, 100f);
            Assert.That(boot.LeagueDebutVisible, Is.False, "hasta ganar el 41, nada");
        }

        [UnityTest]
        public IEnumerator Lobby_WithTheLogbookPending_TheDebutTakesThisEntry_TheLogbookTheNext()
        {
            GameBootstrap.LogbookOverride = true;
            yield return Lobby(new MemoryProfileStore(ProfileAt(45)));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.TickLogbook(16f);
            Assert.That(boot.LeagueDebutVisible, Is.True, "el estreno, el hito de esta entrada");
            Assert.That(boot.LogbookScreen != null && boot.LogbookScreen.Visible, Is.False, "sin la libreta encima");
            boot.LeagueDebutView!.LaterButton.onClick.Invoke();
            boot.TickLogbook(16f);
            boot.TickLeague(16f);
            Assert.That(boot.LogbookScreen != null && boot.LogbookScreen.Visible, Is.False, "ni justo después: nunca dos pantallas seguidas");
            Assert.That(boot.LeagueButton!.NewShown, Is.True);
            Assert.That(boot.Takeovers!.ShownThisSession, Is.EqualTo(0), "el estreno es un hito: no gasta el cupo de la sesión");
            Assert.That(boot.LastTakeover, Is.EqualTo(TakeoverKind.None));
        }

        [UnityTest]
        public IEnumerator ReduceMotion_ShowsItWholeAndStill()
        {
            Profile profile = ProfileAt(45);
            profile.Settings.ReduceMotion = ReduceMotionSetting.On;
            yield return Lobby(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            LeagueDebutScreen debut = boot.LeagueDebutView!;
            Assert.That(boot.LeagueDebutVisible, Is.True);
            Assert.That(debut.ReduceMotion, Is.True);
            Assert.That(debut.Settled && debut.ButtonsLive, Is.True, "entera desde el primer fotograma");
            Assert.That(debut.EmblemScale, Is.EqualTo(1f));
            Tick(boot, 1500f);
            Assert.That(debut.RaysAngle, Is.EqualTo(0f).Within(0.01f), "sin giro");
            Assert.That(debut.ConfettiVisible, Is.EqualTo(0), "sin lluvia");
            Assert.That(debut.EmblemScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator FitsAt130_InEsEnPt([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale)
        {
            LaunchOptions.LanguageOverride = locale == LocLocale.SourceLocale ? null : locale;
            LaunchOptions.FontScaleOverride = 1.3f;
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            yield return Lobby(new MemoryProfileStore(ProfileAt(45)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.LeagueDebutVisible, Is.True);
            Tick(boot, 1200f);
            AssertInsideAndReadable(boot.LeagueDebutView!);
        }

        [UnityTest]
        public IEnumerator Victory41_AfterTheWorldBanner_ShowsOnLeave_NeverOverThem_ThenNextGoesOn()
        {
            // El 41 abre el mundo 3 (Manaos): su banner sale al entrar, antes de jugar. La Liga se desbloquea al ganarlo. El estreno
            // no sale con el banner ni con la victoria a la vista: sale al dejar la victoria («Siguiente»), y «Luego» sigue al 42.
            FunnelSetup.Hooks();
            LaunchOptions.LevelCardsOverride = true;
            LaunchOptions.BannerArtSyncOverride = true;
            Profile start = VictorySetup.Profile(100);
            start.Progress.MaxLevel = 40;
            start.Progress.NextLevel = 41;
            start.Progress.MapSeenLevel = 40;
            var store = new MemoryProfileStore(start);
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.GetComponent<LeagueRunner>().enabled = false;
            boot.TickLeague(16f);
            Assert.That(boot.LeagueDebutVisible, Is.False, "con el 40 ganado, aún no");
            Assert.That(boot.PressPlay(), Is.True);
            LevelFlow flow = boot.Flow!;
            GameScreen view = boot.View!;
            bool bannerSeen = false;
            for (int ms = 0; ms < 20000 && flow.Phase == FlowPhase.LevelCard; ms += 400)
            {
                bannerSeen |= view.WorldBannerView.CardShown;
                PlayModeHelpers.Step(boot, 400);
                boot.TickLeague(16f);
                Assert.That(boot.LeagueDebutVisible, Is.False, "nunca encima del banner de mundo ni de la tarjeta previa");
                boot.TapAnywhere();
            }

            Assert.That(flow.LevelNumber, Is.EqualTo(41));
            Assert.That(bannerSeen, Is.True, "el banner del mundo 3 al entrar al 41");
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickLeague(16f);
            Assert.That(boot.League!.Active, Is.True, "ganado el 41, la Liga está activa");
            Assert.That(boot.LeagueDebutPending, Is.True);
            Assert.That(boot.LeagueDebutVisible, Is.False, "nunca encima de la victoria");

            Assert.That(boot.PressNext(), Is.True, "Siguiente");
            Assert.That(boot.LeagueDebutVisible, Is.True, "al dejar la victoria");
            Assert.That(flow.Phase, Is.EqualTo(FlowPhase.VictoryScreen), "el camino espera a la respuesta");
            Assert.That(boot.PressNext(), Is.False, "con el estreno delante, la victoria no se toca");
            Assert.That(boot.PressHome(), Is.False);

            boot.LeagueDebutView!.LaterButton.onClick.Invoke();
            for (int ms = 0; ms < 15000 && flow.LevelNumber != 42; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(flow.LevelNumber, Is.EqualTo(42), "«Luego» sigue el camino de Siguiente");
            Assert.That(boot.LeagueDebutVisible, Is.False);
            Assert.That(boot.Profile!.Progress.LeagueDebutSeen, Is.True);
            Assert.That(boot.LeagueDebutPending, Is.False, "una sola vez");
        }

        [UnityTest]
        public IEnumerator Victory41_Home_SeeTheLeague_GoesHomeAndOpensIt()
        {
            FunnelSetup.Hooks();
            LaunchOptions.LevelOffsetOverride = 40;
            Profile profile = VictorySetup.Profile(100);
            profile.Progress.MaxLevel = 40;
            profile.Progress.NextLevel = 41;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.GetComponent<LeagueRunner>().enabled = false;
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.LeagueDebutVisible, Is.False);
            Assert.That(boot.PressHome(), Is.True, "Inicio");
            Assert.That(boot.LeagueDebutVisible, Is.True);
            boot.LeagueDebutView!.OpenButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "al inicio");
            for (int i = 0; i < 10 && !boot.LeagueOpen; i++)
            {
                boot.TickLeague(16f);
                yield return null;
            }

            Assert.That(boot.LeagueOpen, Is.True, "y allí la Liga");
            Assert.That(boot.Profile!.Progress.LeagueOpened, Is.True);
        }

        [UnityTest]
        [Explicit("Capturas del estreno de la Liga; necesitan dispositivo gráfico.")]
        public IEnumerator CaptureDebut([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale)
        {
            string dir = SnapshotKit.Prepare(locale);
            try
            {
                yield return Lobby(new MemoryProfileStore(ProfileAt(45)));
                GameBootstrap boot = GameBootstrap.Current!;
                LeagueDebutScreen debut = boot.LeagueDebutView!;
                Assert.That(debut.Visible, Is.True);
                Tick(boot, 1400f); // la entrada terminada, con las hojas aún cayendo
                SnapshotKit.AttachLobby(boot);
                SnapshotKit.AttachOverlay(debut.Canvas);
                yield return SnapshotKit.Wait(0.3f);
                SnapshotKit.Save(dir, "league_debut");
                debut.LaterButton.onClick.Invoke();
                boot.TickLeague(16f);
                SnapshotKit.AttachOverlay(boot.LeagueButton!.Canvas);
                yield return SnapshotKit.Wait(0.3f);
                SnapshotKit.Save(dir, "league_debut_new_badge");
            }
            finally
            {
                SnapshotKit.Release();
            }
        }

        // Todo dentro de la pantalla (en dp, la del lienzo con la pantalla simulada) y sin textos recortados ni por debajo de 12 sp;
        // los botones con su área táctil y sin pisarse.
        private static void AssertInsideAndReadable(LeagueDebutScreen debut)
        {
            var screen = new Rect(0f, 0f, debut.ScreenWidthDp, debut.ScreenHeightDp);
            foreach (TMPro.TextMeshProUGUI text in debut.Texts())
            {
                text.ForceMeshUpdate();
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(12f - 1e-3f), text.name + ": nada por debajo de 12 sp");
                Assert.That(text.isTextOverflowing, Is.False, text.name + " cabe en su caja: «" + text.text + "»");
            }

            Rect title = Box(debut.TitleRect);
            Rect body = Box(debut.BodyRect);
            Rect open = Box((RectTransform)debut.OpenButton.transform);
            Rect later = Box((RectTransform)debut.LaterButton.transform);
            Rect emblem = Box(debut.EmblemRect);
            AssertInside(title, screen, "el título");
            AssertInside(body, screen, "la línea");
            AssertInside(open, screen, "Ver la Liga");
            AssertInside(later, screen, "Luego");
            AssertInside(emblem, screen, "el emblema");
            Assert.That(emblem.yMax, Is.LessThanOrEqualTo(title.yMin + 0.5f), "el emblema sobre el título");
            Assert.That(title.yMax, Is.LessThanOrEqualTo(body.yMin + 0.5f), "el título sobre la línea");
            Assert.That(body.yMax, Is.LessThanOrEqualTo(open.yMin + 0.5f), "la línea sobre «Ver la Liga»");
            Assert.That(open.yMax, Is.LessThanOrEqualTo(later.yMin + 0.5f), "«Ver la Liga» sobre «Luego»");
            Assert.That(((RectTransform)debut.OpenButton.transform).rect.height, Is.GreaterThanOrEqualTo(48f), "área táctil (07 §7.8)");
            Assert.That(((RectTransform)debut.LaterButton.transform).rect.height, Is.GreaterThanOrEqualTo(48f));
        }

        // La caja de rt en dp desde arriba a la izquierda del lienzo (todas cuelgan de la raíz, ancladas arriba a la izquierda).
        private static Rect Box(RectTransform rt)
        {
            Vector2 size = rt.sizeDelta * (Vector2)rt.localScale;
            float left = rt.anchoredPosition.x - (rt.pivot.x * size.x);
            float top = -rt.anchoredPosition.y - ((1f - rt.pivot.y) * size.y);
            return new Rect(left, top, size.x, size.y);
        }

        private static void AssertInside(Rect box, Rect screen, string what)
        {
            Assert.That(box.xMin, Is.GreaterThanOrEqualTo(screen.xMin - 0.5f), what + " dentro de la pantalla (izquierda)");
            Assert.That(box.xMax, Is.LessThanOrEqualTo(screen.xMax + 0.5f), what + " dentro de la pantalla (derecha)");
            Assert.That(box.yMin, Is.GreaterThanOrEqualTo(screen.yMin - 0.5f), what + " dentro de la pantalla (arriba)");
            Assert.That(box.yMax, Is.LessThanOrEqualTo(screen.yMax + 0.5f), what + " dentro de la pantalla (abajo)");
        }
    }
}
