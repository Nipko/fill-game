#nullable enable
using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El juego no se muestra ni se juega sobre la pantalla de bloqueo (observación 9 de la jornada 6, 2026-10-28; nota técnica en
    /// <c>docs/dev/decisiones.md</c>). La causa es la variante de desarrollo del reproductor de Unity 6000.3.25f1
    /// (<c>UnityPlayer.developmentPlayerInitialize</c> pone <c>FLAG_TURN_SCREEN_ON | FLAG_SHOW_WHEN_LOCKED | FLAG_DISMISS_KEYGUARD</c>,
    /// y su diálogo del teclado llama a <c>setShowWhenLocked(true)</c>); no hay ajuste del Player que lo apague, así que el plugin
    /// propio <c>AfLockGuard.androidlib</c> lo quita en cada paso del ciclo de vida de la actividad de Unity. Estos tests vigilan
    /// que el plugin siga ahí, que sea solo Java con API de la plataforma y que quite las tres banderas y el modo de la actividad.
    /// </summary>
    public sealed class AfLockGuardPluginTests
    {
        private static string PluginDir => Path.Combine(Application.dataPath, "_Project", "Plugins", "Android", "AfLockGuard.androidlib");

        private static string JavaDir => Path.Combine(PluginDir, "src", "main", "java", "com", "amazonfill", "lockguard");

        private static string Guard => File.ReadAllText(Path.Combine(JavaDir, "AfLockGuard.java")).Replace("\r\n", "\n");

        [Test]
        public void Plugin_IsJavaOnly_WithPlatformApiOnly_AndNoDataOrNetwork()
        {
            Assert.That(Directory.Exists(PluginDir), Is.True, "sin el plugin, dev.apk vuelve a verse sobre la pantalla de bloqueo");
            Assert.That(Directory.GetFiles(PluginDir, "*.kt", SearchOption.AllDirectories), Is.Empty, "sin Kotlin");
            string[] files = Directory.GetFiles(PluginDir, "*.java", SearchOption.AllDirectories);
            Assert.That(files, Has.Length.EqualTo(2), "AfLockGuard y su proveedor");
            string[] banned =
            {
                "java.net", "javax.net", "okhttp", "HttpURLConnection", "Socket", "http:", "https:", "WebView", "System.out",
                "printStackTrace", "SharedPreferences", "FileOutputStream", "getAdvertisingId", "Settings.Secure", "androidx",
                "java.lang.reflect", "getMethod(",
            };
            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                foreach (string word in banned)
                {
                    Assert.That(text.Contains(word), Is.False, Path.GetFileName(file) + ": " + word);
                }

                foreach (Match m in Regex.Matches(text, @"^import\s+([\w\.]+);", RegexOptions.Multiline))
                {
                    string import = m.Groups[1].Value;
                    Assert.That(import.StartsWith("android.", StringComparison.Ordinal), Is.True, Path.GetFileName(file) + " importa " + import);
                }
            }

            string gradle = File.ReadAllText(Path.Combine(PluginDir, "build.gradle"));
            Assert.That(gradle, Does.Contain("namespace 'com.amazonfill.lockguard'"), "AGP 9: namespace en cada .androidlib");
            Assert.That(gradle, Does.Contain("minSdk 26"));
            Assert.That(gradle, Does.Not.Contain("dependencies"), "regla 4: nada de dependencias nuevas");
        }

        [Test]
        public void Manifest_HasOnlyTheUnexportedProvider_AndNoPermissions_NorLockScreenMode()
        {
            string path = Path.Combine(PluginDir, "src", "main", "AndroidManifest.xml");
            Assert.DoesNotThrow(() => new System.Xml.XmlDocument().Load(path), "XML válido (un «--» en un comentario rompe el build)");
            string manifest = File.ReadAllText(path);
            Assert.That(manifest, Does.Not.Contain("uses-permission"));
            Assert.That(manifest, Does.Not.Contain("<activity"), "no sustituye a la actividad de Unity");
            Assert.That(manifest, Does.Not.Contain("showWhenLocked"));
            Assert.That(manifest, Does.Not.Contain("turnScreenOn"));
            StringAssert.Contains("android:name=\"com.amazonfill.lockguard.AfLockGuardProvider\"", manifest);
            StringAssert.Contains("android:authorities=\"${applicationId}.aflockguard\"", manifest);
            StringAssert.Contains("android:exported=\"false\"", manifest);
        }

        [Test]
        public void TheGuard_ClearsTheThreeWindowFlags_AndTheActivityMode()
        {
            string guard = Guard;
            StringAssert.Contains("FLAG_SHOW_WHEN_LOCKED", guard);
            StringAssert.Contains("FLAG_TURN_SCREEN_ON", guard);
            StringAssert.Contains("FLAG_DISMISS_KEYGUARD", guard);
            StringAssert.Contains("window.clearFlags(LOCK_FLAGS);", guard);
            StringAssert.Contains("activity.setShowWhenLocked(false);", guard);
            StringAssert.Contains("activity.setTurnScreenOn(false);", guard);
            StringAssert.Contains("@TargetApi(27)\n    private static final class Api27", guard);

            // Nunca al revés: nada del plugin activa el modo.
            Assert.That(guard, Does.Not.Contain("setShowWhenLocked(true)"));
            Assert.That(guard, Does.Not.Contain("setTurnScreenOn(true)"));
            Assert.That(guard, Does.Not.Contain("addFlags("));
            Assert.That(guard, Does.Not.Contain("requestDismissKeyguard"));
        }

        [Test]
        public void TheGuard_RunsAtEveryLifecycleStep_BeforeTheScreenGoesOff()
        {
            string guard = Guard;
            foreach (string step in new[] { "onActivityCreated", "onActivityPostCreated", "onActivityStarted", "onActivityResumed", "onActivityPaused", "onActivityStopped" })
            {
                Assert.That(Regex.IsMatch(guard, "public void " + step + @"\(Activity activity[^)]*\) \{\s*(if \()?guard\(activity\)"), Is.True, step);
            }

            StringAssert.Contains("app.registerActivityLifecycleCallbacks(CALLBACKS);", guard);
        }

        [Test]
        public void TheGuard_TouchesOnlyUnitysActivity_NotTheAdScreen()
        {
            string guard = Guard;
            StringAssert.Contains("\"com.unity3d.player.UnityPlayerGameActivity\"", guard);
            StringAssert.Contains("\"com.unity3d.player.UnityPlayerActivity\"", guard);
            StringAssert.Contains("if (activity == null || !isUnity(activity)) {", guard);
            Assert.That(guard, Does.Not.Contain("\"com.google.android.gms.ads"), "la pantalla de un anuncio de Google no es nuestra");
        }

        [Test]
        public void TheProbe_IsOnlyInDebuggableBuilds_WithTheWindowFlags()
        {
            string guard = Guard;
            StringAssert.Contains("public static final String PROBE_TAG = \"AF_LOCK\";", guard);
            StringAssert.Contains("(info.flags & ApplicationInfo.FLAG_DEBUGGABLE) == 0", guard);
            foreach (string field in new[] { "window_flags", "show_when_locked", "turn_screen_on", "dismiss_keyguard", "activity_cleared", "found", "cleared", "api" })
            {
                StringAssert.Contains("\\\"" + field + "\\\":", guard, field);
            }

            Assert.That(Regex.Matches(guard, @"Log\.\w+\(").Count, Is.EqualTo(1), "un solo registro: la sonda");
        }
    }
}
