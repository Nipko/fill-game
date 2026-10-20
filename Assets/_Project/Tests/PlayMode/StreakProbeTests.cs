#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
using AF.Platform;
using AF.Save;
using AF.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.PlayMode
{
    /// <summary>
    /// Cableado de las sondas de la Crecida (B1a, A7; nota <c>docs/dev/crecida-canasta.md</c> §5 y E32): <see cref="GameBootstrap.SetUpStreak"/>
    /// tras el arranque y <see cref="GameBootstrap.TickStreakProbe"/> a mano, con el runner apagado, como el Reto en
    /// <c>DailyProbeTests</c>. El formato exacto de cada línea lo prueba <c>StreakProbeFormatTests</c> (EditMode); aquí, que se
    /// escriben, cuántas veces y cuándo. Vale con y sin el parche S3 (que ya llama a <see cref="GameBootstrap.SetUpStreak"/> en
    /// <c>Start</c>, con los mismos ganchos de test): el log se escucha desde antes del arranque.
    /// </summary>
    public sealed class StreakProbeTests
    {
        private const int MaxLevel = 29;
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime Monday = new DateTime(2026, 10, 19, 10, 0, 0); // hora local de Bogotá

        private ManualClock _clock = null!;
        private InMemoryStreakStore _store = null!;
        private MemoryProfileStore _profiles = null!;
        private readonly List<string> _lines = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(Utc(Monday), Bogota);
            var record = new StreakRecord { DateLocal = "2026-10-19", Wins = 2, ShieldAvailable = false };
            _store = new InMemoryStreakStore(_clock, record);
            _profiles = new MemoryProfileStore(ProfileAt(MaxLevel));
            LaunchOptions.ClockOverride = _clock;
            GameBootstrap.StreakStoreOverride = _store;
            _lines.Clear();
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            GameBootstrap.ResetStreakOverrides();
            GameBootstrap.ResetDailyOverrides();
            GameScreen.SimulatedScreen = null;
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator SetUp_StartOnce_ThenEachResultOnce_ThenTheDayOnce()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(DevProbe.Enabled, Is.True, "en el Editor la sonda escribe");
            StreakService? service = boot.StreakService;
            Assert.That(service, Is.Not.Null, "E32");
            Assert.That(ReferenceEquals(boot.Flow!.Streak, service), Is.True, "inyectado en el flujo de campaña");

            // streak_state de partida, una vez, para max_level + 1, con el almacén del test.
            Assert.That(Count("streak_state"), Is.EqualTo(1));
            Assert.That(
                Only("streak_state"),
                Does.Match("^AF_STATE \\{\"event\":\"streak_state\",\"t\":\\d+,\"cause\":\"start\",\"level\":30,\"wins\":2,"
                    + "\"river_full\":false,\"shield\":false,\"hits\":\\[\\],\"seeds_today\":0,\"date\":\"2026-10-19\","
                    + "\"enabled\":true,\"active\":true,\"free_double_next\":false\\}$"));
            Assert.That(boot.GetComponents<StreakProbeRunner>().Length, Is.EqualTo(1), "un solo runner");
            Assert.That(boot.StreakRunner, Is.Not.Null);

            // Una segunda llamada no duplica nada.
            Assert.That(boot.SetUpStreak(), Is.True);
            Assert.That(ReferenceEquals(boot.StreakService, service), Is.True, "el mismo servicio");
            Assert.That(boot.GetComponents<StreakProbeRunner>().Length, Is.EqualTo(1));
            Assert.That(Count("streak_state"), Is.EqualTo(1), "sin otro streak_state de partida");

            // Mientras nada cambia, nada.
            _lines.Clear();
            Assert.That(boot.TickStreakProbe(), Is.EqualTo(0));
            Assert.That(boot.TickStreakProbe(), Is.EqualTo(0));
            Assert.That(_lines, Is.Empty);

            // Un resultado (Salto del Boto en el 30: su Canasta, sin tocar la Crecida) sale una sola vez.
            Assert.That(boot.PressPlay(), Is.True, "Jugar: el 30 (max_level + 1)");
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(30));
            StreakServiceResult? skip = boot.Flow.OnSkipStreak();
            Assert.That(skip, Is.Not.Null);
            Assert.That(ReferenceEquals(boot.Flow.LastStreakResult, skip), Is.True);
            _lines.Clear();
            Assert.That(boot.TickStreakProbe(), Is.EqualTo(1));
            Assert.That(Count("basket_open"), Is.EqualTo(1));
            Assert.That(Only("basket_open"), Does.Contain("\"level\":30,"));
            Assert.That(Count("streak_state"), Is.EqualTo(0), "el Salto no cambia el estado (C11)");
            Assert.That(boot.TickStreakProbe(), Is.EqualTo(0), "el mismo LastStreakResult no se escribe dos veces");
            Assert.That(Count("basket_open"), Is.EqualTo(1));

            // Cambio de día: streak_state con cause = day, una vez, para max_level + 1 (el Salto no guarda el progreso aquí).
            _lines.Clear();
            _clock.Advance(TimeSpan.FromDays(1));
            Assert.That(boot.TickStreakProbe(), Is.EqualTo(1));
            Assert.That(
                Only("streak_state"),
                Does.Match("^AF_STATE \\{\"event\":\"streak_state\",\"t\":\\d+,\"cause\":\"day\",\"level\":30,\"wins\":2,"
                    + "\"river_full\":false,\"shield\":true,\"hits\":\\[\\],\"seeds_today\":0,\"date\":\"2026-10-20\","),
                "día nuevo: la hoja vuelve");
            Assert.That(boot.TickStreakProbe(), Is.EqualTo(0), "cause = day, una sola vez por día");
            Assert.That(Count("streak_state"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator WithoutTheProbe_NoRunner_AndNoLines()
        {
            GameBootstrap.StreakProbeOverride = false; // el camino de release: sin sonda
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;

            Assert.That(boot.StreakService, Is.Not.Null, "la Crecida sigue, sin sonda");
            Assert.That(boot.StreakRunner, Is.Null);
            Assert.That(boot.GetComponent<StreakProbeRunner>(), Is.Null, "sin DevProbe.Enabled no hay runner");
            Assert.That(Count("streak_state"), Is.EqualTo(0));

            Assert.That(boot.PressPlay(), Is.True, "Jugar: el 30 (max_level + 1)");
            Assert.That(boot.Flow!.LevelNumber, Is.EqualTo(30));
            Assert.That(boot.Flow!.OnSkipStreak(), Is.Not.Null);
            _clock.Advance(TimeSpan.FromDays(1));
            _lines.Clear();
            Assert.That(boot.TickStreakProbe(), Is.EqualTo(0));
            Assert.That(_lines, Is.Empty);
        }

        // ---- Ayudas ----

        private IEnumerator Boot()
        {
            yield return PlayModeHelpers.LoadApp(_profiles);
            GameBootstrap boot = GameBootstrap.Current!;
            Assert.That(boot.App!.Screen, Is.EqualTo(AppScreen.Lobby));
            PlayModeHelpers.TakeClock(boot);
            Assert.That(boot.SetUpStreak(), Is.True, "E32 (con S3 ya lo hizo Start: devuelve true sin repetir)");
            if (boot.StreakRunner != null)
            {
                boot.StreakRunner.enabled = false;
            }

            if (boot.DailyRunner != null)
            {
                boot.DailyRunner.enabled = false;
            }
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log && condition.StartsWith("AF_STATE {\"event\":\"", StringComparison.Ordinal)
                && (condition.Contains("\"event\":\"streak_") || condition.Contains("\"event\":\"basket_open\"")
                    || condition.Contains("\"event\":\"free_double_harvest\"")))
            {
                _lines.Add(condition);
            }
        }

        private int Count(string evt)
        {
            string head = "AF_STATE {\"event\":\"" + evt + "\",";
            int n = 0;
            foreach (string line in _lines)
            {
                if (line.StartsWith(head, StringComparison.Ordinal))
                {
                    n++;
                }
            }

            return n;
        }

        private string Only(string evt)
        {
            string head = "AF_STATE {\"event\":\"" + evt + "\",";
            string? found = null;
            foreach (string line in _lines)
            {
                if (line.StartsWith(head, StringComparison.Ordinal))
                {
                    Assert.That(found, Is.Null, "más de una línea " + evt);
                    found = line;
                }
            }

            Assert.That(found, Is.Not.Null, "ninguna línea " + evt);
            return found!;
        }

        private static Profile ProfileAt(int maxLevel)
        {
            Profile profile = Wallet.NewProfile(new ManualClock(Utc(Monday), Bogota));
            profile.Progress.MaxLevel = maxLevel;
            profile.Progress.NextLevel = maxLevel + 1;
            return profile;
        }

        private static DateTime Utc(DateTime bogota)
        {
            return DateTime.SpecifyKind(bogota - Bogota, DateTimeKind.Utc);
        }
    }
}
