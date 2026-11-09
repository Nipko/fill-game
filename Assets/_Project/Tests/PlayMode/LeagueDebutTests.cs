#nullable enable
using System;
using System.Collections;
using AF.Analytics;
using AF.Audio;
using AF.Economy;
using AF.IAP;
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
            GameBootstrap.ResetSessionOverrides();
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetLogbookOverrides();
            GameBootstrap.A11yHostOverride = null;
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

        // af_league_debut (08 §8.2; Q-LD-1, 2026-11-09): cuántos llegaron y que el último lleva esa respuesta y ese origen.
        private static int DebutEvents(FakeAnalyticsSink sink)
        {
            int n = 0;
            foreach (string name in sink.Names)
            {
                n += name == "af_league_debut" ? 1 : 0;
            }

            return n;
        }

        private static void AssertDebutEvent(FakeAnalyticsSink sink, string answer, string source)
        {
            Assert.That(DebutEvents(sink), Is.EqualTo(1), "af_league_debut, una sola vez");
            AnalyticsEvent e = sink.Last("af_league_debut")!;
            Assert.That(e.Find("answer")!.Value.Text, Is.EqualTo(answer));
            Assert.That(e.Find("source")!.Value.Text, Is.EqualTo(source));
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
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
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
            AssertDebutEvent(sink, "later", "lobby"); // Atrás cuenta como «Luego» [P]
            Assert.That(boot.PressLeagueDebutBack(), Is.False, "cerrado, otro Atrás no lo responde otra vez");
            Assert.That(DebutEvents(sink), Is.EqualTo(1));
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
            Assert.That(DebutEvents(sink), Is.EqualTo(1), "ni al volver a abrir la app");

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
            AssertDebutEvent(sink, "open", "lobby");
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
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
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
            AssertDebutEvent(sink, "later", "victory");
        }

        [UnityTest]
        public IEnumerator Victory41_Home_SeeTheLeague_GoesHomeAndOpensIt()
        {
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
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
            AssertDebutEvent(sink, "open", "victory");
        }

        [UnityTest]
        public IEnumerator Victory41_WithThePushPrePrompt_ThePrePromptGoesFirst_TheDebutWaits()
        {
            // 06 §8.1: la 1.ª victoria de la 2.ª sesión pasado el 11 lleva el pre-prompt de push. El estreno no sale justo después
            // (nunca dos pantallas seguidas): espera al siguiente momento tranquilo, aquí la entrada al inicio.
            var clock = new ManualClock(T0, TimeSpan.FromHours(-5));
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.NotificationSchedulerOverride = new FakeNotificationScheduler(clock);
            GameBootstrap.PushSettingsStoreOverride = new InMemoryPushSettingsStore();
            GameBootstrap.SessionStoreOverride = new InMemorySessionStore(new SessionRecord { Sessions = 1, LastSeenAt = T0.AddHours(-20) });
            GameBootstrap.PrePromptCardOverride = true;
            Profile profile = VictorySetup.Profile(100);
            profile.Progress.MaxLevel = 40;
            profile.Progress.NextLevel = 41;
            LaunchOptions.LevelOffsetOverride = 40;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.GetComponent<SessionRunner>().enabled = false;
            boot.GetComponent<LeagueRunner>().enabled = false;
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickSessions();
            Assert.That(boot.PrePromptPending, Is.Not.EqualTo(PrePromptKind.None), "condición: esta victoria lleva el pre-prompt");

            Assert.That(boot.PressHome(), Is.True);
            Assert.That(boot.PrePromptCard!.Visible, Is.True, "primero el pre-prompt");
            Assert.That(boot.LeagueDebutVisible, Is.False);
            boot.PrePromptCard.DeclineButton.onClick.Invoke();
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby), "«Ahora no» sigue a Inicio");
            boot.TickLeague(16f);
            Assert.That(boot.LeagueDebutVisible, Is.False, "ni justo después: esa entrada al inicio no saca otra pantalla");
            Assert.That(boot.LeagueDebutPending, Is.True, "queda pendiente");

            Assert.That(boot.PressSettings(), Is.True);
            boot.TickLeague(16f);
            Assert.That(boot.CloseSettings(), Is.True);
            Tick(boot, 300f);
            Assert.That(boot.LeagueDebutVisible, Is.True, "en la siguiente entrada al inicio");
        }

        [UnityTest]
        public IEnumerator Lobby_WithANotificationToOpen_TheNotificationWins_TheDebutComesNext()
        {
            // D-062: la entrada al inicio que viene de un aviso abre lo que nombra; el estreno espera a la siguiente entrada.
            var clock = new ManualClock(T0, TimeSpan.FromHours(-5));
            LaunchOptions.ClockOverride = clock;
            var scheduler = new FakeNotificationScheduler(clock);
            GameBootstrap.NotificationSchedulerOverride = scheduler;
            GameBootstrap.LogbookOverride = true;
            GameBootstrap.LogbookStoreOverride = new InMemoryLoginCalendarStore(new LoginCalendarRecord { PlayedDate = "2026-11-06" });
            scheduler.SimulateOpenFrom("logbook_pending");
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(45)));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.LogbookRunner!.enabled = false;
            boot.GetComponent<LeagueRunner>().enabled = false;
            boot.TickSessions();
            boot.TickLogbook(16f);
            boot.TickLeague(16f);
            Assert.That(boot.LogbookScreen!.Visible, Is.True, "el aviso abre la libreta");
            Assert.That(boot.LogbookScreen.Source, Is.EqualTo(LogbookOpenSource.Push));
            Assert.That(boot.LeagueDebutVisible, Is.False, "sin el estreno encima");

            Assert.That(boot.PressLogbookBack(), Is.True);
            boot.TickLogbook(16f);
            boot.TickLeague(16f);
            Assert.That(boot.LeagueDebutVisible, Is.False, "cerrar la libreta no es una entrada nueva al inicio");

            Assert.That(boot.PressSettings(), Is.True);
            boot.TickLogbook(16f);
            boot.TickLeague(16f);
            Assert.That(boot.CloseSettings(), Is.True);
            boot.TickLogbook(16f);
            boot.TickLeague(16f);
            Assert.That(boot.LeagueDebutVisible, Is.True, "el estreno, en la siguiente entrada al inicio");
            Assert.That(boot.LogbookScreen.Visible, Is.False, "sin la libreta encima");
        }

        [UnityTest]
        public IEnumerator TalkBack_ReadsOnlyTheDebut_AnnouncesTheTitle_AndTheCardSaysNew()
        {
            var host = new FakeScreenReaderHost(true);
            GameBootstrap.A11yHostOverride = host;
            yield return Lobby(new MemoryProfileStore(ProfileAt(45)));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.LeagueDebutVisible, Is.True);
            Tick(boot, 1200f);
            boot.A11y!.Refresh();
            Assert.That(host.Announcements, Does.Contain(LeagueText.DebutTitle), "TalkBack anuncia el título al salir");
            Assert.That(host.Find(LeagueText.DebutOpen), Is.Not.Null, "«Ver la Liga», con su etiqueta");
            Assert.That(host.Find(LeagueText.DebutLater), Is.Not.Null, "«Luego»");
            Assert.That(host.Find(LeagueText.DebutTitle), Is.Not.Null, "el título se lee");
            Assert.That(host.Find(boot.Lobby!.PlayLabel), Is.Null, "el velo tapa el inicio: lo de debajo no se publica");
            Assert.That(host.Find(UiText.Settings), Is.Null);

            boot.LeagueDebutView!.LaterButton.onClick.Invoke();
            boot.TickLeague(16f);
            boot.A11y.Refresh();
            A11yItem? card = null;
            foreach (A11yItem item in host.Items)
            {
                if (item.Role == UnityEngine.Accessibility.AccessibilityRole.Button && item.Label.StartsWith(LeagueText.Button, StringComparison.Ordinal))
                {
                    card = item;
                }
            }

            Assert.That(card, Is.Not.Null, "la tarjeta «Liga» del inicio");
            Assert.That(card!.Label, Does.EndWith(LeagueText.New), "TalkBack lee «Nueva» tras el título: " + card.Label);
        }

        [UnityTest]
        public IEnumerator Victory41_DoubleHarvestAdvancesAlone_TheDebutShows_ThenLaterGoesOn()
        {
            // D-011, punto 4: tras la Cosecha doble la victoria pasa sola por el camino de Siguiente; el estreno sale ahí y «Luego» sigue.
            FunnelSetup.Hooks();
            LaunchOptions.LevelOffsetOverride = 40;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(VictorySetup.Profile(100));
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.GetComponent<LeagueRunner>().enabled = false;
            GameScreen view = boot.View!;
            FunnelSetup.Clock.Advance(TimeSpan.FromSeconds(200));
            VictorySetup.WinToVictoryScreen(boot);
            PlayModeHelpers.Step(boot, 900);
            Assert.That(view.DoubleHarvestShown, Is.True, "condición: la Cosecha doble se ofrece");
            view.DoubleHarvestButton.onClick.Invoke();
            Assert.That(view.AutoNextPending, Is.True);
            for (int ms = 0; ms < 3000 && !boot.LeagueDebutVisible; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.LeagueDebutVisible, Is.True, "el avance solo pasa por el estreno");
            Assert.That(boot.Flow!.Phase, Is.EqualTo(FlowPhase.VictoryScreen));
            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(41));
            boot.LeagueDebutView!.LaterButton.onClick.Invoke();
            for (int ms = 0; ms < 15000 && boot.Flow.LevelNumber != 42; ms += PlayModeHelpers.FrameMs)
            {
                PlayModeHelpers.Step(boot, PlayModeHelpers.FrameMs);
            }

            Assert.That(boot.Flow.LevelNumber, Is.EqualTo(42), "«Luego» sigue al 42");
        }

        [UnityTest]
        public IEnumerator Victory41_WithASkinOfferForTheLobby_TheOfferWaitsForTheNextVictory()
        {
            // Q-B5-6: la skin del mundo sale al volver al inicio tras la victoria del 41. Con el estreno pendiente, esa victoria no
            // apunta ninguna oferta (como con el pre-prompt): Inicio → estreno → «Luego» → el inicio, sin la tarjeta de oferta.
            yield return WinSkinVictory41();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressHome(), Is.True);
            Assert.That(boot.LeagueDebutVisible, Is.True);
            boot.TickMarket(16f);
            Assert.That(boot.OfferCardView == null || !boot.OfferCardView.Visible, Is.True, "no antes del estreno ni encima");
            boot.LeagueDebutView!.LaterButton.onClick.Invoke();
            boot.TickMarket(16f);
            boot.TickLeague(16f);
            boot.TickMarket(16f);
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            Assert.That(boot.OfferCardView == null || !boot.OfferCardView.Visible, Is.True, "ni justo después: nunca dos pantallas seguidas");

            // Control: sin el estreno, la misma victoria saca la skin al volver al inicio.
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetSessionOverrides();
            GameBootstrap.LeagueDebutOverride = false;
            yield return WinSkinVictory41();
            boot = GameBootstrap.Current!;
            Assert.That(boot.PressHome(), Is.True);
            boot.TickMarket(16f);
            Assert.That(boot.OfferCardView!.Visible, Is.True, "condición: sin el estreno, la oferta sale");
        }

        private static IEnumerator WinSkinVictory41()
        {
            GameBootstrap.SkinSalesOverride = true;
            GameBootstrap.SessionStoreOverride = new InMemorySessionStore(new SessionRecord { Sessions = 1 });
            LaunchOptions.ClockOverride = new ManualClock(T0);
            LaunchOptions.PurchasePresenterOverride = new ImmediatePurchasePresenter();
            Profile profile = Wallet.NewProfile(new ManualClock(T0));
            profile.Progress.MaxLevel = 40;
            profile.Progress.NextLevel = 41;
            profile.Wallet.Seeds = 340;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.SetUpMarket(), Is.True);
            boot.GetComponent<MarketRunner>().enabled = false;
            boot.GetComponent<LeagueRunner>().enabled = false;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            boot.TickMarket(16f);
            boot.TickMarket(GameBootstrap.OfferCardDelayMs + 16f);
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
