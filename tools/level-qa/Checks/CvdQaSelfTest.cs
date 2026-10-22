// --self-test cvd (Q-B9-13 bis (b) y (A)): QA-CVD-ADJ, los pares del simulador de daltonismo (7-10 y 5-14) juntos en una cuia.
//   - el fixture fixtures/QA-CVD-ADJ__par_del_simulador_junto.json (el 46 del mundo 3, con un 7 sobre un 10 en el tablero
//     base) falla QA-CVD-ADJ sin la reproducción;
//   - un contacto solo en una variante de relief (un tablero del mundo 2 sin contactos en el base, con los tintes de una
//     variante cambiados para que un 7 toque un 10) es aviso, no fallo;
//   - GateChecks.CvdUnavoidable baja el fallo a aviso solo si el pack se reproduce y el generador no lo pudo evitar
//     (Tints.CvdAdjacent > 0): con el pack sin reproducir o con CvdAdjacent = 0 sigue siendo fallo, y no toca otras
//     comprobaciones.
// Imprime ASCII (PASS / FAIL) y sale con 0 si todo pasa, 1 si algo falla y 2 si no encuentra la especificación o los packs.
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using AF.Core.Board;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class CvdQaSelfTest
{
    public const string SpecPath = "tools/level-generator/specs/campaign_v1.json";
    public const string Fixture = "tools/level-qa/fixtures/QA-CVD-ADJ__par_del_simulador_junto.json";
    public const string World2 = "Assets/_Project/Data/Levels/mundo_02_v1.json";

    public static int Run()
    {
        CampaignSpec spec;
        try
        {
            spec = CampaignSpec.Load(SpecPath);
        }
        catch (CampaignSpecException e)
        {
            Console.Error.WriteLine(AsciiText.Of("level-qa --self-test cvd: " + e.Message + " (ejecuta desde la raiz del repositorio)"));
            return 2;
        }

        if (!File.Exists(Fixture) || !File.Exists(World2))
        {
            Console.Error.WriteLine("level-qa --self-test cvd: faltan " + Fixture + " o " + World2 + " (ejecuta desde la raiz del repositorio)");
            return 2;
        }

        int failures = 0;
        void Check(bool ok, string what)
        {
            Console.WriteLine(AsciiText.Of((ok ? "PASS " : "FAIL ") + what));
            failures += ok ? 0 : 1;
        }

        // 1. El fixture: un 7 sobre un 10 en el tablero base, sin reproducción.
        (QaContext ctx, List<QaIssue> issues) = Base(spec, Fixture);
        QaIssue? baseFail = issues.FirstOrDefault(i => i.Check == "QA-CVD-ADJ" && !i.Warning);
        Check(baseFail is not null, "fixture QA-CVD-ADJ__par_del_simulador_junto: QA-CVD-ADJ falla en el tablero base sin la reproduccion");

        // 2. Un contacto solo en una variante de relief: aviso, no fallo.
        string? reliefPath = ReliefOnly(out string what);
        if (reliefPath is null)
        {
            Check(false, "relief: ningun tablero del mundo 2 sirve para el caso (" + what + ")");
        }
        else
        {
            try
            {
                (_, List<QaIssue> r) = Base(spec, reliefPath);
                bool warned = r.Any(i => i.Check == "QA-CVD-ADJ" && i.Warning);
                bool failed = r.Any(i => i.Check == "QA-CVD-ADJ" && !i.Warning);
                Check(warned && !failed, "un contacto solo en una variante de relief (" + what + ") es aviso de QA-CVD-ADJ, no fallo");
            }
            finally
            {
                File.Delete(reliefPath);
            }
        }

        // 3. GateChecks.CvdUnavoidable.
        if (baseFail is not null)
        {
            QaPack pack = ctx.Packs[0];
            QaLevel lv = pack.Levels[0];
            var other = new QaIssue("QA-RISK", pack.Path, lv.Level, "otra comprobacion");
            bool Downgraded(bool reproducedPack, int cvdAdjacent, out bool otherUntouched)
            {
                var list = new List<QaIssue> { baseFail, other };
                HashSet<string> reproduced = reproducedPack ? [pack.PackId] : [];
                GateChecks.CvdUnavoidable(list, ctx, Result(spec, lv, cvdAdjacent), reproduced);
                otherUntouched = list[1] == other;
                return list[0].Warning && list[0].Check == "QA-CVD-ADJ";
            }

            Check(Downgraded(true, 1, out bool o1) && o1, "CvdUnavoidable: pack reproducido y Tints.CvdAdjacent = 1: aviso; las demas comprobaciones no cambian");
            Check(!Downgraded(true, 0, out bool o2) && o2, "CvdUnavoidable: pack reproducido y Tints.CvdAdjacent = 0: sigue siendo fallo");
            Check(!Downgraded(false, 1, out bool o3) && o3, "CvdUnavoidable: pack sin reproducir y Tints.CvdAdjacent = 1: sigue siendo fallo");
        }

        Console.WriteLine(failures == 0 ? "self-test cvd: todo PASS" : string.Create(CultureInfo.InvariantCulture, $"self-test cvd: {failures} FAIL"));
        return failures == 0 ? 0 : 1;
    }

    private static (QaContext Ctx, List<QaIssue> Issues) Base(CampaignSpec spec, string path)
    {
        List<QaPack> packs = [PackReader.Read(path)];
        List<LevelEval> evals = Evaluator.Evaluate(spec, packs);
        var ctx = new QaContext { Spec = spec, Packs = packs, Levels = evals };
        return (ctx, evals.SelectMany(e => BaseChecks.Run(ctx, e)).ToList());
    }

    /// <summary>
    /// Un pack de un nivel del mundo 2 sin contactos del simulador en el tablero base, con una variante de relief cuyos tintes
    /// se cambian (x ↔ 7, con x encima o debajo de un 10) para que un 7 toque un 10. Null si ningún nivel sirve.
    /// </summary>
    private static string? ReliefOnly(out string what)
    {
        JsonObject pack = JsonNode.Parse(File.ReadAllText(World2))!.AsObject();
        JsonArray levels = pack["levels"]!.AsArray();
        static int[][] Cuias(JsonNode n) => n.AsArray().Select(c => c!.AsArray().Select(x => x!.GetValue<int>()).ToArray()).ToArray();
        static int Contacts(int[][] cuias) => cuias.Sum(c => c.Zip(c.Skip(1)).Count(p => (p.First == 7 && p.Second == 10) || (p.First == 10 && p.Second == 7)));
        foreach (JsonNode? node in levels)
        {
            JsonObject lv = node!.AsObject();
            if (Contacts(Cuias(lv["cuias"]!)) > 0 || lv["relief"] is not JsonArray relief)
            {
                continue;
            }

            for (int r = 0; r < relief.Count; r++)
            {
                int[][] cuias = Cuias(relief[r]!["cuias"]!);
                bool has7 = cuias.Any(c => c.Contains(7));
                int x = cuias.SelectMany(c => c.Zip(c.Skip(1)).Where(p => p.First == 10 || p.Second == 10).Select(p => p.First == 10 ? p.Second : p.First))
                    .FirstOrDefault(v => v != 7 && v != 10 && v != Cuia.MudColor);
                if (!has7 || x == 0)
                {
                    continue;
                }

                var swapped = new JsonArray(cuias.Select(c => (JsonNode)new JsonArray(c.Select(v => (JsonNode)JsonValue.Create(v == x ? 7 : v == 7 ? x : v)!).ToArray())).ToArray());
                if (Contacts(Cuias(swapped)) == 0)
                {
                    continue;
                }

                relief[r]!["cuias"] = swapped;
                JsonObject copy = JsonNode.Parse(pack.ToJsonString())!.AsObject();
                copy["levels"] = new JsonArray(JsonNode.Parse(lv.ToJsonString()));
                string path = Path.Combine(Path.GetTempPath(), "af_qa_cvd_relief.json");
                File.WriteAllText(path, copy.ToJsonString(), new UTF8Encoding(false));
                what = string.Create(CultureInfo.InvariantCulture, $"nivel {lv["level"]}, relief {relief[r]!["type"]}, {x} <-> 7");
                return path;
            }
        }

        what = "sin variante con un 10 y un 7 que cambiar";
        return null;
    }

    /// <summary>El resultado de una regeneración con el nivel del pack elegido con <paramref name="cvdAdjacent"/> contactos sin evitar.</summary>
    private static CampaignResult Result(CampaignSpec spec, QaLevel lv, int cvdAdjacent)
    {
        LevelPlan plan = spec.Plan(lv.Level);
        BoardData board = lv.Board;
        var candidate = new Candidate
        {
            Level = lv.Level,
            Attempt = 0,
            Config = new StageConfig(0, lv.K, 1, "walk", [], 0, false, [], [], 0),
            Source = "walk",
            Board = board,
            State = board.Build().State!,
            Tints = new TintResult(new int[Cuia.MaxColor + 1], [], lv.RiskPairs, 0, cvdAdjacent),
            Depth = 0,
            Bot = lv.BotFail,
            MeasuredLabel = lv.Label,
            InWindow = true,
            Complexity = lv.Complexity,
            MaxRealities = 0,
        };
        var level = new GeneratedLevel { Plan = plan, Slot = new SlotResult(plan, [candidate], 1, [], false, 0), Chosen = candidate };
        return new CampaignResult([level], [], [], []);
    }
}
