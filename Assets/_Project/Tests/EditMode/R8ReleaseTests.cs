#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AF.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// R8 (Minify) en el build de release (D-059, añadido del 2026-11-06; <c>docs/dev/release.md</c>, «R8»): solo en la variante
    /// <c>release</c> de Gradle (release, ensayo y humo), nunca en la de desarrollo; cada plugin propio con código lleva su
    /// <c>proguard-rules.pro</c> como <c>consumerProguardFiles</c>; las reglas de Play Games viven en <c>AfKeepRules.androidlib</c>;
    /// y cada clase Java pública de los plugins está en <c>ci/r8-jni-keep.txt</c>, la lista que la compuerta
    /// <c>ci/check-r8-mapping.ps1</c> busca con su nombre en el mapping del AAB.
    /// </summary>
    public sealed class R8ReleaseTests
    {
        private static string PluginsDir => Path.Combine(Application.dataPath, "_Project", "Plugins", "Android");

        private static string KeepList => Path.Combine(Application.dataPath, "..", "ci", "r8-jni-keep.txt");

        [Test]
        public void Minify_OnlyInTheReleaseVariant_AppliedByProjectSetup()
        {
            Assert.That(ProjectSetup.MinifyRelease, Is.True);
            Assert.That(ProjectSetup.MinifyDebug, Is.False, "dev.apk sin R8: se depura con los nombres de verdad");
            Assert.That(PlayerSettings.Android.minifyRelease, Is.True, "ejecuta AF.EditorTools.ProjectSetup.ApplyAndroidDefaults");
            Assert.That(PlayerSettings.Android.minifyDebug, Is.False, "ejecuta AF.EditorTools.ProjectSetup.ApplyAndroidDefaults");
            Assert.That(string.Join("; ", ProjectSetup.Validate()), Does.Not.Contain("minify"));
        }

        [Test]
        public void EveryPluginWithCode_ShipsItsConsumerKeepRules()
        {
            string[] libs = Directory.GetDirectories(PluginsDir, "*.androidlib");
            Assert.That(libs, Is.Not.Empty);
            int withCode = 0;
            foreach (string lib in libs)
            {
                string name = Path.GetFileName(lib);
                if (Directory.GetFiles(lib, "*.java", SearchOption.AllDirectories).Length == 0)
                {
                    continue;
                }

                withCode++;
                string gradle = File.ReadAllText(Path.Combine(lib, "build.gradle"));
                Assert.That(gradle, Does.Contain("consumerProguardFiles 'proguard-rules.pro'"), name);
                string rules = Path.Combine(lib, "proguard-rules.pro");
                Assert.That(File.Exists(rules), Is.True, name + ": sin reglas keep, R8 renombra lo que C# llama por JNI");
                Match ns = Regex.Match(gradle, @"namespace '([\w\.]+)'");
                Assert.That(ns.Success, Is.True, name);
                Assert.That(File.ReadAllText(rules), Does.Contain("-keep class " + ns.Groups[1].Value + ".** { *; }"), name);
            }

            Assert.That(withCode, Is.GreaterThanOrEqualTo(5), "AfBilling, AfSplashHold, AfAgeSignals, AfLockGuard y AfAudioFocus");
        }

        [Test]
        public void KeepRulesLibrary_CarriesPlayGamesRules_WithoutCodeOrNearby()
        {
            string lib = Path.Combine(PluginsDir, "AfKeepRules.androidlib");
            Assert.That(Directory.Exists(lib), Is.True);
            Assert.That(Directory.GetFiles(lib, "*.java", SearchOption.AllDirectories), Is.Empty, "solo reglas");
            Assert.That(Directory.GetFiles(lib, "*.kt", SearchOption.AllDirectories), Is.Empty, "solo reglas");
            string gradle = File.ReadAllText(Path.Combine(lib, "build.gradle"));
            Assert.That(gradle, Does.Contain("namespace 'com.amazonfill.keeprules'"));
            Assert.That(gradle, Does.Contain("consumerProguardFiles 'proguard-rules.pro'"));
            Assert.That(gradle, Does.Not.Contain("dependencies"), "regla 4: nada de dependencias nuevas");
            string rules = File.ReadAllText(Path.Combine(lib, "proguard-rules.pro"));
            Assert.That(rules, Does.Contain("-keep class com.google.games.bridge.** { *; }"));
            Assert.That(rules, Does.Contain("-keep class com.google.android.gms.games.* { *; }"));
            Assert.That(rules, Does.Contain("-keep class com.google.android.gms.games.snapshot.** { *; }"));
            Assert.That(rules, Does.Not.Contain("-keep class com.google.android.gms.nearby"), "Nearby no entra (añadido de D-042)");
        }

        [Test]
        public void EveryPublicJavaTypeOfOurPlugins_IsInTheJniKeepList()
        {
            Assert.That(File.Exists(KeepList), Is.True);
            var listed = new HashSet<string>(StringComparer.Ordinal);
            foreach (string raw in File.ReadAllLines(KeepList))
            {
                string line = raw;
                int hash = line.IndexOf('#');
                if (hash >= 0)
                {
                    line = line.Substring(0, hash);
                }

                line = line.Trim().TrimStart('?').Trim();
                if (line.Length > 0)
                {
                    listed.Add(line);
                }
            }

            int checkedTypes = 0;
            foreach (string file in Directory.GetFiles(PluginsDir, "*.java", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                string package = Regex.Match(text, @"^package\s+([\w\.]+);", RegexOptions.Multiline).Groups[1].Value;
                string outer = Path.GetFileNameWithoutExtension(file);
                Assert.That(listed, Does.Contain(package + "." + outer), Path.GetFileName(file));
                checkedTypes++;
                foreach (Match m in Regex.Matches(text, @"^\s+public\s+(?:static\s+)?interface\s+(\w+)", RegexOptions.Multiline))
                {
                    Assert.That(listed, Does.Contain(package + "." + outer + "$" + m.Groups[1].Value), Path.GetFileName(file));
                    checkedTypes++;
                }
            }

            Assert.That(checkedTypes, Is.GreaterThanOrEqualTo(11));
        }
    }
}
