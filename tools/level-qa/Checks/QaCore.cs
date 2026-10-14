// Núcleo de level-qa: recalcula cada nivel con AF.Core y AF.Solver (en paralelo, un motor por hilo) y deja los
// resultados para las comprobaciones (Checks/*.cs). Cada comprobación tiene un id estable que citan los fixtures.
// Búsquedas con las opciones del nivel (T55; propuesta de la tinaja c.2; C1-8): el par, las trampas y la prueba de
// irresolubilidad con SolverOptions.ForLevel (Metrics.cs, el mismo código que el generador) y la ruta segura de Aguas
// turbias con SafeMoves.ForPipeline(modo, nivel), sin poda de fútiles en los niveles con tinaja. En los niveles con tinaja
// (M13; D-019) se repiten los filtros del generador con el mismo código y las mismas semillas (TinajaWalks.Check: ruta
// segura, 20 paseos seguros, 30 casuales, «S0 sin trampas» en los tableros de estreno y la compuerta de coste de Morpho con
// el mismo tope y factor que P1), y en cada variante de alivio con la tinaja, la ruta segura y los paseos con
// TinajaWalks.ReliefOptions (las de Morpho [P], pendiente de decisión), sin la compuerta de coste (Relief.TinajaReject).
using System.Collections.Concurrent;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Murky;
using AF.Solver.Search;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;
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

    /// <summary>Ruta segura y paseos de la variante con la tinaja (null si no la lleva o no es válida).</summary>
    public TinajaCheckResult? Tinaja { get; init; }
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
    /// <summary>Ruta segura de Aguas turbias (null si el nivel no tiene ocultas fuera de una tinaja).</summary>
    public SafePathStatus? Safe { get; init; }

    /// <summary>Filtros de la tinaja (TinajaWalks.Check, como el generador); null si el nivel no la lleva o no es válido.</summary>
    public TinajaCheckResult? Tinaja { get; init; }

    /// <summary>
    /// Vertidos útiles de S0 que no son seguros en un tablero de estreno con tinaja (TinajaWalks.S0Traps; int.MaxValue si S0 no
    /// tiene vertido seguro); −1 si no se mide.
    /// </summary>
    public int TinajaS0Traps { get; init; } = -1;

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
        var engines = new ThreadLocal<(MetricsEngine Metrics, SafePath Safe, TinajaWalks Tinaja)>(() => (new MetricsEngine(), new SafePath(), new TinajaWalks()));
        Parallel.ForEach(all, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, lv =>
        {
            results[lv] = EvaluateOne(spec, lv, engines.Value.Metrics, engines.Value.Safe, engines.Value.Tinaja);
        });

        return all.Select(l => results[l]).OrderBy(e => e.Level.Level).ThenBy(e => e.Level.Pack, StringComparer.Ordinal).ToList();
    }

    private static LevelEval EvaluateOne(CampaignSpec spec, QaLevel lv, MetricsEngine metrics, SafePath safePath, TinajaWalks walks)
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
        TinajaCheckResult? tinaja = null;
        int s0Traps = -1;
        if (b.HasTinaja)
        {
            // Los mismos filtros y semillas que el generador (SlotGenerator.Measure: Check(estado, plan.DebutBoard, semilla)).
            // Solo sobre un tablero válido: con uno inválido ya falla QA-VALID y las búsquedas no tienen sentido.
            if (validation is null)
            {
                bool debut = plan?.DebutBoard ?? false;
                tinaja = walks.Check(state, debut, lv.Seed);
                if (debut)
                {
                    // Check solo mide S0 sin trampas si la ruta y la compuerta de Morpho pasan; si no, se mide aparte (con un
                    // memo vacío) para que QA-TINAJA-DEBUT lo diga igual.
                    s0Traps = tinaja.S0Traps >= 0
                        ? tinaja.S0Traps
                        : new TinajaWalks().S0Traps(state, SafeMoves.ForPipeline(PourMode.Run, state));
                }
            }
        }
        else if (state.HasHidden)
        {
            safe = safePath.Find(state, SafeMoves.ForPipeline(PourMode.Run, state)).Status;
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

            // Variante con la tinaja (03 §13; propuesta de la tinaja c.3 punto 4): la ruta segura y los paseos, con las
            // opciones y la semilla de Relief.TinajaReject (semilla del nivel + "/" + tipo; sin la compuerta de coste).
            TinajaCheckResult? rt = rv is null && r.Board.HasTinaja
                ? walks.Check(rb.State!, debut: false, lv.Seed + "/" + r.Type, morphoGate: false, relief: true)
                : null;
            relief.Add(new ReliefEval
            {
                Relief = r,
                State = rb.State,
                Error = rv,
                Par = rp,
                Bot = metrics.BotFail(rb.State!, lv.Seed),
                Tinaja = rt,
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
            Tinaja = tinaja,
            TinajaS0Traps = s0Traps,
            Relief = relief,
        };
    }

    /// <summary>Igualdad a 3 decimales (bot_fail se publica con 3).</summary>
    public static bool Same3(double a, double b) => Math.Abs(a - b) < 0.0005;
}
