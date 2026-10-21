#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace AF.Backend.Tests
{
    /// <summary>Rutas del repositorio para los tests: lee el contrato de <c>infra/contracts/v1</c> tal cual, nunca copias.</summary>
    internal static class BackendTestFiles
    {
        public static string RepoRoot => FindRoot();

        public static string ContractDir => Path.Combine(RepoRoot, "infra", "contracts", "v1");

        public static string ExamplesDir => Path.Combine(ContractDir, "examples");

        public static string BackendScriptsDir => Path.Combine(RepoRoot, "Assets", "_Project", "Scripts", "Backend");

        public static string ReadContract(string fileName) => File.ReadAllText(Path.Combine(ContractDir, fileName));

        public static string ReadExample(string fileName) => File.ReadAllText(Path.Combine(ExamplesDir, fileName));

        private static string FindRoot([CallerFilePath] string thisFile = "")
        {
            string? fromCwd = WalkUp(Environment.CurrentDirectory);
            if (fromCwd != null)
            {
                return fromCwd;
            }

            string? fromSource = thisFile.Length == 0 ? null : WalkUp(Path.GetDirectoryName(Path.GetFullPath(thisFile)));
            Assert.That(fromSource, Is.Not.Null, "no encuentro la raíz del repositorio (infra/contracts/v1/index.json)");
            return fromSource!;
        }

        private static string? WalkUp(string? dir)
        {
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "infra", "contracts", "v1", "index.json")))
                {
                    return dir;
                }

                dir = Path.GetDirectoryName(dir);
            }

            return null;
        }
    }
}
