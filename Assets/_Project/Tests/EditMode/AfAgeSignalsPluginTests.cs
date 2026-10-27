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
    /// Guardianes del plugin propio de Play Age Signals (T-L3; D-057, añadido del 2026-10-27), como los de Play Billing: solo Java, solo
    /// habla con Play (sin red propia, logs ni identificadores), la biblioteca en su versión aprobada (0.0.4) y en los dos sitios, sin
    /// permisos propios, solo lee la franja (nunca <c>installId</c>) y el contrato de nombres con <see cref="AndroidAgeSignals"/>.
    /// </summary>
    public sealed class AfAgeSignalsPluginTests
    {
        private const string Approved = "com.google.android.play:age-signals:0.0.4";

        private static string PluginDir => Path.Combine(Application.dataPath, "_Project", "Plugins", "Android", "AfAgeSignals.androidlib");

        private static string JavaFile => Path.Combine(PluginDir, "src", "main", "java", "com", "amazonfill", "agesignals", "AfAgeSignals.java");

        [Test]
        public void Plugin_IsJavaOnly_ReadsOnlyTheAgeRange_AndTalksOnlyToPlay()
        {
            Assert.That(Directory.GetFiles(PluginDir, "*.kt", SearchOption.AllDirectories), Is.Empty, "sin Kotlin");
            Assert.That(Directory.GetFiles(PluginDir, "*.java", SearchOption.AllDirectories), Has.Length.EqualTo(1));
            string text = File.ReadAllText(JavaFile);
            string[] banned =
            {
                "java.net", "javax.net", "okhttp", "HttpURLConnection", "Socket", "http:", "https:", "WebView", "android.util.Log", "Log.",
                "System.out", "printStackTrace", "SharedPreferences", "FileOutputStream", "getAdvertisingId", "Settings.Secure",
                "installId()", "requestAgeSignalsAccess",
            };
            foreach (string word in banned)
            {
                Assert.That(text.Contains(word), Is.False, word);
            }

            foreach (Match m in Regex.Matches(text, @"^import\s+([\w\.]+);", RegexOptions.Multiline))
            {
                string import = m.Groups[1].Value;
                bool allowed = import.StartsWith("com.google.android.play.agesignals.", StringComparison.Ordinal) || import == "android.content.Context";
                Assert.That(allowed, Is.True, "importa " + import);
            }
        }

        [Test]
        public void TheLibrary_IsTheApprovedVersion_InBothPlaces()
        {
            string gradle = File.ReadAllText(Path.Combine(PluginDir, "build.gradle"));
            string edm = File.ReadAllText(Path.Combine(Application.dataPath, "_Project", "Plugins", "Editor", "AfAgeSignalsDependencies.xml"));

            Assert.That(gradle, Does.Contain("compileOnly '" + Approved + "'"));
            Assert.That(gradle, Does.Contain("namespace 'com.amazonfill.agesignals'"), "AGP 9: namespace en cada .androidlib");
            Assert.That(edm, Does.Contain("spec=\"" + Approved + "\""));
            Assert.That(Regex.Matches(gradle + edm, @"age-signals:[\d\.]+").Count, Is.EqualTo(2), "ninguna otra versión");
        }

        [Test]
        public void TheManifest_DeclaresNoPermissions()
        {
            string manifest = File.ReadAllText(Path.Combine(PluginDir, "src", "main", "AndroidManifest.xml"));
            Assert.That(manifest, Does.Not.Contain("uses-permission"));
        }

        [Test]
        public void TheNames_MatchTheCSharpBridge()
        {
            string text = File.ReadAllText(JavaFile);
            Assert.That(AndroidAgeSignals.BridgeClass, Is.EqualTo("com.amazonfill.agesignals.AfAgeSignals"));
            Assert.That(text, Does.Contain("package com.amazonfill.agesignals;"));
            Assert.That(text, Does.Contain("public final class AfAgeSignals"));
            Assert.That(text, Does.Contain("public interface Listener"), "AndroidAgeSignals usa BridgeClass + \"$Listener\"");
            Assert.That(text, Does.Contain("void onResult(String wire);"));
            Assert.That(text, Does.Contain("public static void request(Context context, final Listener listener)"));
            foreach (string wire in new[] { "\"none\"", "\"range:\"", "\"error\"", "\"error:\"" })
            {
                Assert.That(text, Does.Contain(wire), "el formato de AF.Platform.AgeSignal.Parse");
            }
        }
    }
}
