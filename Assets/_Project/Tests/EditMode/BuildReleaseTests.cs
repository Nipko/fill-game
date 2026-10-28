#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Xml;
using AF.EditorTools;
using NUnit.Framework;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Build de release (bloque B10 de la tanda 5 y paso s32; 08 §12.1 y §13.1 #1 y #3; CLAUDE.md, regla dura 6): argumentos, firma,
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
        public void ResultFile_IsOptional_AndKeepsTheVerdictLine()
        {
            Assert.That(BuildRelease.ResultFileFrom(new[] { "Unity.exe", "-batchmode" }), Is.Null);
            Assert.That(BuildRelease.ResultFileFrom(new[] { "Unity.exe", "-afResultFile" }), Is.Null, "sin valor");
            Assert.That(BuildRelease.ResultFileFrom(new[] { "-afResultFile", "-afRehearsal" }), Is.Null, "el siguiente es otra opción");
            Assert.That(BuildRelease.ResultFileFrom(new[] { "-afRehearsal", "-afResultFile", "build/af-build-result.txt" }),
                Is.EqualTo("build/af-build-result.txt"));
            Assert.That(Parse(null, "-afRehearsal", "-afResultFile", "build/r.txt").Rehearsal, Is.True, "el parser lo ignora");

            string dir = Path.Combine(Path.GetTempPath(), "af-result-" + System.Guid.NewGuid().ToString("N"));
            string path = Path.Combine(dir, "sub", "af-build-result.txt");
            try
            {
                BuildRelease.WriteResultFile(path, "[AF.BuildRelease] OK.");
                Assert.That(File.ReadAllText(path), Is.EqualTo("[AF.BuildRelease] OK.\n"));
                BuildRelease.WriteResultFile(null, "x"); // sin ruta: nada
            }
            finally
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
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
        public void FirebaseConfig_RegeneratesInReleaseAndFailsClosedInTheSignedOne()
        {
            System.Func<string, bool> withJson = p => p == BuildRelease.GoogleServicesJsonPath;
            System.Func<string, bool> withoutJson = _ => false;
            BuildRelease.FirebaseConfigPlan plan;

            Assert.That(BuildRelease.CheckFirebaseConfig(new BuildRelease.Options { Rehearsal = true }, withJson, out plan), Is.Null);
            Assert.That(plan, Is.EqualTo(BuildRelease.FirebaseConfigPlan.Skip), "el ensayo usa la configuracion de prueba");

            Assert.That(BuildRelease.CheckFirebaseConfig(new BuildRelease.Options { RequireSigning = true }, withJson, out plan), Is.Null);
            Assert.That(plan, Is.EqualTo(BuildRelease.FirebaseConfigPlan.Regenerate));

            Assert.That(BuildRelease.CheckFirebaseConfig(new BuildRelease.Options(), withoutJson, out plan), Is.Null);
            Assert.That(plan, Is.EqualTo(BuildRelease.FirebaseConfigPlan.Warn), "release local sin JSON: solo aviso");

            Assert.That(BuildRelease.CheckFirebaseConfig(new BuildRelease.Options { RequireSigning = true }, withoutJson, out plan),
                Does.Contain(BuildRelease.GoogleServicesJsonPath), "el release del CI no sale sin Firebase");
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

        private static Dictionary<string, string?> SigningEnv(string? path = "/root/af-upload.keystore")
        {
            return new Dictionary<string, string?>
            {
                [BuildRelease.KeystoreNameEnv] = path,
                [BuildRelease.KeystorePassEnv] = "pass-de-prueba",
                [BuildRelease.KeyaliasNameEnv] = "subida",
                [BuildRelease.KeyaliasPassEnv] = "-empieza-por-guion",
            };
        }

        private static BuildRelease.Options ParseEnv(Dictionary<string, string?> env, params string[] args)
        {
            string? error = BuildRelease.ParseArgsWithEnv(args, name => env.TryGetValue(name, out string? v) ? v : null,
                out BuildRelease.Options? options);
            Assert.That(error, Is.Null);
            return options!;
        }

        [Test]
        public void ReadsTheSigningFromTheEnvironment()
        {
            BuildRelease.Options o = ParseEnv(SigningEnv(), "-afApplicationId", "com.estudio.juego", "-afRequireSigning");
            Assert.That(o.RequireSigning, Is.True);
            Assert.That(o.KeystorePath, Is.EqualTo("/root/af-upload.keystore"));
            Assert.That(o.KeystorePass, Is.EqualTo("pass-de-prueba"));
            Assert.That(o.KeyaliasName, Is.EqualTo("subida"));
            Assert.That(o.KeyaliasPass, Is.EqualTo("-empieza-por-guion"));
        }

        [Test]
        public void TheSigningArgumentsOfGameCiAreTheFallback_EvenEmptyOrWithADash()
        {
            // game-ci pasa siempre los cuatro, vacíos si no hay firma (job build-android de ci.yml).
            BuildRelease.Options none = ParseEnv(new Dictionary<string, string?>(),
                "-androidKeystoreName", "", "-androidKeystorePass", "", "-androidKeyaliasName", "", "-androidKeyaliasPass", "",
                "-afApplicationId", "com.estudio.juego");
            Assert.That(none.ApplicationId, Is.EqualTo("com.estudio.juego"));
            Assert.That(BuildRelease.CheckSigning(none, _ => true, out BuildRelease.Signing? s0), Is.Null);
            Assert.That(s0, Is.Null, "sin firma: clave de depuración");

            BuildRelease.Options args = ParseEnv(new Dictionary<string, string?> { [BuildRelease.KeystorePassEnv] = "del-entorno" },
                "-androidKeystoreName", "k.keystore", "-androidKeystorePass", "del-argumento",
                "-androidKeyaliasName", "subida", "-androidKeyaliasPass", "-guion");
            Assert.That(args.KeystorePath, Is.EqualTo("k.keystore"));
            Assert.That(args.KeystorePass, Is.EqualTo("del-entorno"), "el entorno manda sobre el argumento");
            Assert.That(args.KeyaliasPass, Is.EqualTo("-guion"));
        }

        [Test]
        public void TheSigningIsCompleteOrAnError()
        {
            BuildRelease.Options full = ParseEnv(SigningEnv(), "-afApplicationId", "com.estudio.juego", "-afRequireSigning");
            Assert.That(BuildRelease.CheckSigning(full, _ => true, out BuildRelease.Signing? signing), Is.Null);
            Assert.That(signing, Is.Not.Null);
            Assert.That(signing!.KeyaliasName, Is.EqualTo("subida"));

            Assert.That(BuildRelease.CheckSigning(full, _ => false, out _), Is.Not.Null, "keystore inexistente");

            var required = new BuildRelease.Options { ApplicationId = "com.estudio.juego", RequireSigning = true };
            Assert.That(BuildRelease.CheckSigning(required, _ => true, out _), Is.Not.Null, "-afRequireSigning sin firma");

            Dictionary<string, string?> partial = SigningEnv();
            partial[BuildRelease.KeyaliasPassEnv] = "";
            string? error = BuildRelease.CheckSigning(ParseEnv(partial), _ => true, out _);
            Assert.That(error, Is.Not.Null, "firma a medias");
            Assert.That(error, Does.Contain(BuildRelease.KeyaliasPassEnv));
            Assert.That(error, Does.Not.Contain("pass-de-prueba"), "los errores nunca llevan una contraseña");

            Assert.That(BuildRelease.CheckSigning(ParseEnv(SigningEnv(), "-afRehearsal"), _ => true, out _), Is.Not.Null,
                "un ensayo nunca va firmado con la clave de subida");
        }

        [Test]
        public void ParsesTheVersionName()
        {
            Assert.That(Parse(null, "-afVersionName", "1.0.0").VersionName, Is.EqualTo("1.0.0"));
            Assert.That(Parse(null, "-afVersionName", "12.3.45").VersionName, Is.EqualTo("12.3.45"));
            foreach (string bad in new[] { "1.0", "1.0.0.0", "01.0.0", "1.a.0", "1..0", "v1.0.0", "1.0.0-rc1", "12345.0.0" })
            {
                Assert.That(BuildRelease.ParseArgs(new[] { "-afVersionName", bad }, null, out _), Is.Not.Null, bad);
            }
        }

        [Test]
        public void ApplyingTheSigningIsRestoredAndLeavesTheProjectWithoutKeystore()
        {
            BuildRelease.SigningSettings before = BuildRelease.SigningSettings.Capture();
            try
            {
                BuildRelease.ApplySigning(
                    new BuildRelease.Signing("Temp/af-prueba.keystore", "pass-de-prueba", "subida", "pass-de-alias"));
                Assert.That(UnityEditor.PlayerSettings.Android.useCustomKeystore, Is.True);
                Assert.That(UnityEditor.PlayerSettings.Android.keystoreName, Does.EndWith("Temp/af-prueba.keystore"));
                Assert.That(UnityEditor.PlayerSettings.Android.keyaliasName, Is.EqualTo("subida"));
            }
            finally
            {
                before.Restore();
            }

            BuildRelease.SigningSettings after = BuildRelease.SigningSettings.Capture();
            Assert.That(after.UseCustomKeystore, Is.EqualTo(before.UseCustomKeystore));
            Assert.That(after.KeystoreName, Is.EqualTo(before.KeystoreName));
            Assert.That(after.KeystorePass, Is.EqualTo(before.KeystorePass));
            Assert.That(after.KeyaliasName, Is.EqualTo(before.KeyaliasName));
            Assert.That(after.KeyaliasPass, Is.EqualTo(before.KeyaliasPass));
            Assert.That(ProjectSetup.Validate(), Is.Empty, "useCustomKeystore vuelve a false");
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
