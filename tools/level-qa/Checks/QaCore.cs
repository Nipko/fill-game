// Núcleo de level-qa: recalcula cada nivel con AF.Core y AF.Solver (en paralelo, un motor por hilo) y deja los
// resultados para las comprobaciones (Checks/*.cs). Cada comprobación tiene un id estable que citan los fixtures.
using System.Collections.Concurrent;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Murky;
using AF.Solver.Search;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal sealed record QaIssue(string Check, string Pack, int Level, string Message, bool Warning = false);

internal sealed class ReliefEval
{
    public required QaRelief Relief { get; init; }
    public BoardState? State { get; init; }
    public string? Error { get; init; }
    public ParResult? Par { get; init; }
    public double Bot { get; init; }
}

internal sealed class LevelEval
{
    public required QaLevel Level { get; init; }
    public LevelPlan? Plan { get; init; }
    public BoardState? State { get; init; }
    public string? BuildError { get; init; }
    public string? ValidationError { get; init; }
    public ParResult? Par { get; init; }
    public double Bot { get; init; }
    public int EarlyTraps { get; init; }
    public SafePathStatus? Safe { get; init; }
    public List<ReliefEval> Relief { get; init; } = [];
}

internal sealed class QaContext
{
    public required CampaignSpec Spec { get; init; }
    public required List<QaPack> Packs { get; init; }
    public required List<LevelEval> Levels { get; init; }

    public LevelEval? At(int level) => Levels.FirstOrDefault(l => l.Level.Level == level);
}

internal static class Evaluator
{
    public static List<LevelEval> Evaluate(CampaignSpec spec, List<QaPack> packs)
    {
        var all = packs.SelectMany(p => p.Levels).ToList();
        var results = new ConcurrentDictionary<QaLevel, LevelEval>();
        var engines = new ThreadLocal<(MetricsEngine Metrics, SafePath Safe)>(() => (new MetricsEngine(), new SafePath()));
        Parallel.ForEach(all, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, lv =>
        {
            results[lv] = EvaluateOne(spec, lv, engines.Value.Metrics, engines.Value.Safe);
        });

        return all.Select(l => results[l]).OrderBy(e => e.Level.Level).ThenBy(e => e.Level.Pack, StringComparer.Ordinal).ToList();
    }

    private static LevelEval EvaluateOne(CampaignSpec spec, QaLevel lv, MetricsEngine metrics, SafePath safePath)
    {
        LevelPlan? plan = spec.Calendar.ContainsKey(lv.Level) ? spec.Plan(lv.Level) : null;
        UnitsPerColor units = lv.UnitsOverrides.Count == 0
            ? UnitsPerColor.Uniform(lv.UnitsDefault)
            : UnitsPerColor.FromObject(lv.UnitsOverrides);
        BoardData b = lv.Board;
        BoardBuildResult built = BoardBuilder.Build(
            b.Caps, b.Cuias, b.HasMechanics ? b.Attrs : null, b.Hidden.Length > 0 ? b.Hidden : null, units);
        if (!built.IsValid)
        {
            return new LevelEval { Level = lv, Plan = plan, BuildError = built.Error };
        }

        BoardState state = built.State!;
        string? validation = LevelValidation.Validate(state);
        ParResult par = metrics.Par(state);
        double bot = metrics.BotFail(state, lv.Seed);
        int early = metrics.EarlyTraps(state);
        SafePathStatus? safe = null;
        if (state.HasHidden)
        {
            safe = safePath.Find(state, SafeMoves.ForPipeline(PourMode.Run)).Status;
        }

        var relief = new List<ReliefEval>();
        foreach (QaRelief r in lv.Relief)
        {
            BoardBuildResult rb = r.Board.Build();
            if (!rb.IsValid)
            {
                relief.Add(new ReliefEval { Relief = r, Error = rb.Error });
                continue;
            }

            string? rv = LevelValidation.Validate(rb.State!);
            ParResult? rp = r.Type == "minus_color" ? metrics.Par(rb.State!, Relief.MinusColorBudget) : null;
            relief.Add(new ReliefEval
            {
                Relief = r,
                State = rb.State,
                Error = rv,
                Par = rp,
                Bot = metrics.BotFail(rb.State!, lv.Seed),
            });
        }

        return new LevelEval
        {
            Level = lv,
            Plan = plan,
            State = state,
            ValidationError = validation,
            Par = par,
            Bot = bot,
            EarlyTraps = early,
            Safe = safe,
            Relief = relief,
        };
    }

    /// <summary>Igualdad a 3 decimales (bot_fail se publica con 3).</summary>
    public static bool Same3(double a, double b) => Math.Abs(a - b) < 0.0005;
}
