#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AF.Economy;
using AF.GameFlow;
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
    /// Observación 19 de la jornada 6 (D-062): el aviso del Reto abre el Reto también en caliente desde el inicio y en frío. Un
    /// programador de prueba lee el aviso tocado con <see cref="NotificationIntentReader"/> sobre un puerto falso que reproduce lo
    /// que pasa en el teléfono: los avisos repiten id (1001), la intención sigue en la actividad hasta que llega otra y en frío el
    /// paquete ya no reconstruye el aviso (solo la lista de lo programado sabe su tipo).
    /// </summary>
    public sealed class PushIntentTests
    {
        private const int MaxLevel = 25;
        private const int Frame = PlayModeHelpers.FrameMs;
        private static readonly TimeSpan Bogota = TimeSpan.FromHours(-5);
        private static readonly DateTime Tuesday = new DateTime(2026, 10, 20, 10, 0, 0); // hora local de Bogotá

        private ManualClock _clock = null!;
        private IntentPort _port = null!;
        private PortScheduler _scheduler = null!;
        private readonly List<string> _lines = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(Utc(Tuesday), Bogota);
            LaunchOptions.ClockOverride = _clock;
            _port = new IntentPort();
            _scheduler = new PortScheduler(new FakeNotificationScheduler(_clock), _port);
            GameBootstrap.NotificationSchedulerOverride = _scheduler;
            GameBootstrap.LogbookOverride = true;
            GameBootstrap.LogbookStoreOverride = new InMemoryLoginCalendarStore(new LoginCalendarRecord()); // sin casilla por sellar
            GameBootstrap.DailyPoolOverride = DailyPlayTestPool.Full;
            GameBootstrap.DailyStoreOverride = new InMemoryDailyChallengeStore();
            _lines.Clear();
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            GameBootstrap.ResetSessionOverrides();
            GameBootstrap.ResetLogbookOverrides();
            GameBootstrap.LogbookOverride = null;
            GameBootstrap.ResetDailyOverrides();
            LaunchOptions.Reset();
            LocSetUpFixture.ConfigureEs419();
        }

        [UnityTest]
        public IEnumerator Warm_FromTheLobby_TheSameIdTwice_RoutesBothTimes_AndTheSameIntentOnce()
        {
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);
            _scheduler.Reader.Remember(1001, "daily_ready"); // el primero del plan (sin permiso en el test no se programa de verdad)

            Background(boot);
            _port.Deliver(1001, "daily_ready");
            Foreground(boot);
            Step(boot, Frame);
            Assert.That(boot.DailyScreen!.IsOpen, Is.True, "primer toque: el Reto");
            Assert.That(Count("\"event\":\"push_open\""), Is.EqualTo(1));
            Assert.That(boot.DailyScreen.Exit(), Is.True);
            Step(boot, Frame);

            Background(boot);
            _port.Deliver(1001, "daily_ready"); // otro aviso con el mismo id: antes se descartaba
            Foreground(boot);
            Step(boot, Frame);
            Assert.That(boot.DailyScreen.IsOpen, Is.True, "segundo toque con el mismo id: también el Reto");
            Assert.That(Count("\"event\":\"push_open\""), Is.EqualTo(2));
            Assert.That(boot.DailyScreen.Exit(), Is.True);
            Step(boot, Frame);

            Background(boot);
            Foreground(boot); // volver desde Recientes: la intención de la actividad es la misma, ya leída
            Step(boot, Frame);
            Assert.That(boot.DailyScreen.IsOpen, Is.False);
            Assert.That(Count("\"event\":\"push_open\""), Is.EqualTo(2), "la misma intención no cuenta dos veces");
        }

        [UnityTest]
        public IEnumerator Cold_ThePackageLostTheNotification_StillOpensTheReto()
        {
            _scheduler.Reader.Remember(1001, "daily_ready"); // la lista de lo programado que dejó el proceso anterior
            _port.Deliver(1001, null); // en frío, GetLastNotificationIntent vuelve vacío
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);

            Assert.That(boot.DailyScreen!.IsOpen, Is.True, "en frío: el Reto");
            Assert.That(boot.LastPushDestination, Is.EqualTo("daily"));
            Assert.That(Find("\"event\":\"push_intent\""), Does.Contain("\"id\":1001,\"type\":\"daily_ready\",\"via\":\"log\",\"package\":null"));
            Assert.That(Count("\"event\":\"push_open\""), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AnIntentWithoutAKnownType_StillWritesPushOpen()
        {
            _port.Deliver(4242, null);
            yield return Boot();
            GameBootstrap boot = GameBootstrap.Current!;
            Step(boot, Frame);

            Assert.That(Find("\"event\":\"push_open\""), Does.Contain("\"type\":\"unknown\",\"dest\":\"home\""));
            Assert.That(boot.DailyScreen!.IsOpen, Is.False);
        }

        private IEnumerator Boot()
        {
            Profile profile = Wallet.NewProfile(new ManualClock(Utc(Tuesday), Bogota));
            profile.Progress.MaxLevel = MaxLevel;
            profile.Progress.NextLevel = MaxLevel + 1;
            profile.Progress.MapSeenLevel = MaxLevel;
            yield return PlayModeHelpers.LoadApp(new MemoryProfileStore(profile));
            GameBootstrap boot = GameBootstrap.Current!;
            PlayModeHelpers.TakeClock(boot);
            boot.DailyRunner!.enabled = false;
            boot.LogbookRunner!.enabled = false;
        }

        private void Background(GameBootstrap boot)
        {
            boot.SessionsPaused(true);
            boot.LogbookPause(true);
            _clock.Advance(TimeSpan.FromMinutes(5));
        }

        private static void Foreground(GameBootstrap boot)
        {
            boot.LogbookPause(false);
            boot.SessionsPaused(false);
        }

        private static void Step(GameBootstrap boot, int ms)
        {
            for (int t = 0; t < ms; t += Frame)
            {
                boot.TickSessions();
                boot.TickDaily(Frame);
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

        private int Count(string fragment)
        {
            int n = 0;
            foreach (string line in _lines)
            {
                if (line.Contains(fragment))
                {
                    n++;
                }
            }

            return n;
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

        private static DateTime Utc(DateTime bogota)
        {
            return DateTime.SpecifyKind(bogota - Bogota, DateTimeKind.Utc);
        }

        /// <summary>La intención de la actividad: un aviso tocado la deja con su id hasta que se lee.</summary>
        private sealed class IntentPort : INotificationIntentPort
        {
            private bool _has;
            private int _id;
            private string? _package;

            public void Deliver(int id, string? packageType)
            {
                _has = true;
                _id = id;
                _package = packageType;
            }

            public bool TryTakeOpened(out int id, out string? packageType)
            {
                id = _id;
                packageType = _package;
                if (!_has)
                {
                    return false;
                }

                _has = false;
                return true;
            }
        }

        /// <summary>
        /// El programador falso con la lectura del aviso tocado del de Android: apunta el tipo de cada aviso programado y lo lee con
        /// <see cref="NotificationIntentReader"/>; la línea de diagnóstico va al log, como en el teléfono.
        /// </summary>
        private sealed class PortScheduler : INotificationScheduler
        {
            private readonly FakeNotificationScheduler _inner;

            public PortScheduler(FakeNotificationScheduler inner, INotificationIntentPort port)
            {
                _inner = inner;
                Reader = new NotificationIntentReader(port);
            }

            public NotificationIntentReader Reader { get; }

            public PushPermission Permission => _inner.Permission;

            public bool PermissionRequired => _inner.PermissionRequired;

            public void RequestPermission(Action<PushPermission> done) => _inner.RequestPermission(done);

            public void Schedule(LocalNotification notification)
            {
                _inner.Schedule(notification);
                Reader.Remember(notification.Id, notification.Type);
            }

            public void CancelAll() => _inner.CancelAll();

            public IReadOnlyList<DeliveredNotification> TakeDelivered() => _inner.TakeDelivered();

            public string? TakeOpenedType()
            {
                string? type = Reader.Take();
                if (type != null && Reader.LastDiagnostic != null)
                {
                    Debug.Log(Reader.LastDiagnostic);
                }

                return type;
            }
        }
    }
}
