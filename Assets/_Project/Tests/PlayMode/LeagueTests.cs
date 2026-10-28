#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Analytics;
using AF.Economy;
using AF.Core.Rules;
using AF.GameFlow;
using AF.Solver.Search;
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
    /// La Liga del río de la 1.0 en la escena <c>Game</c> real (D-046; liga.md §1 y §4), con la API falsa (<see cref="FakeLeagueApi"/>):
    /// el botón del inicio desde el 41 y solo con <c>live_league_enabled</c>; la pantalla con el top, la fila del jugador y las dos
    /// pestañas; sin conexión; la banda &lt; 13 (ni botón ni red); la línea «+{n} puntos de Liga» de la victoria y su envío; la fila
    /// de Ajustes; TalkBack en cada fila; y que todo cabe al 130 % en es, en y pt-BR.
    /// </summary>
    public sealed class LeagueTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc);

        private ManualClock _clock = null!;
        private FakeLeagueApi _api = null!;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(T0);
            _api = new FakeLeagueApi(_clock) { StartPoints = 140 };
            GameBootstrap.LeagueApiOverride = _api;
            GameBootstrap.LeagueClockOverride = _clock;
            GameBootstrap.LeagueConfigOverride = new LeagueConfig { LiveLeagueEnabled = true };
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.ResetLeagueOverrides();
            GameBootstrap.ResetBackendOverrides();
            GameBootstrap.ResetTelemetryOverrides();
            GameBootstrap.ResetMarketOverrides();
            GameBootstrap.ResetDailyOverrides();
            GameBootstrap.ResetAccountOverrides();
            GameBootstrap.WipePlayerPrefsForTests = null;
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

        private static IEnumerator Lobby(int maxLevel)
        {
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(ProfileAt(maxLevel)));
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<LeagueRunner>().enabled = false; // el test avanza la Liga a mano
            boot.TickLeague(16f);
        }

        [UnityTest]
        public IEnumerator Lobby_ShowsTheButtonFrom41_OnlyWithTheSwitch()
        {
            yield return Lobby(40);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.LeagueButton!.Shown, Is.False, "hasta ganar el 41, nada (liga.md §1)");
            Assert.That(boot.OpenLeague(), Is.False);

            yield return Lobby(41);
            boot = GameBootstrap.Current!;
            Assert.That(boot.LeagueButton!.Shown, Is.True);
            Assert.That(boot.LeagueButton.Label, Is.EqualTo("Liga"));
            Assert.That(boot.LeagueButton.Rect.rect.height, Is.GreaterThanOrEqualTo(48f), "área táctil (07 §7.8)");
            Assert.That(boot.Lobby!.SettingsButton.gameObject.activeInHierarchy, Is.True, "el engranaje sigue en su sitio");

            GameBootstrap.LeagueConfigOverride = new LeagueConfig { LiveLeagueEnabled = false };
            yield return Lobby(60);
            boot = GameBootstrap.Current!;
            Assert.That(boot.LeagueButton!.Shown, Is.False, "live_league_enabled apagada (el default de 08 §9)");
            Assert.That(_api.BoardCalls + _api.WinsCalls, Is.EqualTo(0), "sin Liga, ni red");
        }

        [UnityTest]
        public IEnumerator DevExtra_TurnsTheLeagueOn_WithoutRemoteConfig()
        {
            GameBootstrap.LeagueConfigOverride = null; // las cifras de Remote Config: apagada
            LaunchOptions.LeagueEnabledOverride = true; // af_league
            yield return Lobby(41);
            Assert.That(GameBootstrap.Current!.LeagueButton!.Shown, Is.True, "el extra de la sonda la enciende en desarrollo");
            Assert.That(LaunchOptions.ResolveLeagueEnabled(false), Is.False, "nunca en release");
            Assert.That(LaunchOptions.ResolveLeagueFake(false), Is.False);
        }

        [UnityTest]
        public IEnumerator Screen_WithTheFake_ShowsTheTop_MyRow_AndBothTabs()
        {
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
            yield return Lobby(57);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.LeagueButton!.Button.onClick.Invoke();
            Assert.That(boot.LeagueOpen, Is.True);
            yield return null;
            LeagueScreen screen = boot.LeagueView!;
            Assert.That(screen.Scope, Is.EqualTo(LeagueScope.Global));
            Assert.That(screen.RowsShown, Is.EqualTo(100), "el top 100");
            Assert.That(screen.WeekText, Is.EqualTo(LeagueText.Week(new DateTime(2026, 10, 19), new DateTime(2026, 10, 26))));
            Assert.That(screen.EndsText, Does.StartWith("Termina en 2"));
            Assert.That(screen.RowName(0), Is.EqualTo(LeagueText.Player(LeagueRules.NameKeys[0], 1000)));
            Assert.That(screen.RowFlag(0), Is.Not.Null, "en Global, la bandera");
            Assert.That(screen.RowCountry(0), Is.EqualTo(LeagueText.Country(screen.RowFlag(0)!)), "siempre con el nombre del país al lado");
            Assert.That(screen.RowName(37), Is.EqualTo(LeagueText.Player(null, 1000 + ((37 * 397) % 9000))), "un animal que el cliente no conoce: «Jugador {n}»");
            Assert.That(screen.MeShown, Is.True, "la fila del jugador, fija abajo");
            Assert.That(screen.MeDetailText, Is.EqualTo(LeagueText.Percentile(86)), "fuera del top: «Estás en el {p} % mejor»");
            Assert.That(screen.MePointsText, Is.EqualTo("140"));
            Assert.That(screen.CountryTabShown, Is.True, "el país de la semana: la pestaña {país}");
            AnalyticsEvent open = sink.Last("af_league_open")!;
            Assert.That(open.Find("scope")!.Value.Text, Is.EqualTo("global"));

            screen.CountryTabButton.onClick.Invoke();
            Assert.That(screen.Scope, Is.EqualTo(LeagueScope.Country));
            Assert.That(screen.RowFlag(0), Is.Null, "en la pestaña del país, sin bandera por fila");
            Assert.That(screen.RowCountry(0), Is.Empty);
            Assert.That(sink.Last("af_league_open")!.Find("scope")!.Value.Text, Is.EqualTo("country"));

            Assert.That(boot.PressAndroidBack(), Is.True, "Atrás cierra la Liga");
            Assert.That(boot.LeagueOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator Screen_InTheTop_ShowsTheExactRank()
        {
            _api.StartPoints = 0;
            _api.TopPoints = 50;
            yield return Lobby(57);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.League!.RecordCampaignWin(57, RewardTier.VeryHard); // 10 en el servidor falso (todo Normal)
            boot.League.RecordCampaignWin(56, RewardTier.Normal);
            boot.League.RecordCampaignWin(55, RewardTier.Normal);
            boot.League.RecordCampaignWin(54, RewardTier.Normal);
            Assert.That(boot.OpenLeague(), Is.True);
            LeagueScreen screen = boot.LeagueView!;
            Assert.That(screen.MeRankText, Is.EqualTo("2"), "con 40 puntos, la posición exacta");
            Assert.That(screen.RowA11y(-1), Does.StartWith("Tú, puesto 2:"));
        }

        [UnityTest]
        public IEnumerator MyRow_SaysYouOnce_WithTheAliasAsTheName()
        {
            // Observación 5 de la jornada 4 (2026-10-27): sin seudónimo la fila decía «Tú · Tú». Ahora el nombre es el seudónimo y la
            // chapa, «Tú» (league_you: «junto a su seudónimo»); sin seudónimo, solo la chapa.
            _api.StartPoints = 0;
            yield return Lobby(57);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenLeague(), Is.True);
            LeagueScreen screen = boot.LeagueView!;
            Assert.That(screen.MeDetailText, Is.EqualTo(LeagueText.NoPoints), "«Gana un nivel para entrar en la Liga»");
            Assert.That(screen.MeNameText, Is.Empty, "sin seudónimo, sin nombre");
            Assert.That(screen.MePillText, Is.EqualTo(LeagueText.You));
            Assert.That(YouCount(screen), Is.EqualTo(1), "«Tú» una sola vez");
        }

        [UnityTest]
        public IEnumerator MyRow_WithAnAlias_ShowsItAsTheName_AndYouAsTheBadge()
        {
            yield return Lobby(57); // 140 puntos: ya tiene seudónimo
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenLeague(), Is.True);
            LeagueScreen screen = boot.LeagueView!;
            Assert.That(screen.MeNameText, Is.EqualTo(LeagueText.Player(_api.NameKey, _api.NameNum)), "el seudónimo como nombre");
            Assert.That(screen.MePillText, Is.EqualTo(LeagueText.You), "y «Tú» como chapa");
            Assert.That(YouCount(screen), Is.EqualTo(1));
        }

        private static int YouCount(LeagueScreen screen)
        {
            int n = screen.MePillText == LeagueText.You ? 1 : 0;
            foreach (TMPro.TextMeshProUGUI text in screen.TextsOf(-1))
            {
                if (text.gameObject.activeInHierarchy && text.text == LeagueText.You)
                {
                    n++;
                }
            }

            return n;
        }

        [UnityTest]
        public IEnumerator Screen_Offline_SaysConnect_OrShowsTheLastOneWithItsAge()
        {
            _api.Offline = true;
            var sink = new FakeAnalyticsSink();
            GameBootstrap.AnalyticsSinkOverride = sink;
            yield return Lobby(57);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenLeague(), Is.True);
            LeagueScreen screen = boot.LeagueView!;
            Assert.That(screen.MessageText, Is.EqualTo(LeagueText.Offline), "sin nada guardado: «Conéctate para ver la Liga»");
            Assert.That(screen.RowsShown, Is.EqualTo(0));
            Assert.That(screen.MeShown, Is.False);
            Assert.That(sink.Last("af_league_unavailable")!.Find("reason")!.Value.Text, Is.EqualTo("offline"));
            boot.CloseLeague();
            Assert.That(boot.OpenLeague(), Is.True);
            boot.CloseLeague();
            Assert.That(CountOf(sink, "af_league_unavailable"), Is.EqualTo(1), "el mismo motivo no se repite en cada reintento");

            _api.Offline = false;
            Assert.That(boot.OpenLeague(), Is.True);
            Assert.That(screen.RowsShown, Is.EqualTo(100));
            Assert.That(screen.StatusText, Is.Empty, "recién descargada, sin «Actualizado hace…»");
            boot.CloseLeague();

            _api.Offline = true;
            _clock.Advance(TimeSpan.FromMinutes(45));
            Assert.That(boot.OpenLeague(), Is.True);
            Assert.That(screen.RowsShown, Is.EqualTo(100), "sin conexión, lo último que se descargó");
            Assert.That(screen.StatusText, Is.EqualTo(LeagueText.UpdatedAgo(TimeSpan.FromMinutes(45))));
        }

        [UnityTest]
        public IEnumerator Under13_NoButton_NoNetwork()
        {
            GameBootstrap.AgeUnder13Override = true;
            yield return Lobby(60);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.LeagueButton!.Shown, Is.False, "con <13 no hay Liga (D-033)");
            Assert.That(boot.OpenLeague(), Is.False);
            Assert.That(boot.League!.RecordCampaignWin(60, RewardTier.Normal), Is.EqualTo(0));
            Assert.That(_api.BoardCalls + _api.WinsCalls, Is.EqualTo(0), "ni red");
        }

        [UnityTest]
        public IEnumerator Victory_From41_ShowsItsLine_AndSendsTheWin()
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
            VictorySetup.WinToVictoryScreen(boot);
            GameScreen view = boot.View!;
            int points = LeagueRules.CampaignPoints(boot.Flow!.HarvestTier);
            Assert.That(view.LeagueLineText, Is.EqualTo(LeagueText.VictoryPoints(points)), "«+{n} puntos de Liga» desde el 41");
            Assert.That(_api.WinsCalls, Is.EqualTo(1));
            Assert.That(_api.LastRequest!.Wins[0].WinId, Is.EqualTo("c:41"));
            Assert.That(boot.League!.PendingCount, Is.EqualTo(0));
            AnalyticsEvent sent = sink.Last("af_league_wins_sent")!;
            Assert.That(sent.Find("accepted")!.Value.Long, Is.EqualTo(1));
            Assert.That(boot.Profile!.League, Does.Contain("c:41"), "la cola y lo contado, en el perfil");
        }

        [UnityTest]
        public IEnumerator Victory_WithoutTheLeague_HasNoLine()
        {
            GameBootstrap.LeagueConfigOverride = new LeagueConfig { LiveLeagueEnabled = false };
            FunnelSetup.Hooks();
            LaunchOptions.LevelOffsetOverride = 40;
            Profile profile = VictorySetup.Profile(100);
            profile.Progress.MaxLevel = 40;
            profile.Progress.NextLevel = 41;
            LaunchOptions.ProfileStoreOverride = new MemoryProfileStore(profile);
            yield return PlayModeHelpers.LoadGame(41, new MemoryProgressStore(41, 40));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            VictorySetup.WinToVictoryScreen(boot);
            Assert.That(boot.View!.LeagueLineShown, Is.False);
            Assert.That(boot.View.LeagueLineRect == null || !boot.View.LeagueLineRect.gameObject.activeSelf, Is.True);
            Assert.That(_api.WinsCalls, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator DailyWin_WithTheLeagueOn_QueuesTheDayWinId()
        {
            // El Reto del río ganado (liga.md §1: 20 puntos, una vez por día): d:<fecha de inicio de la partida> en la cola.
            TimeSpan bogota = TimeSpan.FromHours(-5);
            var clock = new ManualClock(new DateTime(2026, 10, 19, 15, 0, 0, DateTimeKind.Utc), bogota); // lunes 10:00 en Bogotá, ya en la W43
            _api = new FakeLeagueApi(clock) { Offline = true };
            GameBootstrap.LeagueApiOverride = _api;
            GameBootstrap.LeagueClockOverride = clock;
            LaunchOptions.ClockOverride = clock;
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            Profile profile = Wallet.NewProfile(clock);
            profile.Progress.MaxLevel = 45;
            profile.Progress.NextLevel = 46;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.SetUpDaily(), Is.True);
            boot.DailyRunner!.enabled = false;
            Assert.That(boot.League!.Active, Is.True);
            boot.DailyCard!.Button.onClick.Invoke();
            DailyChallengeScreen daily = boot.DailyScreen!;
            LevelFlow flow = daily.Flow!;
            daily.Actions.TapAnywhere();
            StepDaily(boot, () => flow.Phase == FlowPhase.Playing && flow.AcceptsBoardInput, "fin de la intro del Reto");
            SolveResult solution = new AStarSolver().Solve(flow.Session.Current, SolverOptions.ForPipeline(flow.Session.PourMode));
            Assert.That(solution.IsSolved, Is.True);
            for (int i = 0; i < solution.MoveCount; i++)
            {
                Pour move = solution.GetMove(i);
                StepDaily(boot, () => flow.AcceptsBoardInput, "tablero listo");
                daily.Actions.TapCuia(move.Source);
                daily.Actions.TapCuia(move.Target);
            }

            StepDaily(boot, () => daily.Won, "victoria del Reto");
            LeagueRecord record = boot.League.Record;
            Assert.That(record.Pending.Count, Is.EqualTo(1), "sin red, en la cola");
            Assert.That(record.Pending[0].WinId, Is.EqualTo("d:2026-10-19"));
            Assert.That(record.Pending[0].Kind, Is.EqualTo(LeagueWinKind.Daily));
            Assert.That(boot.Profile!.League, Does.Contain("d:2026-10-19"), "y en el perfil");
        }

        [UnityTest]
        public IEnumerator DeleteAccount_EmptiesTheQueueTheCacheAndTheProfileBlock()
        {
            int wipes = 0;
            GameBootstrap.WipePlayerPrefsForTests = () => wipes++;
            var store = new MemoryProfileStore(ProfileAt(57));
            yield return PlayModeHelpers.LoadApp(store);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.GetComponent<LeagueRunner>().enabled = false;
            LeagueService league = boot.League!;
            Assert.That(league.RequestBoard(LeagueScope.Global), Is.True);
            _api.Offline = true;
            league.RecordCampaignWin(57, RewardTier.Normal);
            Assert.That(league.PendingCount, Is.EqualTo(1));
            Assert.That(league.Cached(LeagueScope.Global), Is.Not.Null);
            Assert.That(boot.Profile!.League, Is.Not.Null);
            Profile profile = boot.Profile;

            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.OpenDeleteAccount(), Is.True);
            Assert.That(boot.ContinueDeleteAccount(), Is.True);
            Assert.That(boot.ConfirmDeleteAccount(), Is.True);
            Assert.That(boot.AccountDeleted, Is.True);
            Assert.That(league.PendingCount, Is.EqualTo(0), "la cola");
            Assert.That(league.Cached(LeagueScope.Global), Is.Null, "la caché");
            Assert.That(profile.League, Is.Null, "el bloque league del perfil");
            Assert.That(store.Json, Is.Null, "y el perfil del disco");
            int calls = _api.WinsCalls;
            Assert.That(league.Flush(), Is.False, "nada más se envía");
            Assert.That(_api.WinsCalls, Is.EqualTo(calls));
            yield return PlayModeHelpers.WaitFor(
                () => GameBootstrap.Current != null && GameBootstrap.Current != boot && GameBootstrap.Current.App != null && GameBootstrap.Current.App.Screen != AppScreen.Boot,
                "arranque en frío tras eliminar la cuenta");
        }

        private static int CountOf(FakeAnalyticsSink sink, string name)
        {
            int n = 0;
            foreach (AnalyticsEvent e in sink.Events)
            {
                if (e.Name == name)
                {
                    n++;
                }
            }

            return n;
        }

        private static void StepDaily(GameBootstrap boot, Func<bool> done, string what)
        {
            for (int t = 0; t < 20000 && !done(); t += PlayModeHelpers.FrameMs)
            {
                boot.TickDaily(PlayModeHelpers.FrameMs);
            }

            Assert.That(done(), Is.True, "tiempo agotado: " + what);
        }

        [UnityTest]
        public IEnumerator Settings_ShowsTheLeagueCountry()
        {
            yield return Lobby(57);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.SettingsView!.LeagueCountryText, Is.Null, "sin envío aceptado aún, el país no se sabe");
            boot.CloseSettings();
            boot.League!.RecordCampaignWin(57, RewardTier.Normal);
            Assert.That(boot.PressSettings(), Is.True);
            Assert.That(boot.SettingsView!.LeagueCountryText, Is.EqualTo("País en la Liga: Colombia"));
        }

        [UnityTest]
        public IEnumerator TalkBack_ReadsEachRowWhole()
        {
            yield return Lobby(57);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.OpenLeague(), Is.True);
            yield return null;
            Canvas.ForceUpdateCanvases();
            LeagueScreen screen = boot.LeagueView!;
            string first = screen.RowA11y(0);
            Assert.That(first, Does.StartWith("Puesto 1: "));
            Assert.That(first, Does.Contain(LeagueText.Country(screen.RowFlag(0)!)), "con el país en Global");

            var items = new List<A11yItem>();
            new A11yScanner().Scan(items);
            var labels = new List<string>();
            foreach (A11yItem item in items)
            {
                labels.Add(item.Label);
            }

            Assert.That(labels, Does.Contain(first), "la fila, en un solo elemento");
            Assert.That(labels, Does.Contain(screen.RowA11y(99)), "también las de abajo del visor (Q-B9-17)");
            Assert.That(labels, Does.Contain(screen.RowA11y(-1)), "y la fila del jugador");
            Assert.That(labels, Does.Not.Contain(screen.RowName(0)), "el nombre suelto no se lee aparte");
            Assert.That(labels.FindAll(l => l == LeagueText.TabGlobal).Count, Is.EqualTo(1), "las pestañas, como botones");
        }

        [UnityTest]
        public IEnumerator At130_EverythingFits_InEsEnAndPt([Values(LocLocale.SourceLocale, LocLocale.English, LocLocale.PortugueseBrazil)] string locale)
        {
            LaunchOptions.LanguageOverride = locale;
            LaunchOptions.FontScaleOverride = 1.3f;
            GameScreen.SimulatedScreen = HudLabelTests.Screen640;
            var inventory = new LocInventory(locale, 1.3f);
            yield return Lobby(57);
            GameBootstrap boot = GameBootstrap.Current!;
            yield return SnapshotKit.Wait(0.2f);
            inventory.Scan("league_lobby");
            Assert.That(boot.OpenLeague(), Is.True);
            yield return SnapshotKit.Wait(0.2f);
            inventory.Scan("league_global");
            boot.LeagueView!.CountryTabButton.onClick.Invoke();
            yield return SnapshotKit.Wait(0.2f);
            inventory.Scan("league_country");
            boot.CloseLeague();
            boot.League!.RecordCampaignWin(57, RewardTier.Normal);
            Assert.That(boot.PressSettings(), Is.True);
            yield return SnapshotKit.Wait(0.2f);
            inventory.Scan("league_settings");

            var mine = new List<string>();
            foreach (string line in inventory.Lines)
            {
                if (line.Contains("League"))
                {
                    mine.Add(line);
                }
            }

            Assert.That(mine, Is.Empty, "textos de la Liga que se salen o bajan de 12 sp en " + locale + " al 130 %:\n" + string.Join("\n", mine));
        }
    }
}
