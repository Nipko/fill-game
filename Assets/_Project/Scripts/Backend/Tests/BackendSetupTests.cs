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

        /// <summary>La URL del API de dev desplegado (D-035, añadido de F3-1 y F3-2; codex-estado.md, «Dev desplegado y comprobado»).</summary>
        private const string ApprovedDevUrl = "https://4tbucfarv9.execute-api.sa-east-1.amazonaws.com";

        [Test]
        public void EndpointsByEnvironment_TheEditorStaysOnTheFake()
        {
            Assert.That(BackendEndpoints.Dev, Is.EqualTo(ApprovedDevUrl), "la URL aprobada (F3-6)");
            Assert.That(BackendEndpoint.Normalize(BackendEndpoints.Dev), Is.EqualTo(ApprovedDevUrl), "https://host, sin barra ni consulta");
            Assert.That(BackendEndpoints.Prod, Is.Empty, "la de prod llega con F5-1");
            Assert.That(BackendEndpoints.IsDevelopment, Is.True, "el editor cuenta como desarrollo");
            Assert.That(BackendEndpoints.IsEditor, Is.True);
            Assert.That(BackendEndpoints.Current, Is.Empty, "el editor nunca llama al API: sin Firebase no hay tokens (08 §2.1, principio 6)");
            Assert.That(BackendEndpoints.Mode, Is.EqualTo(BackendMode.Fake));
        }

        [Test]
        public void NoRealFirebaseInTheEditor()
        {
            Assert.That(Firebase.FirebaseBoot.IsConfigured, Is.False, "el editor (ARM64 o x64) no toca el SDK");
            Assert.That(Firebase.FirebaseTokenProviders.TryGet(out Auth.IAuthTokenProvider? auth, out Auth.IIntegrityTokenProvider? integrity), Is.False);
            Assert.That(auth, Is.Null);
            Assert.That(integrity, Is.Null);
            bool? ready = null;
            Firebase.FirebaseBoot.WhenReady(ok => ready = ok);
            Assert.That(ready, Is.False, "responde en el acto, sin arrancar nada");
            Assert.That(Firebase.FirebaseBoot.State, Is.EqualTo(Firebase.FirebaseBootState.Unavailable));
            Assert.That(Firebase.FirebaseBoot.Failure, Is.EqualTo("editor"));

            TokenResultOf(new Firebase.FirebaseAuthTokenProvider(), out Auth.TokenResult? idToken);
            Assert.That(idToken!.IsOk, Is.False);
            Assert.That(idToken.Failure, Does.StartWith("firebase_unavailable"));
            Auth.TokenResult? appCheck = null;
            new Firebase.AppCheckIntegrityTokenProvider().GetLimitedUseToken(r => appCheck = r);
            Assert.That(appCheck!.IsOk, Is.False);
        }

        private static void TokenResultOf(Auth.IAuthTokenProvider provider, out Auth.TokenResult? result)
        {
            Auth.TokenResult? got = null;
            provider.GetIdToken(false, r => got = r);
            result = got;
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
            Assert.That(BackendFactory.Create(clock, null, null, null), Is.InstanceOf<FakeBackendClient>(), "el editor, siempre con el falso");
            Assert.That(BackendFactory.Create(BackendMode.Unavailable, "", false, clock, null, null, null), Is.InstanceOf<UnavailableBackendClient>());
            Assert.That(BackendFactory.Create(BackendMode.Http, "https://abc.example", false, clock, null, null, null), Is.InstanceOf<UnavailableBackendClient>(),
                "un release sin Firebase (sin tokens) no llama a la API ni concede nada");
            Assert.That(BackendFactory.Create(BackendMode.Http, "https://abc.example", true, clock, null, null, null), Is.InstanceOf<FakeBackendClient>(),
                "un build de desarrollo sin google-services.json sigue con el falso");
            Assert.That(BackendFactory.Create(BackendMode.Http, "https://abc.example", false, clock, null, new Auth.FakeAuthTokenProvider(), null),
                Is.InstanceOf<UnavailableBackendClient>(), "hacen falta los dos proveedores");
            Assert.That(BackendFactory.Create(BackendMode.Http, "https://abc.example", false, clock, new ServerDateTracker(clock),
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
                if (Path.GetFileName(path) == "BackendSetup.cs")
                {
                    // La única URL real es la de dev, en BackendEndpoints.Dev (D-035, añadido de F3-1 y F3-2).
                    Assert.That(text.Replace(ApprovedDevUrl, ""), Does.Not.Contain("amazonaws"), "solo la URL de dev aprobada");
                    continue;
                }

                Assert.That(text, Does.Not.Contain("amazonaws"), Path.GetFileName(path) + ": la URL va solo en BackendEndpoints");
            }
        }
    }
}
