#nullable enable
using System;
using NUnit.Framework;

namespace AF.Save.Tests
{
    /// <summary>
    /// <c>settings.reduce_motion</c> y <c>settings.high_contrast</c> [P] (bloque B9; 07 §7.8 y §3, regla 5; 08 §4.3): defaults
    /// (<c>auto</c> y apagado), ida y vuelta, que solo se escriben cuando dejan su default (un perfil que no los usa no cambia) y
    /// lectura tolerante (lo ausente o inválido toma el default; nunca hace corrupto un perfil). Sin migración, como
    /// <c>map_seen_level</c>.
    /// </summary>
    public sealed class A11ySettingsProfileTests
    {
        [Test]
        public void NewProfile_ReduceMotionAuto_AndHighContrastOff_AndNeitherIsWritten()
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2}");
            Assert.That(p.Settings.ReduceMotion, Is.EqualTo(ReduceMotionSetting.Auto));
            Assert.That(p.Settings.HighContrast, Is.False);
            string json = ProfileJson.Serialize(p);
            StringAssert.DoesNotContain("reduce_motion", json, "con su default no se escribe: los perfiles de antes no cambian");
            StringAssert.DoesNotContain("high_contrast", json);
        }

        [TestCase(ReduceMotionSetting.On, "on")]
        [TestCase(ReduceMotionSetting.Off, "off")]
        public void ReduceMotion_RoundTrips(ReduceMotionSetting value, string text)
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2}");
            p.Settings.ReduceMotion = value;
            p.Settings.HighContrast = true;
            string json = ProfileJson.Serialize(p);
            StringAssert.Contains("\"reduce_motion\": \"" + text + "\"", json);
            StringAssert.Contains("\"high_contrast\": true", json);
            Profile copy = ProfileJson.Deserialize(json);
            Assert.That(copy.Settings.ReduceMotion, Is.EqualTo(value));
            Assert.That(copy.Settings.HighContrast, Is.True);
        }

        [TestCase("\"sí\"")]
        [TestCase("true")]
        [TestCase("3")]
        [TestCase("null")]
        public void AnInvalidReduceMotion_IsAuto(string raw)
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2, \"settings\": {\"reduce_motion\": " + raw + ", \"sound\": false}}");
            Assert.That(p.Settings.ReduceMotion, Is.EqualTo(ReduceMotionSetting.Auto));
            Assert.That(p.Settings.Sound, Is.False, "los demás ajustes se leen igual");
        }

        [Test]
        public void AnInvalidHighContrast_IsOff()
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2, \"settings\": {\"high_contrast\": \"sí\"}}");
            Assert.That(p.Settings.HighContrast, Is.False);
        }

        [Test]
        public void ReduceMotion_RejectsAValueOutsideTheEnum()
        {
            Profile p = ProfileJson.Deserialize("{\"profile_schema\": 2}");
            Assert.Throws<ArgumentOutOfRangeException>(() => p.Settings.ReduceMotion = (ReduceMotionSetting)7);
        }
    }
}
