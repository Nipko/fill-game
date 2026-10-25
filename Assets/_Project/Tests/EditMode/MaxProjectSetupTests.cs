#nullable enable
using System.IO;
using AF.Ads.Max.Editor;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Ninguna clave de MAX en el repositorio (D-042; regla dura 5): los ajustes versionados del plugin son los de
    /// <see cref="MaxProjectSetup"/> (sin clave, sin Quality Service y, desde D-049, sin su UMP) y <c>BuildDev</c> y <c>BuildRelease</c> se paran si no.
    /// </summary>
    public sealed class MaxProjectSetupTests
    {
        [Test]
        public void TheProjectSettings_AreTheFixedOnes()
        {
            Assert.That(MaxProjectSetup.Validate(), Is.Null, "ejecuta AF.Ads.Max.Editor.MaxProjectSetup.Apply");
        }

        [Test]
        public void TheVersionedAsset_HasNoSdkKey_AndNoQualityService()
        {
            string text = File.ReadAllText("Assets/MaxSdk/Resources/AppLovinSettings.asset");
            StringAssert.Contains("qualityServiceEnabled: 0", text);
            Assert.That(text, Does.Match(@"(?m)^\s*sdkKey:\s*$"), "sdkKey vacía en el repositorio");
        }

        [Test]
        public void AKeyOrTheQualityService_FailTheRule()
        {
            const string url = MaxProjectSetup.PrivacyPolicyUrl;
            Assert.That(MaxProjectSetup.Validate(false, "", false, url), Is.Null);
            Assert.That(MaxProjectSetup.Validate(false, null, false, url), Is.Null);
            Assert.That(MaxProjectSetup.Validate(false, "abc", false, url), Does.Contain("clave"));
            Assert.That(MaxProjectSetup.Validate(true, "", false, url), Does.Contain("Quality Service"));
            Assert.That(MaxProjectSetup.Validate(false, "", true, url), Does.Contain("D-049"), "con AdMob directo, el UMP de MAX va apagado");
            Assert.That(MaxProjectSetup.Validate(false, "", false, "https://example.com/"), Is.Not.Null);
        }

        [Test]
        public void BuildDevAndBuildRelease_CheckTheMaxSettings_BeforeBuilding()
        {
            foreach (string path in new[] { "Assets/_Project/Editor/CI/BuildDev.cs", "Assets/_Project/Editor/CI/BuildRelease.cs" })
            {
                string source = File.ReadAllText(path);
                int check = source.IndexOf("MaxProjectSetup.CheckForBuild(LogPrefix)", System.StringComparison.Ordinal);
                int build = source.IndexOf("BuildPipeline.BuildPlayer", System.StringComparison.Ordinal);
                Assert.That(check, Is.GreaterThan(0), path);
                Assert.That(build, Is.GreaterThan(check), path + ": la compuerta va antes del build");
            }
        }
    }
}
