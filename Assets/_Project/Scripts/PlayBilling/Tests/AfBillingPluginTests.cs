#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace AF.IAP.PlayBilling.Tests
{
    /// <summary>
    /// Guardianes del plugin propio de Play Billing (D-040, añadido de Q-IAP-1): solo habla con Play (sin red propia ni logs), solo Java,
    /// la biblioteca de Play Billing en su versión aprobada (9.0.0) y en los dos sitios, sin permisos propios, y el contrato de nombres
    /// entre Java y C# (los métodos del oyente y del plugin que llama el puente).
    /// </summary>
    public sealed class AfBillingPluginTests
    {
        private const string ApprovedBilling = "com.android.billingclient:billing:9.0.0";

        private static string PluginDir => Path.Combine(Application.dataPath, "_Project", "Plugins", "Android", "AfBilling.androidlib");

        private static string JavaDir => Path.Combine(PluginDir, "src", "main", "java", "com", "amazonfill", "billing");

        private static string ScriptsDir => Path.Combine(Application.dataPath, "_Project", "Scripts", "PlayBilling");

        private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(parts));

        [Test]
        public void Plugin_IsJavaOnly_AndTalksOnlyToPlay()
        {
            string[] java = Directory.GetFiles(JavaDir, "*.java");
            Assert.That(java, Has.Length.EqualTo(2));
            Assert.That(Directory.GetFiles(PluginDir, "*.kt", SearchOption.AllDirectories), Is.Empty, "sin Kotlin");

            string[] banned =
            {
                "java.net", "javax.net", "okhttp", "HttpURLConnection", "Socket", "URL(", "http:", "https:", "WebView",
                "android.util.Log", "Log.", "System.out", "printStackTrace", "SharedPreferences", "FileOutputStream",
                "getAdvertisingId", "AdvertisingIdClient", "Settings.Secure",
            };
            foreach (string file in java)
            {
                string text = File.ReadAllText(file);
                foreach (string word in banned)
                {
                    Assert.That(text.Contains(word), Is.False, Path.GetFileName(file) + ": " + word);
                }

                foreach (Match m in Regex.Matches(text, @"^import\s+([\w\.]+);", RegexOptions.Multiline))
                {
                    string import = m.Groups[1].Value;
                    bool allowed = import.StartsWith("com.android.billingclient.api.", StringComparison.Ordinal)
                        || import == "android.app.Activity" || import == "android.content.Context"
                        || import == "android.os.Handler" || import == "android.os.Looper"
                        || import.StartsWith("java.util.", StringComparison.Ordinal);
                    Assert.That(allowed, Is.True, Path.GetFileName(file) + " importa " + import);
                }
            }
        }

        [Test]
        public void BillingLibrary_IsTheApprovedVersion_InEdm4uAndInTheGradleOfThePlugin()
        {
            string gradle = Read(PluginDir, "build.gradle");
            StringAssert.Contains("namespace 'com.amazonfill.billing'", gradle, "AGP 9 exige namespace (.claude/rules/sdk.md)");
            Assert.That(gradle.Contains("kotlin"), Is.False, "sin el plugin de Kotlin en Gradle");
            MatchCollection deps = Regex.Matches(gradle, @"^\s*(implementation|api|compileOnly|runtimeOnly)\s+'([^']+)'", RegexOptions.Multiline);
            Assert.That(deps.Count, Is.EqualTo(1), "una sola dependencia");
            Assert.That(deps[0].Groups[1].Value, Is.EqualTo("compileOnly"), "en el build entra por EDM4U, no dos veces");
            Assert.That(deps[0].Groups[2].Value, Is.EqualTo(ApprovedBilling));

            string xml = Read(Application.dataPath, "_Project", "Plugins", "Editor", "AfBillingDependencies.xml");
            MatchCollection specs = Regex.Matches(xml, "androidPackage spec=\"([^\"]+)\"");
            Assert.That(specs.Count, Is.EqualTo(1));
            Assert.That(specs[0].Groups[1].Value, Is.EqualTo(ApprovedBilling), "D-040: cambiar la versión exige al dueño");
            Assert.That(xml.Contains("repositor"), Is.False, "de Google Maven, sin repositorios propios");
        }

        [Test]
        public void PluginManifest_DeclaresNoPermission()
        {
            string manifest = Read(PluginDir, "src", "main", "AndroidManifest.xml");
            Assert.That(manifest.Contains("uses-permission"), Is.False, "BILLING lo declara la biblioteca (D-040)");
            Assert.That(manifest.Contains("<application"), Is.False);
        }

        [Test]
        public void JavaListener_AndCSharpDispatch_HaveTheSameMethods()
        {
            string listener = Read(JavaDir, "AfBillingListener.java");
            var javaMethods = new SortedSet<string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(listener, @"^\s*void\s+(\w+)\s*\(", RegexOptions.Multiline))
            {
                javaMethods.Add(m.Groups[1].Value);
            }

            string dispatch = Read(ScriptsDir, "BillingCallbacks.cs");
            var csMethods = new SortedSet<string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(dispatch, "case \"(\\w+)\":"))
            {
                csMethods.Add(m.Groups[1].Value);
            }

            Assert.That(javaMethods, Has.Count.EqualTo(8));
            Assert.That(csMethods, Is.EqualTo(javaMethods));
            StringAssert.Contains("\"" + "com.amazonfill.billing.AfBillingListener" + "\"", dispatch);
        }

        [Test]
        public void BridgeCalls_ExistInThePlugin_AndTheBridgeOnlyCompilesOnTheDevice()
        {
            string plugin = Read(JavaDir, "AfBilling.java");
            string bridge = Read(ScriptsDir, "AndroidBillingBridge.cs");
            StringAssert.StartsWith("#nullable enable\n#if UNITY_ANDROID && !UNITY_EDITOR", bridge.Replace("\r\n", "\n"));
            StringAssert.Contains("\"com.amazonfill.billing.AfBilling\"", bridge);

            int calls = 0;
            foreach (Match m in Regex.Matches(bridge, "_plugin\\.Call\\(\"(\\w+)\""))
            {
                calls++;
                StringAssert.Contains("public void " + m.Groups[1].Value + "(", plugin, m.Groups[1].Value);
            }

            Assert.That(calls, Is.EqualTo(7), "connect, queryProducts, queryPurchases, launch, consume, acknowledge, release");
            StringAssert.Contains("enablePendingPurchases(PendingPurchasesParams.newBuilder().enableOneTimeProducts().build())", plugin);
            StringAssert.Contains(".enableAutoServiceReconnection()", plugin);
            StringAssert.Contains("setObfuscatedAccountId(", plugin);
        }

        [Test]
        public void PlayBillingScripts_WriteNoLogs()
        {
            foreach (string file in Directory.GetFiles(ScriptsDir, "*.cs", SearchOption.TopDirectoryOnly))
            {
                string text = File.ReadAllText(file);
                foreach (string word in new[] { "Debug" + ".Log", "Console" + ".Write", "UnityWeb" + "Request", "System" + ".Net" })
                {
                    Assert.That(text.Contains(word), Is.False, Path.GetFileName(file) + ": " + word);
                }
            }
        }
    }
}
