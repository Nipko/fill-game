#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AF.RemoteConfig.Firebase;
using NUnit.Framework;

namespace AF.RemoteConfig.Tests
{
    /// <summary>
    /// F3-6: el proveedor de Firebase Remote Config sin el SDK (editor): la caché en disco que lee <c>ActivateCached</c> en el arranque,
    /// los defaults que se le pasan a Firebase y que el editor sigue con el proveedor falso (08 §2.1, principio 6).
    /// </summary>
    public sealed class FirebaseRemoteConfigBackendTests
    {
        private string _dir = "";

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "af-rc-cache-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        [Test]
        public void TheEditorUsesTheFakeProvider()
        {
            Assert.That(RemoteConfigBackends.CreateDefault(RemoteConfigDefaults.Compiled), Is.InstanceOf<FakeRemoteConfigBackend>());
        }

        [Test]
        public void TheCache_RoundTrips_AndFeedsActivateCached()
        {
            string path = Path.Combine(_dir, RemoteConfigBackends.CacheFileName);
            var at = new DateTime(2026, 10, 21, 18, 30, 0, DateTimeKind.Utc);
            var values = new Dictionary<string, string>(StringComparer.Ordinal) { { "eco_reward_normal", "25" }, { "rc_text", "a\"b\\c" } };
            Assert.That(RcCacheFile.TryWrite(path, values, at), Is.True);
            Assert.That(File.Exists(path + ".tmp"), Is.False, "sin temporal suelto");
            Assert.That(File.ReadAllText(path), Does.StartWith("{\"format\":\"af-rc-cache/1\",\"fetched_at\":\"2026-10-21T18:30:00Z\",\"values\":{\"eco_reward_normal\":\"25\""));

            var backend = new FirebaseRemoteConfigBackend(RemoteConfigDefaults.Compiled, path);
            Assert.That(backend.Cached, Is.Not.Null);
            Assert.That(backend.Cached!["eco_reward_normal"], Is.EqualTo("25"));
            Assert.That(backend.Cached!["rc_text"], Is.EqualTo("a\"b\\c"));
            Assert.That(backend.LastFetchUtc, Is.EqualTo(at));
            Assert.That(backend.LastFetchUtc!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));

            var service = new RemoteConfigService(null, backend);
            Assert.That(service.ActivateCached(), Is.True);
            Assert.That(service.Get<long>("eco_reward_normal"), Is.EqualTo(25));
            Assert.That(service.Source, Is.EqualTo(RcSource.Cached));
        }

        [Test]
        public void ABadOrMissingCache_MeansNoCache()
        {
            string path = Path.Combine(_dir, "cache.json");
            Assert.That(new FirebaseRemoteConfigBackend(RemoteConfigDefaults.Compiled, path).Cached, Is.Null, "sin archivo");
            File.WriteAllText(path, "{\"format\":\"otro/1\",\"values\":{}}");
            Assert.That(new FirebaseRemoteConfigBackend(RemoteConfigDefaults.Compiled, path).Cached, Is.Null, "otro formato");
            File.WriteAllText(path, "{roto");
            Assert.That(new FirebaseRemoteConfigBackend(RemoteConfigDefaults.Compiled, path).Cached, Is.Null, "JSON roto");
        }

        [Test]
        public void WithoutFirebase_FetchAnswersNull_AndKeepsTheCache()
        {
            string path = Path.Combine(_dir, "cache.json");
            RcCacheFile.TryWrite(path, new Dictionary<string, string> { { "eco_reward_normal", "25" } }, null);
            var backend = new FirebaseRemoteConfigBackend(RemoteConfigDefaults.Compiled, path);
            bool called = false;
            IReadOnlyDictionary<string, string>? got = new Dictionary<string, string>();
            backend.Fetch(v =>
            {
                called = true;
                got = v;
            });
            Assert.That(called, Is.True);
            Assert.That(got, Is.Null, "en el editor no hay Firebase");
            Assert.That(backend.Cached!["eco_reward_normal"], Is.EqualTo("25"));

            var now = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc);
            backend.MarkFetched(now);
            Assert.That(new FirebaseRemoteConfigBackend(RemoteConfigDefaults.Compiled, path).LastFetchUtc, Is.EqualTo(now), "la hora se guarda en la caché");
        }

        [Test]
        public void TheDefaultsForFirebase_AreTheCompiledOnes_AsText()
        {
            Dictionary<string, object> d = FirebaseRemoteConfigBackend.DefaultsFor(RemoteConfigDefaults.Compiled);
            int withValue = 0;
            foreach (RcParam p in RemoteConfigDefaults.Compiled.Params)
            {
                if (p.Default.HasValue)
                {
                    withValue++;
                    Assert.That(d[p.Key], Is.EqualTo(p.Default.Value.ToRemoteString()), p.Key);
                }
                else
                {
                    Assert.That(d.ContainsKey(p.Key), Is.False, p.Key + ": remite a una tabla, sin valor");
                }
            }

            Assert.That(d.Count, Is.EqualTo(withValue));
            Assert.That(withValue, Is.GreaterThan(0));
        }
    }
}
