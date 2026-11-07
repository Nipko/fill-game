// --measure: medición de la fase F1 de la curva de la 1.1 (D-068; docs/dev/propuesta-curva-1.1.md §4.1 y §7). Genera los
// niveles pedidos con el mismo código que --spec (CampaignRunner.Generate, fases 1-3) pero sin detenerse en un ciclo sin
// colocación, y escribe un informe Markdown con T (cuias), K (colores), el bot casual ×40 y la ventana por papel del ciclo
// de 20, los ciclos que no colocan y los retos e hitos uno a uno. No escribe packs ni alivio: es una medida, no contenido.
// El informe no lleva tiempos: dos ejecuciones dan los mismos bytes.
using System.Globalization;
using System.Text;
using AmazonFill.Tools.LevelGenerator.Campaign;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal static class MeasureRunner
{
    /// <summary>
    /// Topes de cuias por papel de D-068 (P2 A; propuesta §4.1) [P]: respiro (recuperación y remanso) 8-10, estreno 9-10,
    /// comodidad 9-11, reto 10-11 (12 como mucho), hito 12-13 y cúspide (el hito de la firma del mundo) 13-14.
    /// </summary>
    public static readonly (string Role, int Min, int Max)[] Caps =
    [
        ("recuperacion", 8, 10),
        ("estreno", 9, 10),
        ("comodidad", 9, 11),
        ("reto", 10, 12),
        ("remanso", 8, 10),
        ("hito", 12, 13),
        ("cuspide", 13, 14),
    ];

    /// <summary>Papel del nivel para la medida: el de su posición, con el estreno aparte y la cúspide (hito de la firma).</summary>
    public static string RoleOf(LevelPlan plan) =>
        plan.Role == SlotRole.Milestone && plan.Level == plan.World.Last ? "cuspide"
        : plan.DebutBoard && plan.Role != SlotRole.Milestone ? "estreno"
        : plan.RoleName;

    public static int Run(string specPath, string? variant, string levelsArg, string reportPath)
    {
        CampaignSpec spec;
        try
        {
            spec = CampaignSpec.Load(specPath, variant);
        }
        catch (CampaignSpecException e)
        {
            Console.Error.WriteLine(AsciiText.Of($"level-generator: {e.Message}"));
            return 2;
        }

        if (SpecValidator.Validate(spec).Any(i => !i.Warning))
        {
            Console.Error.WriteLine("level-generator: la especificacion no pasa --validate-spec; corrigela antes de medir.");
            return 1;
        }

        string[] parts = levelsArg.Split('-');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int first)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int last)
            || first < spec.FirstLevel || last > spec.LastLevel || first > last)
        {
            Console.Error.WriteLine($"level-generator: --levels debe ser a-b dentro de {spec.FirstLevel}-{spec.LastLevel}: {levelsArg}");
            return 2;
        }

        CampaignResult result = CampaignRunner.Generate(spec, first, last, Console.WriteLine, verbose: true, keepGoing: true)!;
        string text = Build(spec, specPath, variant, levelsArg, result);
        string? dir = Path.GetDirectoryName(Path.GetFullPath(reportPath));
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllBytes(reportPath, PackWriter.Bytes(text));
        Console.WriteLine($"medida: {reportPath.Replace('\\', '/')} ({result.UnplacedBlocks.Count} ciclo(s) sin colocacion)");
        return 0;
    }

    public static string Build(CampaignSpec spec, string specPath, string? variant, string levelsArg, CampaignResult result)
    {
        var sb = new StringBuilder();
        void W(string line = "") => sb.Append(line).Append('\n');
        static string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);

        List<GeneratedLevel> levels = result.Levels;
        List<GeneratedLevel> placed = levels.Where(l => l.Chosen is not null).ToList();
        string variantText = variant is null ? "" : $" --variant {variant}";
        W(F($"# Medida de la curva: {spec.SpecId}, niveles {levelsArg}{(variant is null ? "" : $", variante {variant}")}"));
        W();
        W("Generado por (no editar a mano):");
        W();
        W("```");
        W(F($"dotnet run --project tools/level-generator -c Release -- --measure --spec {specPath.Replace('\\', '/')} --levels {levelsArg}{variantText} --report <este archivo>"));
        W("```");
        W();
        W("Fases 1-3 de `--spec` (candidatos, índice D y colocación en el ciclo de 20 de D-009) sin alivio ni packs; un ciclo sin");
        W("colocación no detiene la medida. «En ventana»: el elegido mide la ventana de su slot (reto 75-89 %, hito ≥ 90 %, comodidad");
        W("20-60 %…); un reto o hito fuera de ella se publicaría con la etiqueta que mide (c.2). T = cuias del tablero, K = colores,");
        W("libres = cuias vacías, bot = fallo del bot casual ×40 del elegido. Topes de T por papel: D-068 (P2 A).");
        W();

        // Resumen.
        List<GeneratedLevel> challenges = levels.Where(l => l.Plan.Role == SlotRole.Challenge).ToList();
        List<GeneratedLevel> milestones = levels.Where(l => l.Plan.Role == SlotRole.Milestone).ToList();
        int cycles = levels.Select(l => (l.Plan.Level - spec.CycleFrom) / 20).Distinct().Count();
        W("## Resumen");
        W();
        W("| Medida | Valor |");
        W("|---|---|");
        W(F($"| Niveles | {levels.Count} ({cycles} ciclos) |"));
        W(F($"| Ciclos sin colocación | {result.UnplacedBlocks.Count}{(result.UnplacedBlocks.Count == 0 ? "" : " (" + string.Join(", ", result.UnplacedBlocks.Select(b => F($"{b}-{b + 19}"))) + ")")} |"));
        W(F($"| Retos colocados en su ventana | {challenges.Count(l => l.Chosen?.InWindow == true)} de {challenges.Count} |"));
        W(F($"| Hitos colocados en su ventana | {milestones.Count(l => l.Chosen?.InWindow == true)} de {milestones.Count} |"));
        W(F($"| Retos con algún candidato en su ventana | {challenges.Count(l => l.Slot.Candidates.Any(c => c.PoolMember))} de {challenges.Count} |"));
        if (placed.Count > 0)
        {
            int[] ts = placed.Select(l => l.Chosen!.Board.Caps.Length).OrderBy(x => x).ToArray();
            int[] ks = placed.Select(l => l.Chosen!.K).OrderBy(x => x).ToArray();
            W(F($"| T media / mediana (colocados) | {ts.Average():0.0} / {Median(ts):0.#} |"));
            W(F($"| K media / mediana (colocados) | {ks.Average():0.0} / {Median(ks):0.#} |"));
            W(F($"| Niveles con T ≤ 11 / T ≥ 12 / T ≥ 13 | {ts.Count(t => t <= 11)} / {ts.Count(t => t >= 12)} / {ts.Count(t => t >= 13)} |"));
            int murky = placed.Count(l => l.Chosen!.Murky);
            W(F($"| Niveles con ocultas (dosis) | {murky} ({100.0 * murky / placed.Count:0} %; {placed.Sum(l => l.Chosen!.Murky ? l.Chosen.Board.Hidden.Length : 0)} dosis) |"));
            int outside = placed.Count(l => !InCap(l));
            W(F($"| Fuera del tope de T de su papel | {outside} |"));
        }

        W();

        // Por papel.
        W("## Por papel");
        W();
        W("| Papel | Tope T | Niveles | T mín-med-máx | T (recuento) | K mín-med-máx | Libres (med.) | Bot (mediana) | Bot mín-máx | En ventana | Fuera del tope | Con ocultas |");
        W("|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach ((string role, int min, int max) in Caps)
        {
            List<GeneratedLevel> rows = placed.Where(l => RoleOf(l.Plan) == role).ToList();
            int total = levels.Count(l => RoleOf(l.Plan) == role);
            if (total == 0)
            {
                continue;
            }

            if (rows.Count == 0)
            {
                W(F($"| {role} | {min}-{max} | 0 de {total} | — | — | — | — | — | — | — | — | — |"));
                continue;
            }

            int[] t = rows.Select(l => l.Chosen!.Board.Caps.Length).OrderBy(x => x).ToArray();
            int[] k = rows.Select(l => l.Chosen!.K).OrderBy(x => x).ToArray();
            int[] free = rows.Select(l => l.Chosen!.FreeEmpties).OrderBy(x => x).ToArray();
            double[] bot = rows.Select(l => l.Chosen!.Bot).OrderBy(x => x).ToArray();
            string hist = string.Join(" · ", t.GroupBy(x => x).OrderBy(g => g.Key).Select(g => F($"{g.Key}: {g.Count()}")));
            W(F($"| {role} | {min}-{max} | {rows.Count}{(rows.Count == total ? "" : F($" de {total}"))} | {t[0]}-{Median(t):0.#}-{t[^1]} | {hist} | {k[0]}-{Median(k):0.#}-{k[^1]} | {Median(free):0.#} | {Pct(Median(bot))} | {Pct(bot[0])}-{Pct(bot[^1])} | {rows.Count(l => l.Chosen!.InWindow)} | {rows.Count(l => !InCap(l))} | {rows.Count(l => l.Chosen!.Murky)} |"));
        }

        W();

        // Por ciclo.
        W("## Por ciclo");
        W();
        W("| Ciclo | Coloca | Retos en ventana | T de los retos | Bot de los retos | Hito | T del hito | Bot del hito | En ventana |");
        W("|---|---|---|---|---|---|---|---|---|");
        foreach (IGrouping<int, GeneratedLevel> g in levels.GroupBy(l => (l.Plan.Level - spec.CycleFrom) / 20).OrderBy(g => g.Key))
        {
            List<GeneratedLevel> cyc = g.OrderBy(l => l.Plan.Level).ToList();
            bool ok = cyc.All(l => l.Chosen is not null);
            List<GeneratedLevel> ch = cyc.Where(l => l.Plan.Role == SlotRole.Challenge).ToList();
            GeneratedLevel? ms = cyc.FirstOrDefault(l => l.Plan.Role == SlotRole.Milestone);
            string chT = ok ? string.Join(", ", ch.Select(l => l.Chosen!.Board.Caps.Length.ToString(CultureInfo.InvariantCulture))) : "—";
            string chBot = ok ? string.Join(", ", ch.Select(l => Pct(l.Chosen!.Bot))) : "—";
            string msText = ms is null ? "—" : ms.Plan.Cell.Short;
            string msT = ms?.Chosen is null ? "—" : ms.Chosen.Board.Caps.Length.ToString(CultureInfo.InvariantCulture);
            string msBot = ms?.Chosen is null ? "—" : Pct(ms.Chosen.Bot);
            string msIn = ms?.Chosen is null ? "—" : ms.Chosen.InWindow ? "sí" : "no";
            W(F($"| {cyc[0].Plan.Level}-{cyc[^1].Plan.Level} | {(ok ? "sí" : "**no**")} | {(ok ? F($"{ch.Count(l => l.Chosen!.InWindow)} de {ch.Count}") : "—")} | {chT} | {chBot} | {msText} | {msT} | {msBot} | {msIn} |"));
        }

        W();

        // Retos e hitos.
        W("## Retos e hitos");
        W();
        W("Candidatos: en ventana (del pool) / medidos / intentos del slot, contando las rondas extra.");
        W();
        W("| Nivel | p | Papel | Celda | K plan | T | K | Libres | Ocultas | Par | Bot | Etiqueta | En ventana | Candidatos |");
        W("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (GeneratedLevel l in levels.Where(l => l.Plan.Role is SlotRole.Challenge or SlotRole.Milestone))
        {
            W(Row(l));
        }

        W();
        W("## Todos los niveles");
        W();
        W("| Nivel | p | Papel | Celda | K plan | T | K | Libres | Ocultas | Par | Bot | Etiqueta | En ventana | Candidatos |");
        W("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (GeneratedLevel l in levels)
        {
            W(Row(l));
        }

        return sb.ToString();
    }

    private static string Row(GeneratedLevel l)
    {
        Candidate? c = l.Chosen;
        List<Candidate> cands = l.Slot.Candidates;
        string counts = string.Create(CultureInfo.InvariantCulture,
            $"{cands.Count(x => x.PoolMember)} / {cands.Count(x => x.Measured)} / {l.Slot.Attempts}");
        string cell = l.Plan.Cell.Short.Replace("|", "\\|", StringComparison.Ordinal);
        if (c is null)
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"| {l.Plan.Level} | {l.Plan.Position} | {RoleOf(l.Plan)} | {cell} | {l.Plan.K} | — | — | — | — | — | — | sin colocar | — | {counts} |");
        }

        return string.Create(CultureInfo.InvariantCulture,
            $"| {l.Plan.Level} | {l.Plan.Position} | {RoleOf(l.Plan)} | {cell} | {l.Plan.K} | {c.Board.Caps.Length} | {c.K} | {c.FreeEmpties} | {(c.Murky ? c.Board.Hidden.Length : 0)} | {c.Par} | {Pct(c.Bot)} | {c.MeasuredLabel} | {(c.InWindow ? "sí" : "no")} | {counts} |");
    }

    private static bool InCap(GeneratedLevel l)
    {
        string role = RoleOf(l.Plan);
        int t = l.Chosen!.Board.Caps.Length;
        return Caps.Where(c => c.Role == role).All(c => t >= c.Min && t <= c.Max);
    }

    private static double Median(int[] sorted) =>
        sorted.Length % 2 == 1 ? sorted[sorted.Length / 2] : (sorted[(sorted.Length / 2) - 1] + sorted[sorted.Length / 2]) / 2.0;

    private static double Median(double[] sorted) =>
        sorted.Length % 2 == 1 ? sorted[sorted.Length / 2] : (sorted[(sorted.Length / 2) - 1] + sorted[sorted.Length / 2]) / 2.0;

    private static string Pct(double v) => string.Create(CultureInfo.InvariantCulture, $"{v * 100:0} %");
}
