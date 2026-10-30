#nullable enable
using AF.Platform;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Observación 19 de la jornada 6 (D-062): el tipo del aviso tocado, con un puerto falso que reproduce lo que hace Android y
    /// <c>com.unity.mobile.notifications</c> 2.4.3: la misma intención sigue en la actividad hasta que llega otra, los avisos repiten
    /// id y en frío el paquete ya no reconstruye el aviso.
    /// </summary>
    public sealed class NotificationIntentReaderTests
    {
        [Test]
        public void WithoutAnIntent_Null()
        {
            var port = new FakeIntentPort();
            var reader = new NotificationIntentReader(port);
            Assert.That(reader.Take(), Is.Null);
            Assert.That(reader.LastDiagnostic, Is.Null);
        }

        [Test]
        public void TheSameIdTwice_TwoTaps_BothCount()
        {
            var port = new FakeIntentPort();
            var reader = new NotificationIntentReader(port);
            reader.Remember(1001, "daily_ready");

            port.Deliver(1001, "daily_ready"); // primer toque (en caliente, onNewIntent)
            Assert.That(reader.Take(), Is.EqualTo("daily_ready"));
            port.Deliver(1001, "daily_ready"); // segundo toque, otro aviso con el mismo id
            Assert.That(reader.Take(), Is.EqualTo("daily_ready"), "el lector anterior lo descartaba por repetir id");
        }

        [Test]
        public void TheSameIntent_ReadTwice_CountsOnce()
        {
            var port = new FakeIntentPort();
            var reader = new NotificationIntentReader(port);
            reader.Remember(1001, "daily_ready");
            port.Deliver(1001, "daily_ready");
            Assert.That(reader.Take(), Is.EqualTo("daily_ready"));
            Assert.That(reader.Take(), Is.Null, "volver desde Recientes o rehacer la UI: la misma intención no cuenta otra vez");

            var rebuilt = new NotificationIntentReader(port); // la reconstrucción por idioma crea otro programador
            rebuilt.Remember(1001, "daily_ready");
            Assert.That(rebuilt.Take(), Is.Null, "la marca de leída es de la intención, no del lector");
        }

        [Test]
        public void ColdStart_ThePackageLostTheNotification_TheLogKnowsIt()
        {
            var port = new FakeIntentPort();
            var reader = new NotificationIntentReader(port);
            reader.Remember(1001, "daily_ready"); // la lista guardada al programar, cargada al arrancar
            port.Deliver(1001, null); // en frío GetLastNotificationIntent vuelve vacío
            Assert.That(reader.Take(), Is.EqualTo("daily_ready"));
            Assert.That(reader.LastDiagnostic,
                Is.EqualTo("AF_STATE {\"event\":\"push_intent\",\"id\":1001,\"type\":\"daily_ready\",\"via\":\"log\",\"package\":null}"));
        }

        [Test]
        public void TheLastScheduleWins()
        {
            var port = new FakeIntentPort();
            var reader = new NotificationIntentReader(port);
            reader.Remember(1001, "comeback_d7");
            reader.Remember(1001, "daily_ready"); // otra salida a segundo plano reprogramó el id
            port.Deliver(1001, null);
            Assert.That(reader.Take(), Is.EqualTo("daily_ready"));
        }

        [Test]
        public void WithoutTheLog_ThePackage_AndOtherwiseUnknown()
        {
            var port = new FakeIntentPort();
            var reader = new NotificationIntentReader(port);
            port.Deliver(1002, "logbook_pending"); // lectura tardía: solo el paquete lo sabe
            Assert.That(reader.Take(), Is.EqualTo("logbook_pending"));
            Assert.That(reader.LastDiagnostic, Does.Contain("\"via\":\"package\""));

            port.Deliver(1003, null);
            Assert.That(reader.Take(), Is.EqualTo(NotificationIntentReader.UnknownType), "push_open sale igual, con type unknown");
            Assert.That(reader.LastDiagnostic, Does.Contain("\"type\":\"unknown\",\"via\":\"none\""));
        }

        /// <summary>La intención de la actividad: un aviso tocado la deja con su id hasta que se lee (se quita el extra).</summary>
        private sealed class FakeIntentPort : INotificationIntentPort
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
    }
}
