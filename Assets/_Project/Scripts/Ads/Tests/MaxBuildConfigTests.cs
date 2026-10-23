#nullable enable
#if UNITY_ANDROID
using System;
using System.IO;
using AF.Ads.Max.Editor;
using NUnit.Framework;

namespace AF.Ads.Tests
{
    /// <summary>
    /// La clave y las unidades de MAX entran en el proyecto de Gradle desde fuera del repositorio (D-042; regla dura 5): con las tres
    /// variables de entorno, <see cref="MaxBuildConfig"/> pone la clave en <c>applovin_settings.json</c> (conservando lo que escribió
    /// AppLovin) y las unidades en <c>af_max_config.xml</c>; sin ellas (ni archivo local), no escribe nada y borra un resto anterior.
    /// Valores inventados: nunca claves reales en los tests.
    /// </summary>
    public sealed class MaxBuildConfigTests
    {
        private static readonly string[] Vars = { "AF_MAX_SDK_KEY", "AF_MAX_ANDROID_REWARDED_UNIT", "AF_MAX_ANDROID_INTERSTITIAL_UNIT" };
        private string _dir = "";
        private readonly string?[] _saved = new string?[3];

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "af-maxcfg-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_dir, "src/main/res/raw"));
            File.WriteAllText(Path.Combine(_dir, "src/main/res/raw/applovin_settings.json"), "{\"sdk_key\":\"\",\"render_outside_safe_area\":false}\n");
            for (int i = 0; i < Vars.Length; i++)
            {
                _saved[i] = Environment.GetEnvironmentVariable(Vars[i]);
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < Vars.Length; i++)
            {
                Environment.SetEnvironmentVariable(Vars[i], _saved[i]);
            }

            Directory.Delete(_dir, true);
        }

        [Test]
        public void WithTheEnvironment_WritesKeyAndUnits()
        {
            Environment.SetEnvironmentVariable(Vars[0], "test-sdk-key");
            Environment.SetEnvironmentVariable(Vars[1], "rv0000test");
            Environment.SetEnvironmentVariable(Vars[2], "in0000test");

            new MaxBuildConfig().OnPostGenerateGradleAndroidProject(_dir);

            string settings = File.ReadAllText(Path.Combine(_dir, "src/main/res/raw/applovin_settings.json"));
            StringAssert.Contains("\"sdk_key\":\"test-sdk-key\"", settings);
            StringAssert.Contains("render_outside_safe_area", settings);
            string xml = File.ReadAllText(Path.Combine(_dir, "src/main/res/values/af_max_config.xml"));
            StringAssert.Contains("name=\"af_max_configured\" translatable=\"false\">true<", xml);
            StringAssert.Contains(">rv0000test<", xml);
            StringAssert.Contains(">in0000test<", xml);
        }

        [Test]
        public void WithoutConfiguration_WritesNothing_AndRemovesALeftover()
        {
            if (File.Exists(MaxBuildConfig.LocalSecretsPath))
            {
                Assert.Ignore("Hay un archivo local de MAX en esta máquina: la prueba sin configuración no aplica.");
            }

            Environment.SetEnvironmentVariable(Vars[0], null);
            Environment.SetEnvironmentVariable(Vars[1], null);
            Environment.SetEnvironmentVariable(Vars[2], null);
            string values = Path.Combine(_dir, "src/main/res/values");
            Directory.CreateDirectory(values);
            File.WriteAllText(Path.Combine(values, "af_max_config.xml"), "<resources/>");

            new MaxBuildConfig().OnPostGenerateGradleAndroidProject(_dir);

            Assert.That(File.Exists(Path.Combine(values, "af_max_config.xml")), Is.False);
            StringAssert.Contains("\"sdk_key\":\"\"", File.ReadAllText(Path.Combine(_dir, "src/main/res/raw/applovin_settings.json")));
        }
    }
}
#endif
