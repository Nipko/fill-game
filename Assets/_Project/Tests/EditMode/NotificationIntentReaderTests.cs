#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Observación 19 de la jornada 6 (D-062): el tipo del aviso tocado, con un puerto falso que reproduce lo que hace Android y
    /// <c>com.unity.mobile.notifications</c> 2.4.3: los avisos repiten id, en frío el paquete ya no reconstruye el aviso y la
    /// intención base de la tarea conserva el extra del aviso aunque el proceso lo quite (Recientes tras matar el proceso).
    /// </summary>
    public sealed class NotificationIntentReaderTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 29, 14, 30, 0, DateTimeKind.Utc);

        private ManualClock _clock = null!;
        private FakeIntentPort _port = null!;
        private InMemoryIntentMarkStore _marks = null!;

        [SetUp]
        public void SetUp()
        {
            _clock = new ManualClock(T0);
            _port = new FakeIntentPort();
            _marks = new InMemoryIntentMarkStore();
        }

        private NotificationIntentReader NewReader() => new NotificationIntentReader(_port, _clock, _marks);

        [Test]
        public void WithoutAnIntent_Null()
        {
            NotificationIntentReader reader = NewReader();
            Assert.That(reader.Take(), Is.Null);
            Assert.That(reader.LastDiagnostic, Is.Null);
        }

        [Test]
        public void TheSameIdTwice_TwoTaps_BothCount()
        {
            NotificationIntentReader reader = NewReader();
            reader.Remember(1001, "daily_ready", T0.AddMinutes(-1));
            _port.Deliver(1001, "daily_ready"); // primer toque (en caliente, onNewIntent)
            Assert.That(reader.Take(), Is.EqualTo("daily_ready"));

            reader.Remember(1001, "daily_ready", T0.AddMinutes(1)); // la siguiente salida reprograma el 1001
            _clock.Advance(TimeSpan.FromMinutes(2)); // suena
            _port.Deliver(1001, "daily_ready"); // segundo toque, otro aviso con el mismo id
            Assert.That(reader.Take(), Is.EqualTo("daily_ready"), "el lector anterior lo descartaba por repetir id");
        }

        [Test]
        public void TheSameIntent_InTheSameProcess_CountsOnce()
        {
            NotificationIntentReader reader = NewReader();
            reader.Remember(1001, "daily_ready", T0.AddMinutes(-1));
            _port.Deliver(1001, "daily_ready");
            Assert.That(reader.Take(), Is.EqualTo("daily_ready"));
            Assert.That(reader.Take(), Is.Null, "volver desde Recientes con el proceso vivo: el extra ya se quitó");
        }

        [Test]
        public void ColdStart_ThePackageLostTheNotification_TheLogKnowsIt()
        {
            NotificationIntentReader reader = NewReader();
            reader.Remember(1001, "daily_ready", T0.AddMinutes(-3)); // la lista guardada al programar, cargada al arrancar
            _port.Deliver(1001, null); // en frío GetLastNotificationIntent vuelve vacío
            Assert.That(reader.Take(), Is.EqualTo("daily_ready"));
            Assert.That(reader.LastDiagnostic,
                Is.EqualTo("AF_STATE {\"event\":\"push_intent\",\"id\":1001,\"type\":\"daily_ready\",\"via\":\"log\",\"package\":null}"));
            Assert.That(_marks.Mark, Is.EqualTo(NotificationIntentReader.Mark(1001, T0.AddMinutes(-3))));
        }

        [Test]
        public void ANewProcess_WithTheSameExtra_AndTheListRescheduled_DoesNotCount()
        {
            // Frío desde el 1001 (aceptado); sale a segundo plano y el plan nuevo reprograma el 1001 para mañana; Android mata el
            // proceso y, al volver, recrea la actividad con la intención base de la tarea, que aún lleva el extra.
            NotificationIntentReader first = NewReader();
            first.Remember(1001, "daily_ready", T0.AddMinutes(-3));
            _port.Deliver(1001, null);
            Assert.That(first.Take(), Is.EqualTo("daily_ready"));

            _clock.Advance(TimeSpan.FromMinutes(10));
            NotificationIntentReader second = NewReader(); // proceso nuevo: la lista cargada ya es la del plan nuevo
            second.Remember(1001, "daily_ready", T0.AddDays(1));
            _port.Deliver(1001, null); // misma intención base, sin la marca de Recientes
            Assert.That(second.Take(), Is.Null, "el aviso recordado con ese id aún no ha sonado");
            Assert.That(second.LastDiagnostic, Does.Contain("\"rejected\":\"not_fired\""));
        }

        [Test]
        public void AnIntentFromRecents_DoesNotCount()
        {
            NotificationIntentReader reader = NewReader();
            reader.Remember(1001, "daily_ready", T0.AddMinutes(-3));
            _port.Deliver(1001, null, fromHistory: true);
            Assert.That(reader.Take(), Is.Null, "FLAG_ACTIVITY_LAUNCHED_FROM_HISTORY");
            Assert.That(reader.LastDiagnostic, Does.Contain("\"rejected\":\"history\""));
            Assert.That(_marks.Mark, Is.Null);
        }

        [Test]
        public void ThePersistedMark_DoesNotCountTwice_EvenInANewProcess()
        {
            NotificationIntentReader first = NewReader();
            first.Remember(1001, "daily_ready", T0.AddMinutes(-3));
            _port.Deliver(1001, null);
            Assert.That(first.Take(), Is.EqualTo("daily_ready"));

            NotificationIntentReader second = NewReader(); // otro proceso con la misma lista (no se llegó a reprogramar)
            second.Remember(1001, "daily_ready", T0.AddMinutes(-3));
            _port.Deliver(1001, null);
            Assert.That(second.Take(), Is.Null);
            Assert.That(second.LastDiagnostic, Does.Contain("\"rejected\":\"consumed\""));
        }

        [Test]
        public void TheLastScheduleWins()
        {
            NotificationIntentReader reader = NewReader();
            reader.Remember(1001, "comeback_d7", T0.AddMinutes(-5));
            reader.Remember(1001, "daily_ready", T0.AddMinutes(-1)); // otra salida a segundo plano reprogramó el id
            _port.Deliver(1001, null);
            Assert.That(reader.Take(), Is.EqualTo("daily_ready"));
        }

        [Test]
        public void WithoutTheLog_ThePackage_AndOtherwiseUnknown()
        {
            NotificationIntentReader reader = NewReader();
            _port.Deliver(1002, "logbook_pending"); // lectura tardía: solo el paquete lo sabe
            Assert.That(reader.Take(), Is.EqualTo("logbook_pending"));
            Assert.That(reader.LastDiagnostic, Does.Contain("\"via\":\"package\""));

            _port.Deliver(1003, null);
            Assert.That(reader.Take(), Is.EqualTo(NotificationIntentReader.UnknownType), "push_open sale igual, con type unknown");
            Assert.That(reader.LastDiagnostic, Does.Contain("\"type\":\"unknown\",\"via\":\"none\""));
        }

        /// <summary>La intención de la actividad: un aviso tocado la deja con su id hasta que se lee en el proceso.</summary>
        private sealed class FakeIntentPort : INotificationIntentPort
        {
            private bool _has;
            private int _id;
            private string? _package;
            private bool _history;

            public void Deliver(int id, string? packageType, bool fromHistory = false)
            {
                _has = true;
                _id = id;
                _package = packageType;
                _history = fromHistory;
            }

            public bool TryTakeOpened(out int id, out string? packageType, out bool fromHistory)
            {
                id = _id;
                packageType = _package;
                fromHistory = _history;
                if (!_has)
                {
                    return false;
                }

                _has = false;
                return true;
            }
        }
    }
}
