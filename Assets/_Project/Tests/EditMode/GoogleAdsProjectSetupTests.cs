#nullable enable
using System.IO;
using AF.Ads;
using AF.Ads.Google.Editor;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El App ID de AdMob en el build (D-049): el asset versionado del plugin Google Mobile Ads tiene los ajustes de
    /// <see cref="GoogleAdsProjectSetup"/> (App ID de Tintalú, SDK Standard, pre-proceso de Gradle) y <c>BuildDev</c> y
    /// <c>BuildRelease</c> los comprueban (y aplican) antes de construir, para que ningún build salga con el App ID vacío.
    /// </summary>
    public sealed class GoogleAdsProjectSetupTests
    {
        [Test]
        public void TheProjectSettings_AreTheFixedOnes()
        {
            Assert.That(GoogleAdsProjectSetup.Validate(), Is.Null, "ejecuta AF.Ads.Google.Editor.GoogleAdsProjectSetup.Apply");
        }

        [Test]
        public void TheVersionedAsset_HasTheAndroidAppId()
        {
            string text = File.ReadAllText(GoogleAdsProjectSetup.SettingsAssetPath);
            StringAssert.Contains("adMobAndroidAppId: " + GoogleAdUnits.AndroidAppId, text);
            StringAssert.Contains("enableGradleBuildPreProcessor: 1", text);
        }

        [Test]
        public void AnEmptyOrForeignAppId_OrTheNextGenSdk_FailTheRule()
        {
            const string id = GoogleAdUnits.AndroidAppId;
            Assert.That(GoogleAdsProjectSetup.Validate(id, true, false, 0), Is.Null);
            Assert.That(GoogleAdsProjectSetup.Validate(id, true, true, 0), Is.Null, "Standard elegido a mano también vale");
            Assert.That(GoogleAdsProjectSetup.Validate("", true, false, 0), Does.Contain("vacio"));
            Assert.That(GoogleAdsProjectSetup.Validate(null, true, false, 0), Does.Contain("vacio"));
            Assert.That(GoogleAdsProjectSetup.Validate("ca-app-pub-3940256099942544~3347511713", true, false, 0), Does.Contain("distinto"));
            Assert.That(GoogleAdsProjectSetup.Validate(id, false, false, 0), Does.Contain("Gradle"));
            Assert.That(GoogleAdsProjectSetup.Validate(id, true, true, 1), Does.Contain("Next-Gen"));
        }

        [Test]
        public void BuildDevAndBuildRelease_CheckTheGoogleSettings_BeforeBuilding()
        {
            foreach (string path in new[] { "Assets/_Project/Editor/CI/BuildDev.cs", "Assets/_Project/Editor/CI/BuildRelease.cs" })
            {
                string source = File.ReadAllText(path);
                int check = source.IndexOf("GoogleAdsProjectSetup.CheckForBuild(LogPrefix)", System.StringComparison.Ordinal);
                int build = source.IndexOf("BuildPipeline.BuildPlayer", System.StringComparison.Ordinal);
                Assert.That(check, Is.GreaterThan(0), path);
                Assert.That(build, Is.GreaterThan(check), path + ": la compuerta va antes del build");
            }
        }
    }
}
