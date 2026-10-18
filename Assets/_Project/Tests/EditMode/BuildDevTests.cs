#nullable enable
using System;
using System.IO;
using System.Text.RegularExpressions;
using AF.EditorTools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// El build de desarrollo empieza sin APK anterior (tarea FX10, observación 2 de la prueba de la ola 6 en el teléfono):
    /// <c>dev.apk</c> es una copia del APK de depuración que deja Gradle en el proyecto que exporta Unity, y con el build
    /// incremental Gradle lo actualiza en su sitio y deja huecos (37,6 MB de 105,6 en el portátil). BuildDev borra los dos antes de
    /// construir y lo dice en el log, una línea por archivo. Con carpetas temporales: no toca el proyecto.
    /// </summary>
    public sealed class BuildDevTests
    {
        private string _root = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "af-builddev-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void DeletesThePreviousDevApk_AndTheGradleDebugApks_AndSaysSo()
        {
            string apk = Path.Combine(_root, "Builds/Android/dev.apk").Replace('\\', '/');
            string projects = Path.Combine(_root, "Library/Bee/Android/Prj").Replace('\\', '/');
            string il2cpp = projects + "/IL2CPP/" + BuildDev.GradleDebugApkDir;
            string mono = projects + "/Mono2x/" + BuildDev.GradleDebugApkDir;
            string release = projects + "/IL2CPP/Gradle/launcher/build/outputs/apk/release";
            foreach (string dir in new[] { Path.GetDirectoryName(apk)!, il2cpp, mono, release })
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(apk, new byte[2048]);
            File.WriteAllBytes(il2cpp + "/launcher-debug.apk", new byte[1024]);
            File.WriteAllText(il2cpp + "/output-metadata.json", "{}");
            File.WriteAllBytes(mono + "/launcher-debug.apk", new byte[16]);
            File.WriteAllBytes(release + "/launcher-release.apk", new byte[16]);

            Assert.That(BuildDev.GradleDebugApkDirs(projects), Is.EqualTo(new[] { il2cpp, mono }), "una carpeta por backend, en orden ordinal");

            LogAssert.Expect(LogType.Log, new Regex("^\\[AF\\.BuildDev\\] borrado el " + Regex.Escape(apk) + " anterior \\(0\\.00 MB\\)"));
            LogAssert.Expect(LogType.Log, new Regex("^\\[AF\\.BuildDev\\] borrado el APK anterior de Gradle " + Regex.Escape(il2cpp + "/launcher-debug.apk")));
            LogAssert.Expect(LogType.Log, new Regex("^\\[AF\\.BuildDev\\] borrado el APK anterior de Gradle " + Regex.Escape(mono + "/launcher-debug.apk")));
            Assert.That(BuildDev.DeletePreviousApks(apk, projects), Is.EqualTo(3));
            Assert.That(File.Exists(apk), Is.False, "sin el dev.apk anterior");
            Assert.That(File.Exists(il2cpp + "/launcher-debug.apk"), Is.False, "Gradle empaqueta el APK de depuración entero");
            Assert.That(File.Exists(mono + "/launcher-debug.apk"), Is.False);
            Assert.That(File.Exists(il2cpp + "/output-metadata.json"), Is.True, "solo los APK");
            Assert.That(File.Exists(release + "/launcher-release.apk"), Is.True, "el de release (BuildSmoke) no es de este build");

            // Sin nada que borrar: lo dice y sigue.
            LogAssert.Expect(LogType.Log, "[AF.BuildDev] no hay " + apk + " anterior.");
            Assert.That(BuildDev.DeletePreviousApks(apk, projects), Is.EqualTo(0));
            Assert.That(BuildDev.GradleDebugApkDirs(_root + "/no-existe"), Is.Empty);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
