#nullable enable
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Bot;
using AF.Solver.Search;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Bot casual ×40 de AF.Solver (C1-5; 03 §6) frente al <c>bot_fail</c> que publicó el prototipo
    /// <c>tools/levelgen_proto.py</c> para los 40 niveles de <c>tools/out/levels_proto.json</c> y
    /// <c>tools/out_inverse/levels_proto.json</c>. El PRNG es otro (09 R26) y 40 partidas tienen un error de muestreo de
    /// hasta ±0,08, así que la comparación es estadística [P]: misma etiqueta (02 §10) en el 75 % o más de los niveles y
    /// diferencia media de 0,10 o menos. El bot no se ajusta a ciegas: si no se cumple, se informa. Solo dotnet: lee disco.
    /// </summary>
    [TestFixture]
    public sealed class BotParityTests
    {
        [Test]
        public void PrototypeLevels_SameLabelAndCloseFailRate()
        {
            var bot = new CasualBot();
            var lines = new StringBuilder();
            lines.AppendLine("| pack | id | T | E | bot_fail proto | bot_fail C# | etiqueta proto | etiqueta C# |");
            int levels = 0;
            int sameLabel = 0;
            double totalDiff = 0;
            string[] packs = { "tools/out/levels_proto.json", "tools/out_inverse/levels_proto.json" };
            foreach (string pack in packs)
            {
                using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), pack))))
                {
                    foreach (JsonElement level in doc.RootElement.GetProperty("levels").EnumerateArray())
                    {
                        BoardState start = BuildBoard(level);
                        double proto = level.GetProperty("difficulty").GetProperty("bot_fail").GetDouble();
                        string seed = level.GetProperty("seed").GetString() ?? string.Empty;
                        BotResult mine = bot.FailRate(start, CasualBot.SeedFor(seed), PourMode.Run);
                        string protoLabel = Label(proto);
                        string myLabel = Label(mine.Rate);
                        levels++;
                        sameLabel += protoLabel == myLabel ? 1 : 0;
                        totalDiff += Math.Abs(proto - mine.Rate);
                        lines.Append("| ").Append(pack.Contains("inverse", StringComparison.Ordinal) ? "inverso" : "híbrido")
                            .Append(" | ").Append(level.GetProperty("id").GetString())
                            .Append(" | ").Append(start.Count.ToString(CultureInfo.InvariantCulture))
                            .Append(" | ").Append(Empties(start).ToString(CultureInfo.InvariantCulture))
                            .Append(" | ").Append(proto.ToString("0.000", CultureInfo.InvariantCulture))
                            .Append(" | ").Append(mine.Rate.ToString("0.000", CultureInfo.InvariantCulture))
                            .Append(" | ").Append(protoLabel).Append(" | ").Append(myLabel).AppendLine(" |");
                    }
                }
            }

            double meanDiff = totalDiff / levels;
            TestContext.WriteLine(lines.ToString());
            TestContext.WriteLine("Misma etiqueta: " + sameLabel + "/" + levels + "; diferencia media "
                + meanDiff.ToString("0.000", CultureInfo.InvariantCulture));
            Assert.That(levels, Is.EqualTo(40));
            Assert.That(sameLabel * 100, Is.GreaterThanOrEqualTo(75 * levels));
            Assert.That(meanDiff, Is.LessThanOrEqualTo(0.10));
        }

        [Test]
        public void SameSeed_SameFailRate_InTwoRuns()
        {
            BoardState start = TwelveCuiaBoard();
            ulong seed = CasualBot.SeedFor("amazonfill:v1:m05:v1:n0186");

            BotResult first = new CasualBot().FailRate(start, seed, PourMode.Run);
            BotResult second = new CasualBot().FailRate(start, seed, PourMode.Run);

            Assert.That(second.Failures, Is.EqualTo(first.Failures));
        }

        [Test]
        [Category("Informe")]
        public void Timing_FortyGamesOnATwelveCuiaBoard()
        {
            // 03 §18: en Python el bot ×40 tarda 20-63 ms por nivel. Informativo (depende de la máquina y del modo Debug).
            BoardState start = TwelveCuiaBoard();
            var bot = new CasualBot();
            bot.FailRate(start, 1UL, PourMode.Run);
            var watch = Stopwatch.StartNew();
            const int Reps = 10;
            for (int r = 0; r < Reps; r++)
            {
                bot.FailRate(start, (ulong)r, PourMode.Run);
            }

            watch.Stop();
            TestContext.WriteLine("bot ×40, 12 cuias: " + (watch.Elapsed.TotalMilliseconds / Reps).ToString("0.0", CultureInfo.InvariantCulture) + " ms por nivel");
            Assert.That(watch.Elapsed.TotalMilliseconds / Reps, Is.LessThan(2000));
        }

        [Test]
        public void EarlyTraps_OfTheTutorial_AreInLevels6And8Only()
        {
            // Q16 B: los niveles 6 y 8 del tutorial llegan a Aguas estancadas en 1 y 2 vertidos a propósito (02 §13); el
            // resto, nunca (informe de tools/level-generator).
            var traps = new EarlyTraps();
            using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), "Assets/_Project/Data/Levels/tutorial_v0.json"))))
            {
                foreach (JsonElement level in doc.RootElement.GetProperty("levels").EnumerateArray())
                {
                    int number = level.GetProperty("level").GetInt32();
                    int count = traps.Count(BuildBoard(level), PourMode.Run);
                    TestContext.WriteLine("nivel " + number + ": " + count + " trampas tempranas (estrictas: "
                        + traps.CountUnavoidable(BuildBoard(level), PourMode.Run) + ")");
                    if (number == 6 || number == 8)
                    {
                        Assert.That(count, Is.GreaterThan(0), "nivel " + number);
                    }
                    else
                    {
                        Assert.That(count, Is.EqualTo(0), "nivel " + number);
                    }
                }
            }
        }

        internal static BoardState TwelveCuiaBoard()
        {
            // Nivel 7 del lote híbrido del prototipo (walk, K = 10, 2 vacíos, 12 cuias; 03 §18.1).
            using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), "tools/out/levels_proto.json"))))
            {
                foreach (JsonElement level in doc.RootElement.GetProperty("levels").EnumerateArray())
                {
                    if (level.GetProperty("caps").GetArrayLength() == 12)
                    {
                        return BuildBoard(level);
                    }
                }
            }

            throw new InvalidOperationException("No hay un nivel de 12 cuias en tools/out/levels_proto.json.");
        }

        private static string Label(double botFail)
        {
            // 02 §10 y 08 §4.1: ≥ 0,90 very_hard, ≥ 0,75 hard.
            return botFail >= 0.90 ? "very_hard" : botFail >= 0.75 ? "hard" : "normal";
        }

        private static int Empties(BoardState state)
        {
            int empties = 0;
            for (int i = 0; i < state.Count; i++)
            {
                empties += state[i].IsEmpty ? 1 : 0;
            }

            return empties;
        }

        internal static BoardState BuildBoard(JsonElement level)
        {
            JsonElement cuias = level.GetProperty("cuias");
            JsonElement caps = level.GetProperty("caps");
            var built = new Cuia[cuias.GetArrayLength()];
            for (int i = 0; i < built.Length; i++)
            {
                JsonElement doses = cuias[i];
                var colors = new int[doses.GetArrayLength()];
                for (int k = 0; k < colors.Length; k++)
                {
                    colors[k] = doses[k].GetInt32();
                }

                built[i] = Cuia.Create(caps[i].GetInt32(), colors);
            }

            return new BoardState(built);
        }

        internal static string RepoRoot()
        {
            DirectoryInfo? dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "tools", "levelgen_proto.py")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("No se encontró la raíz del repositorio desde " + TestContext.CurrentContext.TestDirectory);
        }
    }
}
