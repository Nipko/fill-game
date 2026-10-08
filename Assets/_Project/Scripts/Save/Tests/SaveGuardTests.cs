#nullable enable
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace AF.Save.Tests
{
    /// <summary>
    /// Guardas estáticas de B1-1:
    /// <list type="bullet">
    /// <item>ningún archivo de <c>AF.Platform</c> ni de <c>AF.Save</c> lee el reloj del sistema salvo
    /// <c>SystemClock.cs</c> (el resto usa <c>IClock</c>);</item>
    /// <item>los dos ensamblados son sin motor y <c>AF.Save</c> no referencia <c>AF.Levels</c> (08 §2.2: Levels
    /// depende de Save, no al revés) ni ningún paquete.</item>
    /// </list>
    /// </summary>
    public sealed class SaveGuardTests
    {
        // Se componen por partes para que este archivo no se denuncie a sí mismo.
        private static readonly string[] ClockReads =
        {
            "DateTime" + ".Now", "DateTime" + ".UtcNow", "DateTime" + ".Today",
            "DateTimeOffset" + ".Now", "DateTimeOffset" + ".UtcNow",
            "Environment" + ".TickCount", "Stop" + "watch",
        };

        private static string ScriptsDir => Path.Combine(Application.dataPath, "_Project", "Scripts");

        [Test]
        public void OnlySystemClock_ReadsTheSystemClock()
        {
            var offenders = new List<string>();
            foreach (string module in new[] { "Platform", "Save" })
            {
                string[] files = Directory.GetFiles(Path.Combine(ScriptsDir, module), "*.cs", SearchOption.AllDirectories);
                Assert.That(files, Is.Not.Empty, module);
                foreach (string file in files)
                {
                    if (Path.GetFileName(file) == "SystemClock.cs")
                    {
                        continue;
                    }

                    string text = File.ReadAllText(file);
                    foreach (string read in ClockReads)
                    {
                        if (text.Contains(read))
                        {
                            offenders.Add(Path.GetFileName(file) + ": " + read);
                        }
                    }
                }
            }

            Assert.That(offenders, Is.Empty);
        }

        [TestCase("Platform", "AF.Platform.asmdef")]
        [TestCase("Save", "AF.Save.asmdef")]
        public void RuntimeAssemblies_HaveNoEngine_AndNoUpwardReferences(string module, string asmdef)
        {
            string text = File.ReadAllText(Path.Combine(ScriptsDir, module, asmdef));

            StringAssert.Contains("\"noEngineReferences\": true", text);
            StringAssert.Contains("\"autoReferenced\": false", text);
            foreach (string forbidden in new[] { "AF.Levels", "AF.GameFlow", "AF.UI", "AF.Presentation", "UnityEngine", "Unity." })
            {
                Assert.That(text.Contains(forbidden), Is.False, asmdef + " no debe referenciar " + forbidden);
            }

            string testsDir = Path.Combine(ScriptsDir, module, "Tests") + Path.DirectorySeparatorChar;
            foreach (string file in Directory.GetFiles(Path.Combine(ScriptsDir, module), "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFullPath(file).StartsWith(Path.GetFullPath(testsDir), System.StringComparison.OrdinalIgnoreCase))
                {
                    continue; // los tests sí usan el motor (Application.dataPath)
                }

                Assert.That(File.ReadAllText(file).Contains("using Unity"), Is.False, Path.GetFileName(file));
            }
        }
    }
}
