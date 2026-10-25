#nullable enable
using System.IO;
using AF.EditorTools;
using AF.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// D-051, puntos 1 y 2: sin la pantalla «Made with Unity» (la aplica <c>ProjectSetup.ApplyAndroidDefaults</c> y la exige su
    /// <c>Validate</c>, que es la comprobación de Player Settings de BuildSmoke, BuildDev y BuildRelease: código 2), y la pantalla
    /// del sistema de Android 12+ con el icono v2 sobre el petróleo de la marca, el mismo de la intro (<see cref="BrandIntro"/>).
    /// </summary>
    public sealed class BrandSplashSettingsTests
    {
        private const string Res = "Assets/_Project/Plugins/Android/AfIcon.androidlib/src/main/res/";

        [Test]
        public void TheUnitySplashIsOff_AsApplyAndroidDefaultsLeftIt()
        {
            Assert.That(ProjectSetup.ShowUnitySplash, Is.False, "D-051, punto 1");
            Assert.That(PlayerSettings.SplashScreen.show, Is.False, "ProjectSettings.asset tal como lo escribió ApplyAndroidDefaults");
            Assert.That(PlayerSettings.SplashScreen.showUnityLogo, Is.False);
            Assert.That((Color32)PlayerSettings.SplashScreen.backgroundColor, Is.EqualTo(ProjectSetup.BrandPetroleum));
            Assert.That(ProjectSetup.Validate(), Is.Empty, "ejecuta AF.EditorTools.ProjectSetup.ApplyAndroidDefaults");
        }

        [Test]
        public void TheBuildCheck_RejectsTheUnitySplash()
        {
            bool show = PlayerSettings.SplashScreen.show;
            bool logo = PlayerSettings.SplashScreen.showUnityLogo;
            try
            {
                PlayerSettings.SplashScreen.show = true;
                Assert.That(string.Join("; ", ProjectSetup.Validate()), Does.Contain("splashScreen.show="), "BuildSmoke/BuildDev/BuildRelease salen con 2");
            }
            finally
            {
                PlayerSettings.SplashScreen.show = show;
                PlayerSettings.SplashScreen.showUnityLogo = logo;
            }

            Assert.That(ProjectSetup.Validate(), Is.Empty);
        }

        [Test]
        public void TheBrandPetroleum_IsTheSameInTheSettings_TheSystemSplash_AndTheIntro()
        {
            Assert.That(ProjectSetup.BrandPetroleum, Is.EqualTo(new Color32(0x14, 0x44, 0x4A, 0xFF)), "#14444A (D-045)");
            Assert.That(BrandIntro.Petroleum, Is.EqualTo(ProjectSetup.BrandPetroleum), "la intro empalma con la pantalla del sistema");
            string values = File.ReadAllText(Res + "values/af_splash.xml");
            StringAssert.Contains("<color name=\"af_brand_petroleum\">#14444A</color>", values);
            StringAssert.Contains("<style name=\"AfLaunchTheme\" parent=\"BaseUnityGameActivityTheme\">", values);
            StringAssert.Contains("<item name=\"android:windowBackground\">@color/af_brand_petroleum</item>", values);
        }

        [Test]
        public void TheSystemSplash_ShowsTheV2IconOnThePetroleum_AndTheActivityUsesIt()
        {
            string v31 = File.ReadAllText(Res + "values-v31/af_splash.xml");
            StringAssert.Contains("<item name=\"android:windowSplashScreenBackground\">@color/af_brand_petroleum</item>", v31);
            StringAssert.Contains("<item name=\"android:windowSplashScreenAnimatedIcon\">@mipmap/af_launcher</item>", v31, "el icono adaptativo v2 (D-045)");
            Assert.That(File.Exists(Res + "mipmap-anydpi-v26/af_launcher.xml"), Is.True);

            string launcher = File.ReadAllText("Assets/Plugins/Android/LauncherManifest.xml");
            StringAssert.Contains("<activity android:name=\"com.unity3d.player.UnityPlayerGameActivity\"", launcher);
            StringAssert.Contains("android:theme=\"@style/AfLaunchTheme\"", launcher);
            StringAssert.Contains("tools:replace=\"android:theme\"", launcher);
        }
    }
}
