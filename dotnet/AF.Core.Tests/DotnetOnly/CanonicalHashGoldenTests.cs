#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using AF.Core.Board;
using AF.Core.Mechanics;
using NUnit.Framework;

namespace AF.Core.Tests.DotnetOnly
{
    /// <summary>
    /// Golden del hash canónico de 64 bits (03 §3; D-068, fase F2): el Aguapé renumera sus ids de grupo al canonicalizar y
    /// eso no puede cambiar el hash de ningún tablero de la 1.0. Recorre todos los tableros publicados (el tablero base y
    /// cada variante de <c>relief</c> de los packs de campaña, de la reserva y del tutorial, y los 2.187 del Reto del río) y
    /// fija, por pack, un resumen FNV-1a de 64 bits de sus hashes canónicos en orden. Los valores se calcularon con el núcleo
    /// de 1830df08, antes de F2. Si cambian, cambia el hash de algún tablero publicado: el generador, el solver (visitados) y
    /// el bot (orden canónico) darían otros resultados. Solo dotnet: lee los packs.
    /// </summary>
    [TestFixture]
    public sealed class CanonicalHashGoldenTests
    {
        private static readonly object[] Packs =
        {
            new object[] { "Levels/tutorial_v0.json", 10, 0x53F0E6F2BEF48A05UL },
            new object[] { "Levels/mundo_02_v1.json", 50, 0x8E291A2C9803B1CAUL },
            new object[] { "Levels/mundo_03_v1.json", 119, 0x42768D37F8C41983UL },
            new object[] { "Levels/mundo_04_v1.json", 179, 0x7A5A6C81C2F780C9UL },
            new object[] { "Levels/mundo_05_v1.json", 178, 0x1E3CEB06A85798CAUL },
            new object[] { "Levels/mundo_06_v1.json", 298, 0x7497ED7DE43A12C1UL },
            new object[] { "Levels/mundo_07_v1.json", 360, 0x79C9DC4E0B9B8197UL },
            new object[] { "Levels/mundo_08_v1.json", 417, 0x1EE6B39A64848B07UL },
            new object[] { "Levels/mundo_09_v1.json", 416, 0x2162506E512F4E59UL },
            new object[] { "Levels/reserva_03_v1.json", 24, 0xDAE4D7FE0EFFE125UL },
            new object[] { "Levels/reserva_04_v1.json", 36, 0x421ED95F45AE3FF8UL },
            new object[] { "Levels/reserva_05_v1.json", 36, 0xF603D0FD9E3E15E3UL },
            new object[] { "Levels/reserva_06_v1.json", 60, 0xF34258D9A7E76118UL },
            new object[] { "Levels/reserva_07_v1.json", 72, 0x72DD85BAF680FFB5UL },
            new object[] { "Levels/reserva_08_v1.json", 83, 0x9299CFE97B7D4E44UL },
            new object[] { "Levels/reserva_09_v1.json", 84, 0x23A63ED35AA4A2CCUL },
            new object[] { "Daily/daily_pool_v1.json", 2187, 0x50584471295B0477UL },
        };

        [TestCaseSource(nameof(Packs))]
        public void PublishedBoards_KeepTheirCanonicalHash_03_3(string pack, int boards, ulong digest)
        {
            List<BoardState> states = Load(pack);
            ulong actual = BoardCanon.FnvOffsetBasis;
            for (int i = 0; i < states.Count; i++)
            {
                actual = Fold(actual, states[i].CanonicalHash());
            }

            TestContext.WriteLine(pack + ": " + states.Count.ToString(CultureInfo.InvariantCulture) + " tableros, resumen 0x" + actual.ToString("X16", CultureInfo.InvariantCulture));
            Assert.That(states.Count, Is.EqualTo(boards), pack);
            Assert.That(actual, Is.EqualTo(digest), pack);
        }

        private static ulong Fold(ulong hash, ulong value)
        {
            unchecked
            {
                for (int shift = 0; shift < 64; shift += 8)
                {
                    hash ^= (value >> shift) & 0xFFUL;
                    hash *= BoardCanon.FnvPrime;
                }
            }

            return hash;
        }

        /// <summary>Los tableros del pack, en orden: de cada nivel, el base y después sus variantes de <c>relief</c>.</summary>
        internal static List<BoardState> Load(string pack)
        {
            string path = Path.Combine(RepoRoot(), "Assets", "_Project", "Data", pack.Replace('/', Path.DirectorySeparatorChar));
            var states = new List<BoardState>();
            using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path)))
            {
                foreach (JsonElement level in doc.RootElement.GetProperty("levels").EnumerateArray())
                {
                    states.Add(BuildBoard(level));
                    if (level.TryGetProperty("relief", out JsonElement relief) && relief.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement variant in relief.EnumerateArray())
                        {
                            states.Add(BuildBoard(variant));
                        }
                    }
                }
            }

            return states;
        }

        /// <summary>Tablero de un nivel o de una variante (<c>af-level/1</c>, 08 §4.1): caps, cuias, attrs y hidden.</summary>
        internal static BoardState BuildBoard(JsonElement level)
        {
            JsonElement capsNode = level.GetProperty("caps");
            JsonElement cuiasNode = level.GetProperty("cuias");
            int count = capsNode.GetArrayLength();
            var caps = new int[count];
            var cuias = new int[count][];
            for (int i = 0; i < count; i++)
            {
                caps[i] = capsNode[i].GetInt32();
                JsonElement doses = cuiasNode[i];
                cuias[i] = new int[doses.GetArrayLength()];
                for (int k = 0; k < cuias[i].Length; k++)
                {
                    cuias[i][k] = doses[k].GetInt32();
                }
            }

            MechanicSpec[]? attrs = null;
            if (level.TryGetProperty("attrs", out JsonElement attrsNode) && attrsNode.ValueKind == JsonValueKind.Array)
            {
                for (int i = 0; i < count; i++)
                {
                    JsonElement entry = attrsNode[i];
                    if (entry.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    Assert.That(MechanicTypes.TryParse(entry.GetProperty("type").GetString(), out MechanicType type), Is.True);
                    attrs ??= new MechanicSpec[count];
                    attrs[i] = new MechanicSpec(type, Optional(entry, "key", 0), Optional(entry, "partner", -1), Optional(entry, "n", 0), Optional(entry, "k", 0), Optional(entry, "key2", 0), Optional(entry, "group", -1));
                }
            }

            int[][]? hidden = null;
            if (level.TryGetProperty("hidden", out JsonElement hiddenNode) && hiddenNode.ValueKind == JsonValueKind.Array && hiddenNode.GetArrayLength() > 0)
            {
                hidden = new int[hiddenNode.GetArrayLength()][];
                for (int h = 0; h < hidden.Length; h++)
                {
                    hidden[h] = new[] { hiddenNode[h][0].GetInt32(), hiddenNode[h][1].GetInt32() };
                }
            }

            BoardBuildResult built = BoardBuilder.Build(caps, cuias, attrs, hidden);
            Assert.That(built.Error, Is.Null);
            return built.State!;
        }

        private static int Optional(JsonElement entry, string name, int fallback)
        {
            return entry.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : fallback;
        }

        private static string RepoRoot()
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

            throw new InvalidOperationException("No se encuentra la raíz del repositorio.");
        }
    }
}
