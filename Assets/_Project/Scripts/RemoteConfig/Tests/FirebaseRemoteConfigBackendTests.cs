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

        [TestCase("rc_schema", true)]
        [TestCase("mech_enabled_hoja", true)]
        [TestCase("_x1", true)]
        [TestCase("A9_b", true)]
        [TestCase("mech_enabled.hoja", false)]
        [TestCase("1abc", false)]
        [TestCase("eco-price", false)]
        [TestCase("niño", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsValidKey_FollowsTheFirebaseRule(string? key, bool valid)
        {
            Assert.That(FirebaseRemoteConfigBackend.IsValidKey(key), Is.EqualTo(valid));
        }

        [Test]
        public void IsValidKey_AcceptsUpTo256Characters()
        {
            Assert.That(FirebaseRemoteConfigBackend.IsValidKey(new string('a', 256)), Is.True);
            Assert.That(FirebaseRemoteConfigBackend.IsValidKey(new string('a', 257)), Is.False);
        }

        [Test]
        public void TheDefaultsForFirebase_LeaveOutTheKeysFirebaseRejects()
        {
            RemoteConfigDefaults defaults = RemoteConfigDefaults.Parse(
                "{\"format\":\"af-rc-defaults/1\",\"params\":{"
                + "\"a_ok\":{\"type\":\"int\",\"default\":1},"
                + "\"b.dot\":{\"type\":\"bool\",\"default\":true},"
                + "\"c_table\":{\"type\":\"json\",\"default\":null,\"pending\":\"tabla\"}}}");
            Dictionary<string, object> d = FirebaseRemoteConfigBackend.DefaultsFor(defaults);
            Assert.That(d.Count, Is.EqualTo(1));
            Assert.That(d["a_ok"], Is.EqualTo("1"));
        }

        [Test]
        public void D043_EveryCompiledKeyIsValidForFirebase()
        {
            foreach (RcParam p in RemoteConfigDefaults.Compiled.Params)
            {
                Assert.That(FirebaseRemoteConfigBackend.IsValidKey(p.Key), Is.True, p.Key);
            }
        }

        [Test]
        public void ThePublishedTemplate_IsWhatTheClientGivesFirebase_ValueByValue()
        {
            // tools/out/remote_config_template_dev.json (tools/remote-config/rc_template.py): lo que se publica en la consola tiene que
            // ser, clave a clave, lo que el cliente le da a Firebase como default; si no, publicar cambiaría el comportamiento.
            string path = Path.Combine(RemoteConfigDefaultsTests.RepoRoot, "tools", "out", "remote_config_template_dev.json");
            Assert.That(File.Exists(path), Is.True, path);
            RcJson root = RcJson.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
            RcJson? parameters = root["parameters"];
            Assert.That(parameters, Is.Not.Null);
            Assert.That(parameters!.Kind, Is.EqualTo(RcJsonKind.Object));

            Dictionary<string, object> d = FirebaseRemoteConfigBackend.DefaultsFor(RemoteConfigDefaults.Compiled);
            Assert.That(parameters.Members.Count, Is.EqualTo(d.Count), "las mismas claves");
            for (int i = 0; i < parameters.Members.Count; i++)
            {
                string key = parameters.Members[i].Key;
                RcJson p = parameters.Members[i].Value;
                Assert.That(d.ContainsKey(key), Is.True, key + ": en la plantilla y no en DefaultsFor");
                RcParam param = RemoteConfigDefaults.Compiled.Find(key)!;
                string published = p["defaultValue"]!["value"]!.Text!;
                Assert.That(p["valueType"]!.Text, Is.EqualTo(ValueTypeOf(param.Type)), key);
                Assert.That(RcValue.TryFromRemote(param.Type, published, out RcValue back), Is.True, key + ": el cliente la entiende");
                Assert.That(back, Is.EqualTo(param.Default!.Value), key + ": el mismo valor que el compilado");
                if (param.Type != RcType.Float)
                {
                    Assert.That(published, Is.EqualTo((string)d[key]), key + ": el mismo texto");
                }
            }
        }

        private static string ValueTypeOf(RcType type)
        {
            switch (type)
            {
                case RcType.Int:
                case RcType.Float:
                    return "NUMBER";
                case RcType.Bool:
                    return "BOOLEAN";
                case RcType.Json:
                    return "JSON";
                default:
                    return "STRING";
            }
        }
    }
}
