#nullable enable
using System;
using System.IO;
using System.Text.RegularExpressions;
using AF.UI;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// La pantalla de arranque del sistema hasta la intro (añadido de D-051 del 2026-10-28): el plugin propio
    /// <c>AfSplashHold.androidlib</c> es solo Java con API de la plataforma (sin androidx, dependencias, permisos, red, disco ni
    /// registros), su proveedor no se exporta, tiene un tope de 3 s y el contrato de nombres con <see cref="AndroidOsSplash"/> cuadra;
    /// y cuándo la suelta el juego (<see cref="AndroidOsSplash.SignalFor"/>).
    /// </summary>
    public sealed class AfSplashHoldPluginTests
    {
        private static string PluginDir => Path.Combine(Application.dataPath, "_Project", "Plugins", "Android", "AfSplashHold.androidlib");

        private static string JavaDir => Path.Combine(PluginDir, "src", "main", "java", "com", "amazonfill", "splash");

        private static string Hold => File.ReadAllText(Path.Combine(JavaDir, "AfSplashHold.java"));

        [Test]
        public void Plugin_IsJavaOnly_WithPlatformApiOnly_AndNoDataOrNetwork()
        {
            Assert.That(Directory.GetFiles(PluginDir, "*.kt", SearchOption.AllDirectories), Is.Empty, "sin Kotlin");
            string[] files = Directory.GetFiles(PluginDir, "*.java", SearchOption.AllDirectories);
            Assert.That(files, Has.Length.EqualTo(2), "AfSplashHold y su proveedor");
            string[] banned =
            {
                "java.net", "javax.net", "okhttp", "HttpURLConnection", "Socket", "http:", "https:", "WebView", "android.util.Log", "Log.",
                "System.out", "printStackTrace", "SharedPreferences", "FileOutputStream", "getAdvertisingId", "Settings.Secure", "androidx",
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
                    bool allowed = import.StartsWith("android.", StringComparison.Ordinal) || import == "java.lang.reflect.Method";
                    Assert.That(allowed, Is.True, Path.GetFileName(file) + " importa " + import);
                }
            }

            string gradle = File.ReadAllText(Path.Combine(PluginDir, "build.gradle"));
            Assert.That(gradle, Does.Contain("namespace 'com.amazonfill.splash'"), "AGP 9: namespace en cada .androidlib");
            Assert.That(gradle, Does.Contain("minSdk 26"));
            Assert.That(gradle, Does.Not.Contain("dependencies"), "regla 4: nada de dependencias nuevas");
        }

        [Test]
        public void Manifest_HasOnlyTheUnexportedProvider_AndNoPermissions()
        {
            string path = Path.Combine(PluginDir, "src", "main", "AndroidManifest.xml");
            Assert.DoesNotThrow(() => new System.Xml.XmlDocument().Load(path), "XML válido (un «--» en un comentario rompe el build)");
            string manifest = File.ReadAllText(path);
            Assert.That(manifest, Does.Not.Contain("uses-permission"));
            Assert.That(manifest, Does.Not.Contain("<activity"), "no sustituye a la actividad de Unity");
            StringAssert.Contains("android:name=\"com.amazonfill.splash.AfSplashHoldProvider\"", manifest);
            StringAssert.Contains("android:authorities=\"${applicationId}.afsplashhold\"", manifest);
            StringAssert.Contains("android:exported=\"false\"", manifest);
        }

        [Test]
        public void TheBridge_MatchesTheCSharpSide_AndHasTheCap()
        {
            string hold = Hold;
            Assert.That(AndroidOsSplash.BridgeClass, Is.EqualTo("com.amazonfill.splash.AfSplashHold"));
            StringAssert.Contains("package com.amazonfill.splash;", hold);
            StringAssert.Contains("public static final long CAP_MS = " + AndroidOsSplash.CapMs + "L;", hold);
            Assert.That(AndroidOsSplash.CapMs, Is.EqualTo(3000), "tope de seguridad: nunca se queda colgada");
            foreach (string method in new[] { "release(String by)", "String result()", "long heldMs()", "long procReleaseMs()", "long focusAfterMs()" })
            {
                StringAssert.Contains("public static " + (method.StartsWith("release", StringComparison.Ordinal) ? "void " : string.Empty) + method, hold, method);
            }

            foreach (string result in new[] { "none", "pending", "signal", "cap", "gone" })
            {
                StringAssert.Contains("= \"" + result + "\";", hold, result);
            }

            // Mientras retiene, Unity corre sin foco (Android no da el foco a una ventana sin pintar) y luego vuelve a lo de siempre.
            StringAssert.Contains("MAIN.postDelayed(CAP, CAP_MS);", hold);
            StringAssert.Contains("setRunWithoutFocus(activity, true)", hold);
            StringAssert.Contains("setRunWithoutFocus(activity, false)", hold);
            StringAssert.Contains("getUnityPlayerConnection", hold);
        }

        [Test]
        public void TheGame_ReleasesItWhenTheIntroIsOnScreen_OrWithoutIntro_OrWithoutSplash()
        {
            Assert.That(AndroidOsSplash.SignalFor(true, true, false, 50), Is.Null, "con la intro: espera a su primer fotograma en la pantalla");
            Assert.That(AndroidOsSplash.SignalFor(true, true, true, 3), Is.EqualTo(AndroidOsSplash.ByIntro), "también quieta (Reducir movimiento) o con caché");
            Assert.That(AndroidOsSplash.SignalFor(true, false, false, 1), Is.Null, "sin intro: el primer fotograma aún no se pintó");
            Assert.That(AndroidOsSplash.SignalFor(true, false, false, 2), Is.EqualTo(AndroidOsSplash.BySplash));
            Assert.That(AndroidOsSplash.SignalFor(false, false, false, 1), Is.EqualTo(AndroidOsSplash.ByNoSplash), "una recarga de la escena no espera");
        }

        [Test]
        public void InTheEditor_ReleaseDoesNothing_AndReadSaysNoHold()
        {
            Assert.DoesNotThrow(() => AndroidOsSplash.Release(AndroidOsSplash.ByIntro));
            OsSplashInfo info = AndroidOsSplash.Read();
            Assert.That(info.Result, Is.EqualTo("none"));
            Assert.That(info.HeldMs, Is.EqualTo(-1L));
            Assert.That(info.ProcReleaseMs, Is.EqualTo(-1L));
            Assert.That(info.FocusAfterMs, Is.EqualTo(-1L));
        }
    }
}
