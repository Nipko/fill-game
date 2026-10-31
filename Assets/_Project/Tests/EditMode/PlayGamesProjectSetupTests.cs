#nullable enable
using System.IO;
using AF.EditorTools;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El <c>APP_ID</c> de Play Games en los archivos versionados del plugin (<c>play-games.md</c> §9, paso 3): el Setup aplicado por
    /// <see cref="PlayGamesProjectSetup"/> deja el manifiesto, el asset y los ajustes de proyecto con el <c>app_id</c> de Play Console,
    /// sin Web Client ID ni Nearby (D-042), y sin el <c>applicationId</c> definitivo (regla dura 6).
    /// </summary>
    public sealed class PlayGamesProjectSetupTests
    {
        [Test]
        public void TheVersionedFiles_CarryTheAppId()
        {
            Assert.That(PlayGamesProjectSetup.Validate(), Is.Null, "ejecuta AF.EditorTools.PlayGamesProjectSetup.Apply");
        }

        [Test]
        public void TheProjectSettings_HaveTheAppId_AndNoBundleId()
        {
            string text = File.ReadAllText(PlayGamesProjectSetup.ProjectSettingsPath);
            StringAssert.Contains("proj.AppId=" + PlayGamesProjectSetup.AppId, text);
            Assert.That(text, Does.Not.Contain("tintalu"), "el applicationId definitivo solo vive en la variable de CI");
        }

        [Test]
        public void AnEmptyOrForeignAppId_OrAWebClientId_FailTheRule()
        {
            const string id = PlayGamesProjectSetup.AppId;
            string manifest = "<meta-data android:name=\"com.google.android.gms.games.APP_ID\"\n    android:value=\"\\u003" + id + "\" />";
            string asset = "  mAppId: " + id + "\n  mWebClientId: \n  mNearbyServiceId: \n";
            Assert.That(PlayGamesProjectSetup.Validate(manifest, asset), Is.Null);
            Assert.That(PlayGamesProjectSetup.Validate(manifest.Replace(id, string.Empty), asset), Does.Contain("APP_ID"));
            Assert.That(PlayGamesProjectSetup.Validate(manifest.Replace(id, "123456789012"), asset), Does.Contain("APP_ID"));
            Assert.That(PlayGamesProjectSetup.Validate(manifest, asset.Replace("mAppId: " + id, "mAppId: ")), Does.Contain("mAppId"));
            Assert.That(
                PlayGamesProjectSetup.Validate(manifest, asset.Replace("mWebClientId: ", "mWebClientId: 1-x.apps.googleusercontent.com")),
                Does.Contain("Web Client ID"));
            Assert.That(
                PlayGamesProjectSetup.Validate(manifest + "<meta-data android:name=\"com.google.android.gms.nearby.connection.SERVICE_ID\" />", asset),
                Does.Contain("Nearby"));
        }
    }
}
