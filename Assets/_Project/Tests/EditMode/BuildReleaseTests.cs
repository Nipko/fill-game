#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Xml;
using AF.EditorTools;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Build de release sin firma (bloque B10 de la tanda 5; 08 §12.1 y §13.1 #1; CLAUDE.md, regla dura 6): argumentos,
    /// identidad (el ID provisional nunca llega a un release), rutas de salida, los Player Settings fijados (Managed Stripping
    /// High, 08 §5.4; D-033, Q-T6) y el <c>link.xml</c>. Sin builds: solo funciones puras y los ajustes del proyecto.
    /// </summary>
    public sealed class BuildReleaseTests
    {
        private static BuildRelease.Options Parse(string? env, params string[] args)
        {
            var all = new List<string> { "Unity.exe", "-batchmode" };
            all.AddRange(args);
            string? error = BuildRelease.ParseArgs(all.ToArray(), env, out BuildRelease.Options? options);
            Assert.That(error, Is.Null);
            Assert.That(options, Is.Not.Null);
            return options!;
        }

        [Test]
        public void ParsesTheReleaseArguments()
        {
            BuildRelease.Options o = Parse(null, "-afApplicationId", "com.estudio.juego", "-afApk",
                "-customBuildPath", "build/Android/Android.aab", "-androidVersionCode", "42");
            Assert.That(o.ApplicationId, Is.EqualTo("com.estudio.juego"));
            Assert.That(o.Apk, Is.True);
            Assert.That(o.Rehearsal, Is.False);
            Assert.That(o.CustomBuildPath, Is.EqualTo("build/Android/Android.aab"));
            Assert.That(o.VersionCode, Is.EqualTo(42));
        }

        [Test]
        public void TheEnvironmentIdIsUsedOnlyWithoutTheArgument()
        {
            Assert.That(Parse("com.estudio.env").ApplicationId, Is.EqualTo("com.estudio.env"));
            Assert.That(Parse("com.estudio.env", "-afApplicationId", "com.estudio.arg").ApplicationId, Is.EqualTo("com.estudio.arg"));
        }

        [Test]
        public void RejectsBadArguments()
        {
            Assert.That(BuildRelease.ParseArgs(new[] { "-afApplicationId" }, null, out _), Is.Not.Null);
            Assert.That(BuildRelease.ParseArgs(new[] { "-afApplicationId", "-afApk" }, null, out _), Is.Not.Null);
            Assert.That(BuildRelease.ParseArgs(new[] { "-androidVersionCode", "0" }, null, out _), Is.Not.Null);
            Assert.That(BuildRelease.ParseArgs(new[] { "-androidVersionCode", "x1" }, null, out _), Is.Not.Null);
        }

        [Test]
        public void TheProvisionalIdNeverReachesARelease()
        {
            Assert.That(BuildRelease.CheckIdentity(new BuildRelease.Options()), Is.Not.Null, "sin ID ni ensayo");
            Assert.That(BuildRelease.CheckIdentity(new BuildRelease.Options { ApplicationId = ProjectSetup.ApplicationId }), Is.Not.Null);
            Assert.That(BuildRelease.CheckIdentity(new BuildRelease.Options { ApplicationId = "COM.AMAZONFILL.DEV" }), Is.Not.Null);
            Assert.That(BuildRelease.CheckIdentity(new BuildRelease.Options { ApplicationId = "com.amazonfill.release" }), Is.Not.Null,
                "el ID público no lleva 'amazon' (08 §13.1 #1)");
            Assert.That(BuildRelease.CheckIdentity(new BuildRelease.Options { ApplicationId = "juego" }), Is.Not.Null);
            Assert.That(BuildRelease.CheckIdentity(new BuildRelease.Options { ApplicationId = "com.1estudio.juego" }), Is.Not.Null);
            Assert.That(BuildRelease.CheckIdentity(new BuildRelease.Options { ApplicationId = "com.estudio.juego" }), Is.Null);
        }

        [Test]
        public void TheRehearsalAlwaysUsesTheProvisionalId()
        {
            Assert.That(BuildRelease.CheckIdentity(new BuildRelease.Options { Rehearsal = true }), Is.Null);
            Assert.That(BuildRelease.CheckIdentity(new BuildRelease.Options { Rehearsal = true, ApplicationId = "com.estudio.juego" }),
                Is.Not.Null);
        }

        [Test]
        public void OutputPaths()
        {
            Assert.That(BuildRelease.OutputPath(new BuildRelease.Options { Rehearsal = true }, false), Is.EqualTo("Builds/Android/rehearsal.aab"));
            Assert.That(BuildRelease.OutputPath(new BuildRelease.Options { Rehearsal = true }, true), Is.EqualTo("Builds/Android/rehearsal.apk"));
            Assert.That(BuildRelease.OutputPath(new BuildRelease.Options(), false), Is.EqualTo("Builds/Android/release.aab"));
            var ci = new BuildRelease.Options { CustomBuildPath = "build\\Android\\Android.aab" };
            Assert.That(BuildRelease.OutputPath(ci, false), Is.EqualTo("build/Android/Android.aab"));
            Assert.That(BuildRelease.OutputPath(ci, true), Is.EqualTo("build/Android/Android.apk"));
            Assert.That(BuildRelease.OutputPath(new BuildRelease.Options { CustomBuildPath = "build/Android/" }, false),
                Is.EqualTo("build/Android/release.aab"));
        }

        [Test]
        public void ThePlayerSettingsAreTheFixedOnes_WithManagedStrippingHigh()
        {
            Assert.That(ProjectSetup.StrippingLevel, Is.EqualTo(UnityEditor.ManagedStrippingLevel.High));
            Assert.That(ProjectSetup.Validate(), Is.Empty, "ejecuta AF.EditorTools.ProjectSetup.ApplyAndroidDefaults");
        }

        [Test]
        public void TheLinkXmlPreservesTheInputSystem()
        {
            const string path = "Assets/Plugins/link.xml";
            Assert.That(File.Exists(path), Is.True, path);
            var doc = new XmlDocument();
            doc.Load(path);
            XmlNode? node = doc.SelectSingleNode("/linker/assembly[@fullname='Unity.InputSystem']");
            Assert.That(node, Is.Not.Null);
            Assert.That(node!.Attributes?["preserve"]?.Value, Is.EqualTo("all"));
        }
    }
}
