#nullable enable
using System;
using AF.Platform;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// <c>settings.crash_reports</c> [P] (T-L6 de <c>docs/legal/investigacion-legal-mundial.md</c> §11; D-057, añadido del
    /// 2026-10-27; sustituye la «línea sin interruptor» de Q-B12-4): activado por defecto y sin escribirse (un perfil de antes no
    /// cambia), ida y vuelta apagado, lectura tolerante y, en la fusión con la nube, la objeción no se pierde.
    /// </summary>
    public sealed class CrashReportsSettingTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

        private static Profile At(int maxLevel, int seeds)
        {
            Profile p = Profile.CreateNew(new ManualClock(T0), 5);
            p.Progress.MaxLevel = maxLevel;
            p.Progress.NextLevel = maxLevel + 1;
            p.EconomyTs = T0;
            p.Wallet.Seeds = seeds;
            return p;
        }

        [Test]
        public void NewProfile_SendsCrashReports_AndTheFieldIsNotWritten()
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2}");
            Assert.That(p.Settings.CrashReports, Is.True, "activado por defecto (D-057)");
            StringAssert.DoesNotContain("crash_reports", ProfileJson.Serialize(p), "con su default no se escribe");
        }

        [Test]
        public void Off_RoundTrips()
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2}");
            p.Settings.CrashReports = false;
            string json = ProfileJson.Serialize(p);
            StringAssert.Contains("\"crash_reports\": false", json);
            Assert.That(ProfileJson.Deserialize(json).Settings.CrashReports, Is.False);
            Assert.That(p.Clone().Settings.CrashReports, Is.False);
        }

        [TestCase("\"no\"")]
        [TestCase("0")]
        [TestCase("null")]
        public void AnInvalidValue_IsTheDefault(string raw)
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2, \"settings\": {\"crash_reports\": " + raw + ", \"music\": false}}");
            Assert.That(p.Settings.CrashReports, Is.True);
            Assert.That(p.Settings.Music, Is.False, "los demás ajustes se leen igual");
        }

        [Test]
        public void CloudMerge_KeepsTheObjection_FromEitherSide()
        {
            Profile local = At(40, 100);
            Profile cloud = At(50, 900);
            local.Settings.CrashReports = false;
            CloudMergeResult r = CloudMerge.Resolve(local, cloud);
            Assert.That(r.Winner, Is.EqualTo(CloudMergeWinner.Cloud));
            Assert.That(r.Merged.Settings.CrashReports, Is.False, "gana la nube, pero el apagado del dispositivo se queda");

            local.Settings.CrashReports = true;
            cloud.Settings.CrashReports = false;
            Assert.That(CloudMerge.Resolve(local, cloud).Merged.Settings.CrashReports, Is.False, "apagado en el otro dispositivo");

            local.Settings.CrashReports = false;
            cloud.Settings.CrashReports = true;
            Assert.That(CloudMerge.Resolve(local, cloud, localCoherent: false).Merged.Settings.CrashReports, Is.False,
                "también si la nube gana por la firma");

            local.Settings.CrashReports = true;
            Assert.That(CloudMerge.Resolve(local, cloud).Merged.Settings.CrashReports, Is.True, "los dos encendidos");
        }
    }
}
