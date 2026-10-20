#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using AF.Platform;
using NUnit.Framework;

namespace AF.LiveOps.Tests
{
    /// <summary>
    /// B6: cifras de push y del Regalo de regreso contra la tabla única de 08 §9 (patrón de <c>DailyChallengeConfigTests</c>), horas
    /// silenciosas, reglas del pre-prompt (06 §8.1) y <c>settings.push</c> (08 §4.3) con el diálogo nativo simulado.
    /// </summary>
    public sealed class PushPermissionTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc);

        private static readonly string[] ExpectedKeys =
        {
            "live_comeback_gift", "push_daily_cap_d0_d7", "push_preprompt_min_level", "push_preprompt_session", "push_quiet_hours",
            "push_weekly_cap_lapsed",
        };

        [Test]
        public void EveryDefault_IsTheOneOfThe08_9Table()
        {
            Dictionary<string, RcRow> table = LiveOpsTestFiles.RemoteConfigTable();
            var seen = new List<string>();
            foreach (object instance in new object[] { PushConfig.Defaults, ComebackConfig.Defaults })
            {
                foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var attribute = property.GetCustomAttribute<RemoteConfigKeyAttribute>();
                    if (attribute == null)
                    {
                        Assert.That(property.CanWrite, Is.False, property.Name + ": toda cifra configurable sale de 08 §9");
                        continue;
                    }

                    string key = attribute.Key;
                    Assert.That(table.ContainsKey(key), Is.True, key + " no está en 08 §9");
                    RcRow row = table[key];
                    Assert.That(row.Type, Is.EqualTo(GddType(property.PropertyType)), key + ": tipo");
                    Assert.That(Format(property.GetValue(instance)), Is.EqualTo(row.Default), key + ": default");
                    seen.Add(key);
                }
            }

            seen.Sort(StringComparer.Ordinal);
            Assert.That(seen, Is.EqualTo(ExpectedKeys));
        }

        [Test]
        public void NoKey_IsARetiredAlias()
        {
            string retired = LiveOpsTestFiles.RetiredAliases();
            StringAssert.Contains("`push_preprompt_level`", retired);
            StringAssert.Contains("`comeback_*`", retired);
            foreach (string key in ExpectedKeys)
            {
                Assert.That(key.StartsWith("comeback_", StringComparison.Ordinal), Is.False, key);
                Assert.That(key, Is.Not.EqualTo("push_preprompt_level"));
            }
        }

        [Test]
        public void PushConfig_ValidatesTheRangesOf08_9()
        {
            PushConfig.Defaults.Validate();
            var bad = new List<Action<PushConfig>>
            {
                c => c.PushPrepromptSession = 1,
                c => c.PushPrepromptSession = 4,
                c => c.PushPrepromptMinLevel = 7,
                c => c.PushPrepromptMinLevel = 13,
                c => c.PushQuietHours = "22:09",
                c => c.PushQuietHours = "25-09",
                c => c.PushDailyCapD0D7 = 0,
                c => c.PushWeeklyCapLapsed = 0,
            };
            for (int i = 0; i < bad.Count; i++)
            {
                PushConfig config = PushConfig.Defaults;
                bad[i](config);
                Assert.Throws<InvalidOperationException>(() => config.Validate(), "caso " + i);
            }
        }

        [Test]
        public void QuietHours_2209_CoverTheNight()
        {
            Assert.That(QuietHours.TryParse("22-09", out QuietHours q), Is.True);
            Assert.That(q, Is.EqualTo(QuietHours.Default));
            Assert.That(q.Format(), Is.EqualTo("22-09"));
            var day = new DateTime(2026, 10, 20);
            Assert.That(q.Contains(day.AddHours(21).AddMinutes(59)), Is.False);
            Assert.That(q.Contains(day.AddHours(22)), Is.True);
            Assert.That(q.Contains(day.AddHours(3)), Is.True);
            Assert.That(q.Contains(day.AddHours(8).AddMinutes(59)), Is.True);
            Assert.That(q.Contains(day.AddHours(9)), Is.False);

            Assert.That(q.NextAllowed(day.AddHours(23)), Is.EqualTo(day.AddDays(1).AddHours(9)), "de noche, a las 09:00 del día siguiente");
            Assert.That(q.NextAllowed(day.AddHours(3)), Is.EqualTo(day.AddHours(9)), "de madrugada, a las 09:00 de hoy");
            Assert.That(q.NextAllowed(day.AddHours(12)), Is.EqualTo(day.AddHours(12)));

            Assert.That(QuietHours.TryParse("01-05", out QuietHours inDay), Is.True);
            Assert.That(inDay.Contains(day.AddHours(2)), Is.True);
            Assert.That(inDay.Contains(day.AddHours(5)), Is.False);
            Assert.That(new QuietHours(9, 9).Contains(day.AddHours(9)), Is.False, "inicio = fin: sin silencio");
            foreach (string badText in new[] { null!, "", "2209", "22-9", "aa-09", "22-24", "-1-09" })
            {
                Assert.That(QuietHours.TryParse(badText, out QuietHours d), Is.False, badText ?? "null");
                Assert.That(d, Is.EqualTo(QuietHours.Default));
            }

            Assert.That(new PushConfig { PushQuietHours = "nada" }.QuietHours, Is.EqualTo(QuietHours.Default));
        }

        [TestCase(1, 30, PrePromptKind.None, TestName = "PrePrompt_NeverInTheFirstSession")]
        [TestCase(2, 9, PrePromptKind.None, TestName = "PrePrompt_NeverOnTheVictoryOfLevel10")]
        [TestCase(2, 10, PrePromptKind.First, TestName = "PrePrompt_FirstVictoryOfSession2_WithLevel10Passed")]
        [TestCase(3, 11, PrePromptKind.First, TestName = "PrePrompt_LaterSessionsToo_IfItNeverShowed")]
        [TestCase(2, 4, PrePromptKind.None, TestName = "PrePrompt_WaitsForLevel10_IfSession2StartsBefore")]
        public void FirstPrePrompt(int sessions, int maxBefore, PrePromptKind expected)
        {
            var record = new PushSettingsRecord();
            var moment = new PrePromptMoment(sessions, maxBefore, 0, T0);
            Assert.That(PushPrePromptRules.Due(record, PushConfig.Defaults, PushPermission.NotRequested, in moment), Is.EqualTo(expected));
        }

        [Test]
        public void SecondPrePrompt_AtStreak3_OrAfterSevenDays_AndNeverAThird()
        {
            var record = new PushSettingsRecord { PrePromptShown = true, PrePromptAt = T0 };
            PushConfig config = PushConfig.Defaults;
            Assert.That(Due(record, config, new PrePromptMoment(3, 20, 2, T0.AddDays(6))), Is.EqualTo(PrePromptKind.None));
            Assert.That(Due(record, config, new PrePromptMoment(3, 20, 3, T0.AddDays(1))), Is.EqualTo(PrePromptKind.Second), "hito 3");
            Assert.That(Due(record, config, new PrePromptMoment(3, 20, 4, T0.AddDays(1))), Is.EqualTo(PrePromptKind.None),
                "una racha ya pasada de 3 no es «alcanzar el hito 3»");
            Assert.That(Due(record, config, new PrePromptMoment(3, 20, 3, T0)), Is.EqualTo(PrePromptKind.None), "no en el mismo instante del primero");
            Assert.That(Due(record, config, new PrePromptMoment(3, 20, 0, T0.AddDays(7))), Is.EqualTo(PrePromptKind.Second), "7 días");
            Assert.That(Due(record, config, new PrePromptMoment(3, 20, 0, T0.AddDays(-30))), Is.EqualTo(PrePromptKind.None), "reloj atrasado");

            record.SecondPromptShown = true;
            Assert.That(Due(record, config, new PrePromptMoment(9, 90, 9, T0.AddDays(90))), Is.EqualTo(PrePromptKind.None), "después, solo Ajustes");

            var granted = new PushSettingsRecord { Granted = true };
            Assert.That(Due(granted, config, new PrePromptMoment(2, 20, 0, T0)), Is.EqualTo(PrePromptKind.None));
            var fresh = new PushSettingsRecord();
            Assert.That(
                PushPrePromptRules.Due(fresh, config, PushPermission.Blocked, new PrePromptMoment(2, 20, 0, T0)), Is.EqualTo(PrePromptKind.None),
                "bloqueado en el teléfono: el diálogo nativo ya no saldría");
        }

        [Test]
        public void Accepting_AsksTheNativeDialog_AndGrants()
        {
            var f = new Fixture();
            f.Presenter.Mode = SimulatedPrePromptAnswer.Accept;
            bool? done = null;
            PrePromptKind shown = f.Service.OnVictory(new PrePromptMoment(2, 10, 0, T0), f.Presenter, g => done = g);

            Assert.That(shown, Is.EqualTo(PrePromptKind.First));
            Assert.That(f.Presenter.Shown.Count, Is.EqualTo(1));
            Assert.That(f.Scheduler.PermissionRequests, Is.EqualTo(1), "el diálogo nativo, solo tras «Avísame»");
            Assert.That(done, Is.True);
            PushSettingsRecord r = f.Store.Load();
            Assert.That(r.PrePromptShown, Is.True);
            Assert.That(r.PrePromptAt, Is.EqualTo(T0));
            Assert.That(r.Granted, Is.True);
            Assert.That(f.Service.CanNotify, Is.True);
            Assert.That(f.Events, Is.EqualTo(new[] { "pre/accepted/first", "native/granted/first" }));
            Assert.That(f.Service.OnVictory(new PrePromptMoment(2, 11, 0, T0), f.Presenter), Is.EqualTo(PrePromptKind.None));
        }

        [Test]
        public void NotNow_RecordsTheFirst_AndTheSecondComesWithTheStreak()
        {
            var f = new Fixture();
            Assert.That(f.Presenter.Mode, Is.EqualTo(SimulatedPrePromptAnswer.NotNow), "por defecto el simulado nunca pide permiso");
            Assert.That(f.Service.OnVictory(new PrePromptMoment(2, 10, 0, T0), f.Presenter), Is.EqualTo(PrePromptKind.First));
            Assert.That(f.Scheduler.PermissionRequests, Is.EqualTo(0));
            Assert.That(f.Store.Load().Granted, Is.False);
            Assert.That(f.Service.CanNotify, Is.False);

            f.Presenter.Mode = SimulatedPrePromptAnswer.Accept;
            f.Scheduler.NativeAnswer = PushPermission.Denied;
            Assert.That(f.Service.OnVictory(new PrePromptMoment(2, 11, 3, T0.AddHours(1)), f.Presenter), Is.EqualTo(PrePromptKind.Second));
            Assert.That(f.Store.Load().SecondPromptShown, Is.True);
            Assert.That(f.Store.Load().Granted, Is.False, "rechazó el diálogo nativo");
            Assert.That(f.Events, Is.EqualTo(new[] { "pre/declined/first", "pre/accepted/second", "native/denied/second" }));
            Assert.That(f.Service.OnVictory(new PrePromptMoment(5, 50, 3, T0.AddDays(20)), f.Presenter), Is.EqualTo(PrePromptKind.None));
        }

        [Test]
        public void AStreakAlreadyAbove3AtTheFirst_DoesNotBringTheSecondAtTheNextWin()
        {
            var f = new Fixture();
            Assert.That(f.Service.OnVictory(new PrePromptMoment(2, 30, 5, T0), f.Presenter), Is.EqualTo(PrePromptKind.First));
            Assert.That(f.Service.OnVictory(new PrePromptMoment(2, 31, 6, T0.AddMinutes(3)), f.Presenter), Is.EqualTo(PrePromptKind.None),
                "minutos después, con la racha en 6: no (revisión de B6)");
            Assert.That(f.Service.OnVictory(new PrePromptMoment(3, 40, 3, T0.AddDays(2)), f.Presenter), Is.EqualTo(PrePromptKind.Second),
                "tras romperse, al volver a llegar a 3");
        }

        [Test]
        public void AnOpenPrePrompt_IsNotShownTwice()
        {
            var f = new Fixture();
            f.Presenter.Mode = SimulatedPrePromptAnswer.Wait;
            Assert.That(f.Service.OnVictory(new PrePromptMoment(2, 10, 0, T0), f.Presenter), Is.EqualTo(PrePromptKind.First));
            Assert.That(f.Service.Asking, Is.True);
            Assert.That(f.Service.OnVictory(new PrePromptMoment(2, 11, 0, T0), f.Presenter), Is.EqualTo(PrePromptKind.None));
            f.Presenter.Answer(false);
            Assert.That(f.Service.Asking, Is.False);
            Assert.That(f.Presenter.Shown.Count, Is.EqualTo(1));
        }

        [Test]
        public void Android12_HasNoNativeDialog_ButStillNeedsTheYes()
        {
            var f = new Fixture(permissionRequired: false);
            Assert.That(f.Scheduler.Permission, Is.EqualTo(PushPermission.Allowed));
            Assert.That(f.Service.CanNotify, Is.False, "sin «Avísame» no se avisa (B6-Q3)");
            f.Presenter.Mode = SimulatedPrePromptAnswer.Accept;
            f.Service.OnVictory(new PrePromptMoment(2, 10, 0, T0), f.Presenter);
            Assert.That(f.Service.CanNotify, Is.True);
            Assert.That(f.Events, Is.EqualTo(new[] { "pre/accepted/first" }), "sin paso native");
        }

        [Test]
        public void Settings_TurnsOnAndOff()
        {
            var f = new Fixture();
            bool? result = null;
            f.Service.SetFromSettings(true, g => result = g);
            Assert.That(result, Is.True);
            Assert.That(f.Store.Load().Granted, Is.True);
            Assert.That(f.Events, Is.EqualTo(new[] { "native/granted/settings" }));

            f.Scheduler.Schedule(new LocalNotification(1, "daily_ready", "t", "b", T0.AddDays(1)));
            f.Service.SetFromSettings(false, g => result = g);
            Assert.That(result, Is.False);
            Assert.That(f.Store.Load().Granted, Is.False);
            Assert.That(f.Scheduler.Scheduled.Count, Is.EqualTo(0), "apagar cancela los programados");

            f.Service.SetFromSettings(true, g => result = g);
            Assert.That(result, Is.True, "con el permiso ya concedido no hay diálogo");
            Assert.That(f.Scheduler.PermissionRequests, Is.EqualTo(1));

            f.Scheduler.Permission = PushPermission.Blocked;
            f.Store.Load().Granted = false;
            f.Service.SetFromSettings(true, g => result = g);
            Assert.That(result, Is.False, "bloqueado en el teléfono: la UI explica cómo activarlo allí");
            Assert.That(f.Store.Load().Granted, Is.False);
        }

        [Test]
        public void SyncWithSystem_OnlyLowersGranted()
        {
            var f = new Fixture();
            f.Store.Load().Granted = true;
            f.Scheduler.Permission = PushPermission.Allowed;
            Assert.That(f.Service.SyncWithSystem(), Is.False);
            f.Scheduler.Permission = PushPermission.Blocked;
            Assert.That(f.Service.SyncWithSystem(), Is.True);
            Assert.That(f.Store.Load().Granted, Is.False);
            f.Scheduler.Permission = PushPermission.Allowed;
            Assert.That(f.Service.SyncWithSystem(), Is.False, "encenderlos en el teléfono no salta el pre-prompt");
            Assert.That(f.Store.Load().Granted, Is.False);
        }

        private static PrePromptKind Due(PushSettingsRecord record, PushConfig config, PrePromptMoment moment)
        {
            return PushPrePromptRules.Due(record, config, PushPermission.NotRequested, in moment);
        }

        private static string GddType(Type type)
        {
            if (type == typeof(int))
            {
                return "int";
            }

            if (type == typeof(string))
            {
                return "string";
            }

            if (type == typeof(ComebackGift))
            {
                return "json";
            }

            throw new NotSupportedException(type.Name);
        }

        private static string Format(object? value)
        {
            switch (value)
            {
                case int i: return i.ToString(CultureInfo.InvariantCulture);
                case string s: return s;
                case ComebackGift g: return g.Format();
                default: throw new NotSupportedException(value?.GetType().Name ?? "null");
            }
        }

        private sealed class Fixture
        {
            public Fixture(bool permissionRequired = true)
            {
                Clock = new ManualClock(T0);
                Scheduler = new FakeNotificationScheduler(Clock, PushPermission.NotRequested, permissionRequired);
                Store = new InMemoryPushSettingsStore();
                Service = new PushPermissionService(Store, Scheduler);
                Presenter = new SimulatedPushPrePrompt();
                Service.Reported += e => Events.Add(e.Step + "/" + e.Result + "/" + e.Origin);
            }

            public ManualClock Clock { get; }

            public FakeNotificationScheduler Scheduler { get; }

            public InMemoryPushSettingsStore Store { get; }

            public PushPermissionService Service { get; }

            public SimulatedPushPrePrompt Presenter { get; }

            public List<string> Events { get; } = new List<string>();
        }
    }
}
