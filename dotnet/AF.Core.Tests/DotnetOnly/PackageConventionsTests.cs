#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Guardarraíles del paquete Packages/com.amazonfill.core que solo se pueden comprobar desde dotnet,
    /// porque leen el disco (informe D §4.2 y §6.4). No se comparten con Unity.
    /// </summary>
    [TestFixture]
    public sealed class PackageConventionsTests
    {
        [Test]
        public void EveryPackageSource_StartsWithNullableEnable()
        {
            var offenders = new List<string>();
            foreach (string file in Directory.GetFiles(PackageDir(), "*.cs", SearchOption.AllDirectories))
            {
                string firstLine;
                using (var reader = new StreamReader(file))
                {
                    firstLine = reader.ReadLine() ?? string.Empty;
                }

                if (firstLine != "#nullable enable")
                {
                    offenders.Add(file);
                }
            }

            Assert.That(offenders, Is.Empty);
        }

        [Test]
        public void RuntimeSources_HaveNoUnityConditionals()
        {
            // Si el núcleo se comporta distinto según el compilador, dotnet test deja de representar al cliente.
            // (En los tests se tolera para rutas de fixtures; en Runtime/ nunca.)
            var offenders = new List<string>();
            string runtimeDir = Path.Combine(PackageDir(), "Runtime");
            foreach (string file in Directory.GetFiles(runtimeDir, "*.cs", SearchOption.AllDirectories))
            {
                foreach (string line in File.ReadLines(file))
                {
                    string trimmed = line.TrimStart();
                    bool isConditional = trimmed.StartsWith("#if", System.StringComparison.Ordinal)
                        || trimmed.StartsWith("#elif", System.StringComparison.Ordinal);
                    if (isConditional && trimmed.Contains("UNITY_", System.StringComparison.Ordinal))
                    {
                        offenders.Add(file + ": " + trimmed);
                    }
                }
            }

            Assert.That(offenders, Is.Empty);
        }

        [TestCase("Runtime/AF.Core/AF.Core.asmdef")]
        [TestCase("Runtime/AF.Solver/AF.Solver.asmdef")]
        public void RuntimeAssemblies_HaveNoEngineReferences(string asmdefPath)
        {
            string json = File.ReadAllText(Path.Combine(PackageDir(), asmdefPath));
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                Assert.That(doc.RootElement.GetProperty("noEngineReferences").GetBoolean(), Is.True);
            }
        }

        private static string PackageDir()
        {
            DirectoryInfo? dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "Packages", "com.amazonfill.core");
                if (File.Exists(Path.Combine(candidate, "package.json")))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "No se encontró Packages/com.amazonfill.core subiendo desde " + TestContext.CurrentContext.TestDirectory);
        }
    }
}
