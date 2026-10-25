#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.EditMode
{
    /// <summary>
    /// Guarda de T6, carril A (tests que no dependen de la hora a la que se ejecutan): ningún test lee el reloj del sistema
    /// (<c>DateTime</c>.<c>UtcNow</c>, <c>Now</c> y <c>Today</c>, sus equivalentes de <c>DateTimeOffset</c> ni
    /// <c>SystemClock</c>.<c>Instance</c>; esta descripción los parte para no denunciarse a sí misma), salvo los de la lista blanca, en los que el reloj real es justo lo que se prueba. Los demás
    /// usan un reloj fijo: <c>ManualClock</c>, el <c>TestClock</c> de PlayMode (el que pone <c>PlayModeHelpers</c> si el test no puso
    /// otro), <c>LaunchOptions.ClockOverride</c>, <c>GameBootstrap.LeagueClockOverride</c>…, o fechas relativas a ese reloj. Para
    /// medir esperas reales, <c>Stopwatch</c> o <c>Time.realtimeSinceStartup</c>, que no dependen de la fecha.
    /// Mira <c>Assets/_Project/Tests/**</c> y las carpetas <c>Tests</c> de <c>Assets/_Project/Scripts/**</c>.
    /// </summary>
    public sealed class TestClockGuardTests
    {
        // Se componen por partes para que este archivo no se denuncie a sí mismo.
        private static readonly string[] ClockReads =
        {
            "DateTime" + ".UtcNow", "DateTime" + ".Now", "DateTime" + ".Today",
            "DateTimeOffset" + ".UtcNow", "DateTimeOffset" + ".Now",
            "SystemClock" + ".Instance",
        };

        // Lista blanca: archivo (ruta relativa a Assets/_Project, con '/') → lectura permitida y por qué.
        private static readonly Dictionary<string, string[]> Allowed = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            // Prueba el reloj real de AF.Platform: que SystemClock da UTC y un desfase local razonable (B1-1).
            { "Scripts/Save/Tests/ClockTests.cs", new[] { "SystemClock" + ".Instance" } },

            // El plazo de espera de un hilo (Monitor.Wait hasta que lleguen los Post): mide tiempo real transcurrido, no una fecha
            // del juego. (AF.Backend es del sdk-integrator: no se cambia aquí.)
            { "Scripts/Backend/Tests/HttpBackendClientTests.cs", new[] { "DateTime" + ".UtcNow" } },

            // Un valor cualquiera para un constructor que debe lanzar (un «daily» sin fecha del Reto): la fecha no se compara con nada.
            { "Scripts/Backend/Tests/LeagueContractTests.cs", new[] { "DateTime" + ".UtcNow" } },
        };

        private static string ProjectDir => Path.Combine(Application.dataPath, "_Project");

        [Test]
        public void NoTest_ReadsTheSystemClock_OutsideTheAllowList()
        {
            var offenders = new List<string>();
            var allowedSeen = new HashSet<string>(StringComparer.Ordinal);
            int scanned = 0;
            foreach (string file in TestFiles())
            {
                scanned++;
                string relative = Relative(file);
                Allowed.TryGetValue(relative, out string[]? allowed);
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (string read in ClockReads)
                    {
                        if (lines[i].IndexOf(read, StringComparison.Ordinal) < 0)
                        {
                            continue;
                        }

                        if (allowed != null && Array.IndexOf(allowed, read) >= 0)
                        {
                            allowedSeen.Add(relative + ": " + read);
                            continue;
                        }

                        offenders.Add(relative + ":" + (i + 1) + ": " + read);
                    }
                }
            }

            Assert.That(scanned, Is.GreaterThan(300), "condición del test: encuentra los archivos de tests");
            Assert.That(offenders, Is.Empty, "tests que leen el reloj del sistema (usa un reloj fijo o una fecha relativa a él):\n" + string.Join("\n", offenders));

            // La lista blanca no guarda entradas que ya no hacen falta.
            foreach (KeyValuePair<string, string[]> entry in Allowed)
            {
                foreach (string read in entry.Value)
                {
                    Assert.That(allowedSeen.Contains(entry.Key + ": " + read), Is.True, "entrada de la lista blanca sin uso: " + entry.Key + ": " + read);
                }
            }
        }

        [Test]
        public void TheGuard_SeesTheTestFolders()
        {
            var relatives = new List<string>();
            foreach (string file in TestFiles())
            {
                relatives.Add(Relative(file));
            }

            Assert.That(relatives, Has.Member("Tests/EditMode/TestClockGuardTests.cs"));
            Assert.That(relatives, Has.Member("Tests/PlayMode/PlayModeHelpers.cs"));
            Assert.That(relatives, Has.Member("Scripts/Save/Tests/ClockTests.cs"));
            Assert.That(relatives.Exists(r => r.StartsWith("Scripts/", StringComparison.Ordinal) && !r.Contains("/Tests/")), Is.False,
                "solo carpetas de tests");
        }

        private static IEnumerable<string> TestFiles()
        {
            foreach (string file in Directory.GetFiles(Path.Combine(ProjectDir, "Tests"), "*.cs", SearchOption.AllDirectories))
            {
                yield return file;
            }

            foreach (string dir in Directory.GetDirectories(Path.Combine(ProjectDir, "Scripts"), "Tests", SearchOption.AllDirectories))
            {
                foreach (string file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
                {
                    yield return file;
                }
            }
        }

        private static string Relative(string file)
        {
            string full = Path.GetFullPath(file);
            string root = Path.GetFullPath(ProjectDir) + Path.DirectorySeparatorChar;
            return (full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full.Substring(root.Length) : full).Replace('\\', '/');
        }
    }
}
