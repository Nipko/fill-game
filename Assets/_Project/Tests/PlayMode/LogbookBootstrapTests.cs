#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.Localization;
using AF.LiveOps;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// La Bitácora en el arranque (B4; <c>GameBootstrap.Logbook.cs</c>; 06 §4.2 y §4.7; D-033, añadido del 2026-10-20, puntos 4-6
    /// y 11) con la escena real y un almacén en memoria: la tarjeta con punto, el takeover al entrar al inicio (uno por sesión), el
    /// sello con la cartera del perfil en una escritura y la libreta antes del Mapa del 10. <c>SetUpLogbook</c> se llama a mano
    /// (en la integración lo llama <c>Start</c>: parche B4-3-bootstrap); el día jugado se siembra en el registro (en la
    /// integración lo apunta el fin de nivel: parche B4-2-flow, probado allí).
    /// </summary>
    public sealed class LogbookBootstrapTests
    {
        private const int MaxLevel = 25;
        private const int Frame = PlayModeHelpers.FrameMs;
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime Tuesday = new DateTime(2026, 10, 20, 10, 0, 0); // hora local de Bogotá

        private ManualClock _clock = null!;
        private MemoryProfileStore _profiles = null!;
        private readonly List<string> _lines = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(Utc(Tuesday), Bogota);
            _profiles = new MemoryProfileStore(ProfileAt(MaxLevel));
            LaunchOptions.ClockOverride = _clock;
            _lines.Clear();
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            GameBootstrap.ResetLogbookOverrides();
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator WithoutALevelToday_TheCardWaits_AndNothingOpensAlone()
        {
            yield return Boot(playedToday: false);
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, 5 * Frame);

            LogbookCard card = boot.LogbookCard!;
            Assert.That(card.Visible, Is.True);
            Assert.That(card.State, Is.EqualTo(LoginCalendarState.Waiting));
            Assert.That(card.DotShown, Is.False);
            Assert.That(boot.LogbookScreen!.Visible, Is.False);
            Assert.That(boot.LastTakeover, Is.EqualTo(TakeoverKind.None));
            Assert.That(boot.Takeovers!.ShownThisSession, Is.EqualTo(0));
            Assert.That(Find("\"event\":\"takeover\""), Does.Contain("\"kind\":\"none\",\"shown\":false"));
        }

        [UnityTest]
        public IEnumerator WithAPagePending_TheNotebookOpensAloneOncePerSession_AndATapSeals()
        {
            yield return Boot(playedToday: true);
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);

            LogbookScreen screen = boot.LogbookScreen!;
            Assert.That(screen.Visible, Is.True, "el único takeover de la sesión al entrar al inicio (D-033, punto 5)");
            Assert.That(screen.Source, Is.EqualTo(LogbookOpenSource.Takeover));
            Assert.That(boot.LastTakeover, Is.EqualTo(TakeoverKind.Logbook));
            Assert.That(boot.Takeovers!.ShownThisSession, Is.EqualTo(1));
            Assert.That(boot.LogbookCard!.Visible, Is.False, "la tarjeta no se ve con la libreta encima");

            screen.CloseButton.onClick.Invoke();
            Step(boot, 5 * Frame);
            Assert.That(screen.Visible, Is.False, "cerrar sin elegir: no vuelve a salir sola");
            Assert.That(boot.LogbookCard.Visible, Is.True);
            Assert.That(boot.LogbookCard.DotShown, Is.True, "la casilla sigue pendiente: punto");

            boot.LogbookPause(true);
            _clock.Advance(TimeSpan.FromMinutes(10));
            boot.LogbookPause(false);
            Step(boot, Frame);
            Assert.That(screen.Visible, Is.False, "menos de 30 min fuera: la misma sesión, sin otro takeover");

            Profile profile = boot.Profile!;
            int seeds = profile.Wallet.Seeds;
            Assert.That(boot.PressLogbookCard(), Is.True);
            Assert.That(screen.Source, Is.EqualTo(LogbookOpenSource.Card));
            Assert.That(screen.PressToday(), Is.True);
            screen.CardButton(LoginCalendarChoice.Seeds).onClick.Invoke();
            Assert.That(profile.Wallet.Seeds, Is.EqualTo(seeds + 60));
            Profile onDisk = ProfileJson.Deserialize(_profiles.Json!);
            Assert.That(onDisk.Wallet.Seeds, Is.EqualTo(seeds + 60), "el cobro ya está en disco");
            Assert.That(boot.Logbook!.Record.NextDay, Is.EqualTo(2));
            Assert.That(Find("\"event\":\"calendar_claim\""), Does.Contain("\"day\":1,\"reward_type\":\"seeds\",\"reward_value\":60,\"days_missed_before\":0,\"choice\":\"seeds\",\"cycle\":1"));

            screen.CloseButton.onClick.Invoke();
            Step(boot, 5 * Frame);
            Assert.That(boot.LogbookCard.State, Is.EqualTo(LoginCalendarState.Sealed));
            Assert.That(boot.LogbookCard.DotShown, Is.False);
            Assert.That(boot.LogbookCard.StateText, Is.EqualTo("Sellada hoy"));
        }

        [UnityTest]
        public IEnumerator ANewSession_AfterThirtyMinutes_CanOpenItAgain()
        {
            yield return Boot(playedToday: true);
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);
            boot.LogbookScreen!.CloseButton.onClick.Invoke();
            Step(boot, Frame);

            boot.LogbookPause(true);
            _clock.Advance(TimeSpan.FromMinutes(31));
            boot.LogbookPause(false);
            Step(boot, Frame);
            Assert.That(boot.Takeovers!.Session, Is.EqualTo(2), "Q-M4 (a): vuelta tras ≥ 30 min");
            Assert.That(boot.LogbookScreen.Visible, Is.True);
            Assert.That(boot.LogbookScreen.Source, Is.EqualTo(LogbookOpenSource.Takeover));
        }

        [UnityTest]
        public IEnumerator TheLevel10Sequence_OpensTheNotebookBeforeTheMap_WithoutSpendingTheQuota()
        {
            yield return Boot(playedToday: true, maxLevel: 10);
            GameBootstrap boot = GameBootstrap.Current!;
            boot.LogbookRunner!.enabled = false;
            Assert.That(boot.OpenLogbookBeforeL10Map(), Is.True);
            LogbookScreen screen = boot.LogbookScreen!;
            Assert.That(screen.Source, Is.EqualTo(LogbookOpenSource.L10));
            Assert.That(screen.IntroShown, Is.True);
            Assert.That(screen.ButtonText, Is.EqualTo("Seguir"));
            Assert.That(boot.Takeovers!.ShownThisSession, Is.EqualTo(0), "dentro de la secuencia de hito no gasta el cupo");
            Assert.That(screen.PressToday(), Is.True);
            screen.CardButton(LoginCalendarChoice.Booster).onClick.Invoke();
            Assert.That(boot.Logbook!.LastClaim!.Reward.ToJson(), Is.EqualTo("{\"undo\":2}"));

            screen.CloseButton.onClick.Invoke();
            Assert.That(screen.Visible, Is.False);
            if (boot.RiverMapAvailable)
            {
                Assert.That(boot.RiverMapView!.Visible, Is.True, "Victoria → libreta → Mapa → Inicio (D-033, punto 11)");
            }

            Assert.That(boot.OpenLogbookBeforeL10Map(), Is.False, "sellada hoy: el Mapa sale directo");
        }

        [UnityTest]
        public IEnumerator BeforeLevel10_TheLogbookDoesNotExist()
        {
            yield return Boot(playedToday: true, maxLevel: 9);
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, 5 * Frame);
            Assert.That(boot.LogbookCard!.Visible, Is.False, "D-008: oculta antes de live_unlock_level_daily");
            Assert.That(boot.LogbookScreen!.Visible, Is.False);
            Assert.That(boot.OpenLogbook(LogbookOpenSource.Card), Is.False);
            Assert.That(boot.LogbookVictoryRowDue, Is.False);
        }

        // ---- Ayudas ----

        private IEnumerator Boot(bool playedToday, int maxLevel = MaxLevel)
        {
            if (maxLevel != MaxLevel)
            {
                _profiles = new MemoryProfileStore(ProfileAt(maxLevel));
            }

            var record = new LoginCalendarRecord { PlayedDate = playedToday ? "2026-10-20" : null };
            GameBootstrap.LogbookStoreOverride = new InMemoryLoginCalendarStore(record);
            yield return PlayModeHelpers.LoadApp(_profiles);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            PlayModeHelpers.TakeClock(boot);
            Loc.Configure(LogbookTestTables.Catalog(), LocLocale.SourceLocale, true);
            Assert.That(boot.SetUpLogbook(), Is.True);
            Assert.That(boot.SetUpLogbook(), Is.True, "una segunda llamada no hace nada");
            boot.LogbookRunner!.enabled = false;
        }

        private static void Step(GameBootstrap boot, int ms)
        {
            for (int t = 0; t < ms; t += Frame)
            {
                boot.TickLogbook(Frame);
            }
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (condition.StartsWith("AF_STATE ", StringComparison.Ordinal))
            {
                _lines.Add(condition);
            }
        }

        private string Find(string fragment)
        {
            foreach (string line in _lines)
            {
                if (line.Contains(fragment))
                {
                    return line;
                }
            }

            Assert.Fail("no hay ninguna línea con " + fragment);
            return "";
        }

        private static Profile ProfileAt(int maxLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(Utc(Tuesday), Bogota));
            profile.Progress.MaxLevel = maxLevel;
            profile.Progress.NextLevel = maxLevel + 1;
            profile.Progress.MapSeenLevel = maxLevel; // sin la secuencia del Mapa del 10 al entrar al inicio
            return profile;
        }

        private static DateTime Utc(DateTime bogota)
        {
            return DateTime.SpecifyKind(bogota - Bogota, DateTimeKind.Utc);
        }
    }
}
