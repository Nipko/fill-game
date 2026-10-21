#nullable enable
using System;
using System.IO;
using AF.Backend.Json;
using AF.Backend.Unity;
using AF.Platform;
using NUnit.Framework;

namespace AF.Backend.Tests
{
    /// <summary>F3-1/F3-2: URL por entorno, elección del cliente, Date del servidor y reglas del módulo.</summary>
    public sealed class BackendSetupTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void EndpointsAreEmptyInTheRepository()
        {
            Assert.That(BackendEndpoints.Dev, Is.Empty, "sin URL real en el repositorio (F4-2 la pone el dueño)");
            Assert.That(BackendEndpoints.Prod, Is.Empty, "la de prod llega con F5-1");
            Assert.That(BackendEndpoints.IsDevelopment, Is.True, "el editor cuenta como desarrollo");
            Assert.That(BackendEndpoints.Mode, Is.EqualTo(BackendMode.Fake));
        }

        [Test]
        public void ChooseMode()
        {
            Assert.That(BackendEndpoint.Choose("", development: true), Is.EqualTo(BackendMode.Fake));
            Assert.That(BackendEndpoint.Choose(null, development: false), Is.EqualTo(BackendMode.Unavailable), "release sin URL nunca usa el falso");
            Assert.That(BackendEndpoint.Choose("https://abc.execute-api.sa-east-1.amazonaws.com", false), Is.EqualTo(BackendMode.Http));
            Assert.That(BackendEndpoint.Choose("http://abc.example", true), Is.EqualTo(BackendMode.Unavailable));
            Assert.That(BackendEndpoint.Choose("https://abc.example/?x=1", true), Is.EqualTo(BackendMode.Unavailable));
            Assert.That(BackendEndpoint.Choose("https://user@abc.example", true), Is.EqualTo(BackendMode.Unavailable));
            Assert.That(BackendEndpoint.Choose("https://abc.example:8443", true), Is.EqualTo(BackendMode.Unavailable));
            Assert.That(BackendEndpoint.Normalize(" https://abc.example/dev/ "), Is.EqualTo("https://abc.example/dev"));
        }

        [Test]
        public void FactoryFollowsTheMode()
        {
            var clock = new ManualClock(Now);
            Assert.That(BackendFactory.Create(clock, null, null, null), Is.InstanceOf<FakeBackendClient>());
            Assert.That(BackendFactory.Create(BackendMode.Unavailable, "", clock, null, null, null), Is.InstanceOf<UnavailableBackendClient>());
            Assert.That(BackendFactory.Create(BackendMode.Http, "https://abc.example", clock, null, null, null), Is.InstanceOf<UnavailableBackendClient>(),
                "sin proveedores de tokens reales (F3-6) no se llama a la API");
            Assert.That(BackendFactory.Create(BackendMode.Http, "https://abc.example", clock, new ServerDateTracker(clock),
                new Auth.FakeAuthTokenProvider(), new Auth.FakeIntegrityTokenProvider()), Is.InstanceOf<HttpBackendClient>());
        }

        [Test]
        public void HttpDateParsing()
        {
            var expected = new DateTime(1994, 11, 6, 8, 49, 37, DateTimeKind.Utc);
            Assert.That(ServerDateTracker.TryParseHttpDate("Sun, 06 Nov 1994 08:49:37 GMT", out DateTime a), Is.True);
            Assert.That(a, Is.EqualTo(expected));
            Assert.That(a.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(ServerDateTracker.TryParseHttpDate("Sunday, 06-Nov-94 08:49:37 GMT", out DateTime b), Is.True);
            Assert.That(b, Is.EqualTo(expected));
            Assert.That(ServerDateTracker.TryParseHttpDate("Sun Nov  6 08:49:37 1994", out DateTime c), Is.True);
            Assert.That(c, Is.EqualTo(expected));
            Assert.That(ServerDateTracker.TryParseHttpDate("ayer", out _), Is.False);
            Assert.That(ServerDateTracker.TryParseHttpDate(null, out _), Is.False);
        }

        [Test]
        public void TrackerMeasuresTheOffset()
        {
            var clock = new ManualClock(Now);
            var tracker = new ServerDateTracker(clock);
            IServerDate asInterface = tracker;
            Assert.That(asInterface.TryGetServerOffset(out _), Is.False, "sin ninguna respuesta todavía");

            Assert.That(tracker.ObserveDateHeader("Wed, 21 Oct 2026 10:00:00 GMT"), Is.True);
            Assert.That(asInterface.TryGetServerOffset(out TimeSpan offset), Is.True);
            Assert.That(offset, Is.EqualTo(TimeSpan.FromHours(-2)), "el dispositivo va 2 h adelantado");

            Assert.That(tracker.ObserveDateHeader("Sun, 06 Nov 1994 08:49:37 GMT"), Is.False, "una fecha anterior a 2026 no es del servidor");
            Assert.That(tracker.ObserveDateHeader("basura"), Is.False);
            Assert.That(asInterface.TryGetServerOffset(out offset), Is.True);
            Assert.That(offset, Is.EqualTo(TimeSpan.FromHours(-2)), "una cabecera mala no borra la medición");

            tracker.MarkUnreachable();
            Assert.That(asInterface.TryGetServerOffset(out offset), Is.False);
            Assert.That(offset, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void BackendAsmdefHasNoEngineAndOnlyDependsOnPlatform()
        {
            JsonNode asmdef = BackendJson.Parse(File.ReadAllText(Path.Combine(BackendTestFiles.BackendScriptsDir, "AF.Backend.asmdef")));
            Assert.That(asmdef.Get("noEngineReferences")!.BoolValue, Is.True);
            Assert.That(asmdef.Get("autoReferenced")!.BoolValue, Is.False);
            JsonNode refs = asmdef.Get("references")!;
            Assert.That(refs.Items.Count, Is.EqualTo(1));
            Assert.That(refs.Items[0].Text, Is.EqualTo("AF.Platform"));
        }

        [Test]
        public void BackendSourcesNeverLog()
        {
            foreach (string path in Directory.GetFiles(BackendTestFiles.BackendScriptsDir, "*.cs", SearchOption.AllDirectories))
            {
                if (path.Replace('\\', '/').Contains("/Tests/"))
                {
                    continue;
                }

                string text = File.ReadAllText(path);
                Assert.That(text, Does.Not.Contain("Debug.Log"), Path.GetFileName(path) + ": el backend no escribe en los logs (UID y tokens)");
                Assert.That(text, Does.Not.Contain("Console.Write"), Path.GetFileName(path));
                Assert.That(text, Does.Not.Contain("amazonaws"), Path.GetFileName(path) + ": sin URL real en el repositorio");
            }
        }
    }
}
