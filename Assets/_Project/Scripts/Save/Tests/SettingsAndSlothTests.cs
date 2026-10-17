#nullable enable
using System;
using System.IO;
using System.Text;
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// <c>settings</c> y <c>sloth</c> del perfil (S12; 08 §4.3, D-016 punto 4 y d.10, D-018 Q7-A y §3.5): defaults
    /// (<c>language</c> = <c>auto</c>, <c>patterns</c> = <c>auto</c>, <c>sloth_messages</c> e <c>idle_hint</c> encendidos
    /// y ninguna frase vista), ida y vuelta, y lectura que nunca hace corrupto un perfil por un ajuste: lo ausente o
    /// inválido toma el default, campo a campo. Un perfil del esquema 1 escrito antes de S12 carga y se guarda con los
    /// defaults, sin migrar (<c>CurrentSchema</c> = 1).
    /// </summary>
    public sealed class SettingsAndSlothTests
    {
        private static readonly int[] NoLines = { -1, -1, -1, -1, -1, -1, -1, -1 };

        // Perfil del esquema 1 tal como lo escribía la build anterior a S12 (sin settings, sloth ni map_seen_level).
        private const string PreS12Json = "{\"profile_schema\": 1, \"created_at\": \"2026-10-05T13:10:00Z\","
            + " \"wallet\": {\"seeds\": 120, \"canoes\": {\"count\": 4}},"
            + " \"progress\": {\"max_level\": 12, \"next_level\": 13, \"skipped_levels\": [], \"content_version\": \"1.0.0\"}}";

        private string? _dir;

        [TearDown]
        public void TearDown()
        {
            TempDirs.Delete(_dir);
            _dir = null;
        }

        [Test]
        public void NewProfile_HasTheDefaults()
        {
            Profile p = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);

            AssertDefaults(p);
            Assert.That(SettingsData.LanguageAuto, Is.EqualTo("auto"), "D-018 Q7-A: seguir al teléfono");
            Assert.That(SettingsData.Languages, Is.EqualTo(new[] { "es-419", "pt-BR", "en", "en-XA" }), "tanda 1 y en-XA de desarrollo");
            Assert.That(SlothData.MomentCount, Is.EqualTo(8));
            Assert.That(SlothData.Moments, Is.EqualTo(new[] { "win", "hard", "fail", "restart", "streak", "return", "debut", "milestone" }),
                "orden de D-016 punto 4: el índice de last_line se guarda");
        }

        [Test]
        public void ProfileWithoutTheFields_LoadsAndSavesWithTheDefaults()
        {
            Profile p = ProfileJson.Deserialize(PreS12Json);

            AssertDefaults(p);
            Assert.That(p.Wallet.Seeds, Is.EqualTo(120));
            Assert.That(p.Progress.MaxLevel, Is.EqualTo(12));
            string json = ProfileJson.Serialize(p);
            StringAssert.Contains("\"profile_schema\": 1,", json);
            StringAssert.Contains("\"settings\": {\n    \"sound\": true,\n    \"music\": true,\n    \"haptics\": true,\n    \"patterns\": \"auto\",\n    \"language\": \"auto\",\n    \"sloth_messages\": true,\n    \"idle_hint\": true\n  }", json);
            StringAssert.Contains("\"last_line\": [-1, -1, -1, -1, -1, -1, -1, -1]", json);
            StringAssert.Contains("\"map_seen_level\": 0", json);
        }

        [Test]
        public void ProfileWithoutTheFields_OnDisk_LoadsWithoutMigrating_AndIsSavedWithTheDefaults()
        {
            _dir = TempDirs.Create();
            var store = new LocalProfileStore(_dir);
            File.WriteAllText(store.MainPath, PreS12Json, new UTF8Encoding(false));

            ProfileLoadResult load = store.Load();

            Assert.That(load.Status, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(load.Migrated, Is.False, "esquema 1 sin migrar (08 §4.3)");
            AssertDefaults(load.Profile!);

            load.Profile!.Settings.Language = "es-419";
            store.Save(load.Profile);
            Profile again = new LocalProfileStore(_dir).Load().Profile!;
            Assert.That(again.Settings.Language, Is.EqualTo("es-419"));
            Assert.That(again.Settings.Patterns, Is.EqualTo(PatternsSetting.Auto));
            Assert.That(again.Sloth.LastLine, Is.EqualTo(NoLines));
            Assert.That(again.Wallet.Seeds, Is.EqualTo(120));
        }

        [Test]
        public void SettingsAndSloth_RoundTrip()
        {
            Profile p = Profile.CreateNew(new ManualClock(SampleProfiles.T0), 5);
            p.Settings.Patterns = PatternsSetting.On;
            p.Settings.Language = "en-XA";
            p.Settings.SlothMessages = false;
            p.Settings.Sound = false;
            p.Settings.Music = false;
            p.Settings.Haptics = false;
            p.Settings.IdleHint = false;
            p.Sloth.LastLine[0] = 7;
            p.Sloth.LastLine[7] = 1;
            p.Sloth.LastGreetingDate = "2026-02-28";
            p.Sloth.LastSeenDate = "2024-02-29";

            Profile copy = ProfileJson.Deserialize(ProfileJson.Serialize(p));

            Assert.That(copy.Settings.Patterns, Is.EqualTo(PatternsSetting.On));
            Assert.That(copy.Settings.Language, Is.EqualTo("en-XA"));
            Assert.That(copy.Settings.SlothMessages, Is.False);
            Assert.That(copy.Settings.IdleHint, Is.False);
            Assert.That(copy.Settings.Sound, Is.False, "AU1");
            Assert.That(copy.Settings.Music, Is.False);
            Assert.That(copy.Settings.Haptics, Is.False);
            Assert.That(copy.Sloth.LastLine, Is.EqualTo(new[] { 7, -1, -1, -1, -1, -1, -1, 1 }));
            Assert.That(copy.Sloth.LastGreetingDate, Is.EqualTo("2026-02-28"));
            Assert.That(copy.Sloth.LastSeenDate, Is.EqualTo("2024-02-29"));
            Assert.That(SampleProfiles.Describe(p.Clone()), Is.EqualTo(SampleProfiles.Describe(p)), "Clone copia los dos bloques");
        }

        [TestCase("off", PatternsSetting.Off)]
        [TestCase("on", PatternsSetting.On)]
        [TestCase("auto", PatternsSetting.Auto)]
        [TestCase("OFF", PatternsSetting.Auto)]
        [TestCase("siempre", PatternsSetting.Auto)]
        [TestCase("", PatternsSetting.Auto)]
        public void Patterns_UnknownText_IsAuto(string text, PatternsSetting expected)
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 1, \"settings\": {\"patterns\": \"" + text + "\"}}");

            Assert.That(p.Settings.Patterns, Is.EqualTo(expected));
        }

        [TestCase("es-419", "es-419")]
        [TestCase("ES-419", "es-419")]
        [TestCase("pt-br", "pt-BR")]
        [TestCase("en", "en")]
        [TestCase("en-xa", "en-XA")]
        [TestCase("Auto", "auto")]
        [TestCase("fr", null)]
        [TestCase("es", null)]
        [TestCase("es-CO", null)]
        [TestCase("en-US", null)]
        [TestCase(" en", null)]
        [TestCase("", null)]
        public void Language_IsAutoOrASupportedCode(string text, string? canonical)
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 1, \"settings\": {\"language\": \"" + text + "\"}}");

            Assert.That(SettingsData.NormalizeLanguage(text), Is.EqualTo(canonical));
            Assert.That(p.Settings.Language, Is.EqualTo(canonical ?? "auto"), "un código que no se admite vuelve a auto");
        }

        [TestCase("{\"profile_schema\": 1, \"settings\": 5, \"sloth\": \"x\"}")]
        [TestCase("{\"profile_schema\": 1, \"settings\": [], \"sloth\": []}")]
        [TestCase("{\"profile_schema\": 1, \"settings\": null, \"sloth\": null}")]
        [TestCase("{\"profile_schema\": 1, \"settings\": {\"patterns\": 1, \"language\": 5, \"sloth_messages\": \"no\", \"idle_hint\": 0, \"sound\": \"off\", \"music\": 0, \"haptics\": []}}")]
        [TestCase("{\"profile_schema\": 1, \"settings\": {\"patterns\": null, \"language\": null, \"sloth_messages\": null, \"idle_hint\": null, \"sound\": null, \"music\": null, \"haptics\": null}}")]
        [TestCase("{\"profile_schema\": 1, \"sloth\": {\"last_line\": 3, \"last_greeting_date\": 20261005, \"last_seen_date\": true}}")]
        [TestCase("{\"profile_schema\": 1, \"sloth\": {\"last_line\": null, \"last_greeting_date\": \"ayer\", \"last_seen_date\": \"2026-02-30\"}}")]
        [TestCase("{\"profile_schema\": 1, \"sloth\": {\"last_line\": [\"1\", 1.5, -2, null, true, 3000000000, [], {}], \"last_seen_date\": \"2026-10-5\"}}")]
        [TestCase("{\"profile_schema\": 1, \"sloth\": {\"last_greeting_date\": \"2026-10-05T00:00:00Z\", \"last_seen_date\": \"2026/10/05\"}}")]
        public void InvalidValues_TakeTheDefaults_WithoutCorruptingTheProfile(string json)
        {
            Profile p = ProfileJson.Deserialize(json);

            AssertDefaults(p);
        }

        [Test]
        public void LastLine_OfAnotherLength_KeepsWhatFits()
        {
            Profile shorter = ProfileJson.Deserialize("{\"profile_schema\": 1, \"sloth\": {\"last_line\": [3, 0, -1]}}");
            Profile longer = ProfileJson.Deserialize("{\"profile_schema\": 1, \"sloth\": {\"last_line\": [1, 2, 3, 4, 5, 6, 7, 0, 9, 9]}}");
            Profile mixed = ProfileJson.Deserialize("{\"profile_schema\": 1, \"sloth\": {\"last_line\": [2, \"x\", 4, -5, 1]}}");

            Assert.That(shorter.Sloth.LastLine, Is.EqualTo(new[] { 3, 0, -1, -1, -1, -1, -1, -1 }));
            Assert.That(longer.Sloth.LastLine, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 0 }));
            Assert.That(mixed.Sloth.LastLine, Is.EqualTo(new[] { 2, -1, 4, -1, 1, -1, -1, -1 }));
            StringAssert.Contains("\"last_line\": [1, 2, 3, 4, 5, 6, 7, 0]", ProfileJson.Serialize(longer), "siempre se escriben 8");
        }

        [Test]
        public void ValidFields_SurviveNextToInvalidOnes()
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 1,"
                + " \"settings\": {\"patterns\": \"on\", \"language\": \"klingon\", \"sloth_messages\": false, \"idle_hint\": \"no\"},"
                + " \"sloth\": {\"last_line\": [4], \"last_greeting_date\": \"2026-10-05\", \"last_seen_date\": 7}}");

            Assert.That(p.Settings.Patterns, Is.EqualTo(PatternsSetting.On));
            Assert.That(p.Settings.Language, Is.EqualTo("auto"));
            Assert.That(p.Settings.SlothMessages, Is.False);
            Assert.That(p.Settings.IdleHint, Is.True);
            Assert.That(p.Sloth.LastLine[0], Is.EqualTo(4));
            Assert.That(p.Sloth.LastGreetingDate, Is.EqualTo("2026-10-05"));
            Assert.That(p.Sloth.LastSeenDate, Is.Null);
        }

        [Test]
        public void Setters_RejectWhatTheFileCouldNotHold()
        {
            var settings = new SettingsData();
            var sloth = new SlothData();

            Assert.Throws<ArgumentException>(() => settings.Language = "fr");
            Assert.Throws<ArgumentException>(() => settings.Language = null!);
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.Patterns = (PatternsSetting)3);
            Assert.Throws<ArgumentException>(() => sloth.LastGreetingDate = "2026-13-01");
            Assert.Throws<ArgumentException>(() => sloth.LastSeenDate = "hoy");
            Assert.That(settings.Language, Is.EqualTo("auto"), "un valor rechazado no cambia nada");
            Assert.That(sloth.LastGreetingDate, Is.Null);

            settings.Language = "PT-BR";
            sloth.LastSeenDate = "2026-10-05";
            sloth.LastSeenDate = null;
            Assert.That(settings.Language, Is.EqualTo("pt-BR"), "se guarda en su forma canónica");
            Assert.That(sloth.LastSeenDate, Is.Null);
            Assert.That(sloth.LastLine.Length, Is.EqualTo(SlothData.MomentCount));
        }

        [Test]
        public void DateKeys_AreTheOnesOfTheClock()
        {
            var clock = new ManualClock(new DateTime(2026, 10, 6, 2, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(-5));
            string today = clock.LocalDateKey();

            Assert.That(today, Is.EqualTo("2026-10-05"), "fecha local de Bogotá");
            Assert.That(SlothData.IsDateKey(today), Is.True);
            using (CultureScope.Use(CultureScope.ThaiOrHostile()))
            {
                Assert.That(SlothData.IsDateKey("2026-10-05"), Is.True, "calendario gregoriano aunque la cultura use otro");
                Assert.That(SlothData.IsDateKey("2569-10-05"), Is.True);
                Assert.That(SlothData.IsDateKey("2026-02-29"), Is.False);
            }

            Assert.That(SlothData.IsDateKey(null), Is.False);
            Assert.That(SlothData.IsDateKey("２026-10-05"), Is.False);
            Assert.That(SlothData.IsDateKey("2026-10-05 "), Is.False);
        }

        private static void AssertDefaults(Profile p)
        {
            Assert.That(p.ProfileSchema, Is.EqualTo(1));
            Assert.That(p.Settings.Patterns, Is.EqualTo(PatternsSetting.Auto));
            Assert.That(p.Settings.Language, Is.EqualTo("auto"));
            Assert.That(p.Settings.Sound, Is.True, "AU1: audio_sfx_default_on = true (08 §9)");
            Assert.That(p.Settings.Music, Is.True, "AU1: audio_music_default_on = true (08 §9)");
            Assert.That(p.Settings.Haptics, Is.True, "AU1: ui_haptics_default_on = true (08 §9)");
            Assert.That(p.Settings.SlothMessages, Is.True, "D-016 punto 4: activado por defecto");
            Assert.That(p.Settings.IdleHint, Is.True);
            Assert.That(p.Sloth.LastLine, Is.EqualTo(NoLines));
            Assert.That(p.Sloth.LastGreetingDate, Is.Null);
            Assert.That(p.Sloth.LastSeenDate, Is.Null);
            Assert.That(p.Progress.MapSeenLevel, Is.EqualTo(0));
        }
    }
}
