#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// Formato JSON del perfil (GDD 08 §4.3, con el inventario de 04 §10.1): nombres de clave del GDD, orden estable,
    /// ida y vuelta exacta (también de los ticks de las horas), InvariantCulture, salida ASCII con LF, tope de 100 KB,
    /// lectura tolerante a claves que faltan o sobran y estricta con los tipos (un tipo equivocado es corrupción).
    /// </summary>
    public sealed class ProfileJsonTests
    {
        private static readonly string NewProfileJson = string.Join("\n", new[]
        {
            "{",
            "  \"profile_schema\": 1,",
            "  \"created_at\": \"2026-10-05T13:10:00Z\",",
            "  \"economy_ts\": null,",
            "  \"wallet\": {",
            "    \"seeds\": 0,",
            "    \"canoes\": {",
            "      \"count\": 5,",
            "      \"next_regen_at\": null,",
            "      \"infinite_until\": null",
            "    }",
            "  },",
            "  \"stock\": {",
            "    \"undo\": 0,",
            "    \"hint\": 0,",
            "    \"reveal\": 0,",
            "    \"extra_cuia\": 0,",
            "    \"shuffle\": 0",
            "  },",
            "  \"daily\": {",
            "    \"date_local\": null,",
            "    \"rv_undo_refill\": 0,",
            "    \"rv_hint\": 0,",
            "    \"rv_reveal\": 0,",
            "    \"rv_extra_canoe\": 0,",
            "    \"rv_double_harvest\": 0,",
            "    \"rv_total\": 0,",
            "    \"interstitials_today\": 0,",
            "    \"rescues_today\": 0,",
            "    \"af_rescue_shown\": 0,",
            "    \"af_rescue_declined_days\": 0,",
            "    \"af_rescue_hidden_until\": null,",
            "    \"shuffles_today\": 0,",
            "    \"shield_available\": false",
            "  },",
            "  \"progress\": {",
            "    \"max_level\": 0,",
            "    \"next_level\": 1,",
            "    \"since_last_skip\": 0,",
            "    \"skipped_levels\": [],",
            "    \"content_version\": null",
            "  },",
            "  \"level_state\": null,",
            "  \"entitlements\": {",
            "    \"no_ads\": false,",
            "    \"starter_bought\": false,",
            "    \"skins\": []",
            "  },",
            "  \"ads\": {",
            "    \"last_iap\": null,",
            "    \"levels_since_interstitial\": 0",
            "  },",
            "  \"stats\": {",
            "    \"levels_won\": 0,",
            "    \"fails_hard\": 0,",
            "    \"rescues\": 0,",
            "    \"rv_watched\": 0,",
            "    \"sessions\": 0",
            "  }",
            "}",
            "",
        });

        [Test]
        public void NewProfile_HasTheGddKeysInAStableOrder()
        {
            Profile p = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);

            Assert.That(ProfileJson.Serialize(p), Is.EqualTo(NewProfileJson));
            Assert.That(p.ProfileSchema, Is.EqualTo(1));
            Assert.That(p.Wallet.Seeds, Is.EqualTo(0), "el GDD no fija otro saldo inicial");
            Assert.That(p.Progress.NextLevel, Is.EqualTo(1), "08 §2.3: la primera vez se entra al nivel 1");
            Assert.That(p.LevelState, Is.Null);
            Assert.That(p.CreatedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void CreateNew_RejectsNegativeCanoesAndNullClock()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Profile.CreateNew(new ManualClock(SampleProfiles.T0), -1));
            Assert.Throws<ArgumentNullException>(() => Profile.CreateNew(null!, 5));
        }

        [Test]
        public void FullProfile_RoundTripsExactly()
        {
            Profile original = SampleProfiles.Full();
            string json = ProfileJson.Serialize(original);

            Profile copy = ProfileJson.Deserialize(json);

            Assert.That(SampleProfiles.Describe(copy), Is.EqualTo(SampleProfiles.Describe(original)));
            Assert.That(ProfileJson.Serialize(copy), Is.EqualTo(json));
            Assert.That(copy.EconomyTs!.Value.Ticks, Is.EqualTo(original.EconomyTs!.Value.Ticks), "los ticks sueltos sobreviven");
            Assert.That(copy.EconomyTs.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(copy.LevelState!.History, Is.EqualTo(original.LevelState!.History), "comillas, barras, saltos y no ASCII");
            StringAssert.Contains("\"economy_ts\": \"2026-10-05T14:03:11.1234567Z\"", json);
            StringAssert.Contains("\"infinite_until\": \"2026-10-05T15:00:00.5Z\"", json);
            StringAssert.Contains("\"skipped_levels\": [63, 71]", json);
            StringAssert.Contains("\"skins\": [\"af_skin_m02\", \"af_skin_m04\"]", json);
            StringAssert.Contains("\"last_iap\": \"2026-10-05T13:40:00Z\"", json);
        }

        [Test]
        public void Output_IsAsciiWithLfAndNoBom()
        {
            string json = ProfileJson.Serialize(SampleProfiles.Full());

            Assert.That(json.EndsWith("}\n", StringComparison.Ordinal), Is.True);
            Assert.That(json.IndexOf('\r'), Is.EqualTo(-1));
            for (int i = 0; i < json.Length; i++)
            {
                Assert.That(json[i] == '\n' || (json[i] >= ' ' && json[i] <= '~'), Is.True, "carácter no ASCII en la posición " + i);
            }

            StringAssert.Contains("\\u00f1\\u00e1\\u20ac\\ud83c\\udf34", json);
        }

        [Test]
        public void Serialization_IsTheSameInAnyCulture()
        {
            Profile p = SampleProfiles.Full();
            string invariant;
            using (CultureScope.Use(System.Globalization.CultureInfo.InvariantCulture))
            {
                invariant = ProfileJson.Serialize(p);
            }

            using (CultureScope.Use(CultureScope.Hostile()))
            {
                Assert.That(ProfileJson.Serialize(p), Is.EqualTo(invariant));
                Assert.That(ProfileJson.Serialize(ProfileJson.Deserialize(invariant)), Is.EqualTo(invariant));
            }

            using (CultureScope.Use(CultureScope.ThaiOrHostile()))
            {
                Assert.That(ProfileJson.Serialize(p), Is.EqualTo(invariant), "calendario gregoriano aunque la cultura use otro");
                Assert.That(ProfileJson.Deserialize(invariant).CreatedAt, Is.EqualTo(SampleProfiles.T0));
            }
        }

        [Test]
        public void LargestPlausibleProfile_IsUnder100KB()
        {
            string json = ProfileJson.Serialize(SampleProfiles.Largest());
            int bytes = System.Text.Encoding.UTF8.GetByteCount(json);

            Assert.That(bytes, Is.LessThan(ProfileJson.MaxBytes), "08 §4.3: blob < 100 KB");
            Assert.That(ProfileJson.MaxBytes, Is.EqualTo(100 * 1024));
        }

        [Test]
        public void MissingKeys_TakeDefaults_AndUnknownKeysAreIgnored()
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 1, \"consent\": {\"analytics\": true}, \"wallet\": {\"seeds\": 30, \"gems\": 9}}");

            Assert.That(p.Wallet.Seeds, Is.EqualTo(30));
            Assert.That(p.Wallet.Canoes.Count, Is.EqualTo(0));
            Assert.That(p.Progress.NextLevel, Is.EqualTo(1));
            Assert.That(p.Progress.SkippedLevels, Is.Empty);
            Assert.That(p.LevelState, Is.Null);
            Assert.That(p.EconomyTs, Is.Null);
            Assert.That(ProfileJson.Serialize(p).Contains("gems"), Is.False);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("[]")]
        [TestCase("{\"wallet\": {\"seeds\": 1}}")]
        [TestCase("{\"profile_schema\": \"1\"}")]
        [TestCase("{\"profile_schema\": -1}")]
        [TestCase("{\"profile_schema\": 2}")]
        [TestCase("{\"profile_schema\": 1, \"wallet\": {\"seeds\": \"12\"}}")]
        [TestCase("{\"profile_schema\": 1, \"wallet\": {\"seeds\": 1.5}}")]
        [TestCase("{\"profile_schema\": 1, \"wallet\": {\"seeds\": 3000000000}}")]
        [TestCase("{\"profile_schema\": 1, \"wallet\": []}")]
        [TestCase("{\"profile_schema\": 1, \"economy_ts\": \"2026-10-05 14:03:11\"}")]
        [TestCase("{\"profile_schema\": 1, \"economy_ts\": \"2026-02-30T00:00:00Z\"}")]
        [TestCase("{\"profile_schema\": 1, \"progress\": {\"skipped_levels\": [1, \"2\"]}}")]
        [TestCase("{\"profile_schema\": 1, \"level_state\": {\"seed\": null}}")]
        [TestCase("{\"profile_schema\": 1, \"entitlements\": {\"no_ads\": 1}}")]
        [TestCase("{\"profile_schema\": 1, \"entitlements\": {\"skins\": [\"af_skin_m02\", 3]}}")]
        [TestCase("{\"profile_schema\": 1, \"entitlements\": {\"skins\": \"af_skin_m02\"}}")]
        [TestCase("{\"profile_schema\": 1, \"ads\": {\"last_iap\": 5}}")]
        [TestCase("{\"profile_schema\": 1, \"ads\": []}")]
        [TestCase("{\"profile_schema\": 1, \"daily\": {\"af_rescue_hidden_until\": \"ayer\"}}")]
        [TestCase("{\"profile_schema\": 1} x")]
        [TestCase("{\"profile_schema\": 1, \"profile_schema\": 1}")]
        [TestCase("{\"profile_schema\": 1, \"wallet\": {\"seeds\": 1}")]
        public void WrongShapesOrTypes_AreFormatErrors(string json)
        {
            Assert.Throws<FormatException>(() => ProfileJson.Deserialize(json));
        }

        [TestCase("2026-10-05T14:03:11Z", 0L)]
        [TestCase("2026-10-05T14:03:11.5Z", 5000000L)]
        [TestCase("2026-10-05T14:03:11.1234567Z", 1234567L)]
        [TestCase("2026-10-05T14:03:11.0000001Z", 1L)]
        public void Timestamps_ParseTheirOwnFormat(string text, long fractionTicks)
        {
            DateTime value = ProfileJson.ParseTimestamp(text);

            Assert.That(value.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(value, Is.EqualTo(new DateTime(2026, 10, 5, 14, 3, 11, DateTimeKind.Utc).AddTicks(fractionTicks)));
            Assert.That(ProfileJson.FormatTimestamp(value), Is.EqualTo(text));
        }

        [TestCase("2026-10-05T14:03:11")]
        [TestCase("2026-10-05T14:03:11.Z")]
        [TestCase("2026-10-05T14:03:11.12345678Z")]
        [TestCase("2026-10-05T14:03:11+00:00")]
        [TestCase("2026-13-05T14:03:11Z")]
        [TestCase("2026-10-05T24:00:00Z")]
        [TestCase("2026/10/05T14:03:11Z")]
        [TestCase("２026-10-05T14:03:11Z")]
        [TestCase("")]
        public void Timestamps_RejectOtherFormats(string text)
        {
            Assert.Throws<FormatException>(() => ProfileJson.ParseTimestamp(text));
        }

        [Test]
        public void LocalTimes_AreWrittenAsUtc()
        {
            DateTime utc = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

            Assert.That(ProfileJson.FormatTimestamp(utc.ToLocalTime()), Is.EqualTo("2026-07-01T12:00:00Z"));
        }

        [Test]
        public void Clone_IsADeepCopy()
        {
            Profile original = SampleProfiles.Full();
            Profile clone = original.Clone();

            clone.Wallet.Seeds = 1;
            clone.Progress.SkippedLevels.Add(99);
            clone.LevelState!.RevealedCuias.Clear();

            Assert.That(original.Wallet.Seeds, Is.EqualTo(1240));
            Assert.That(original.Progress.SkippedLevels, Is.EqualTo(new[] { 63, 71 }));
            Assert.That(original.LevelState!.RevealedCuias, Is.EqualTo(new[] { 5, 0 }));
        }
    }
}
