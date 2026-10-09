// Candidatos de un slot (GDD 03 §2 y §4.4; D-009; propuesta c.3-c.4): tablero base de la fuente de la banda (walk, deep o
// fill), superposición de mecánicas (03 §9), tintes (03 §4.5), orden visual, verificación con AF.Core/AF.Solver
// (BoardBuilder, LevelValidation, SafePath en Aguas turbias, A* con par ≥ 2K) y medición (bot ×40 con la semilla del nivel,
// trampas tempranas con el tope de su etiqueta, métricas de la ruta para el índice D).
// Rechazo por ventana (03 §4.4): hasta 30 candidatos por etapa; si no se llena el pool, se relaja en el orden de 03 §4.4
// (comodidad: Cuia chica de 2 como «medio vacío» y después 2 vacíos; reto e hito: fill con 2 vacíos) y se registra.
// Cada candidato sale de una sub-semilla de la semilla del nivel (03 §2, regla 1): el slot se reproduce desde su seed.
using System.Globalization;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Solver.Generation;
using AF.Solver.Murky;
using AF.Solver.Search;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

/// <summary>Configuración del tablero base de una etapa.</summary>
internal sealed record StageConfig(
    int Stage, int K, int Empties, string Source, int[] Chicas, int AltaCap, bool Slack,
    IReadOnlyList<MechanicInfo> Mechanics, IReadOnlyList<string> Notes, int DeepNodes);

internal sealed class Candidate
{
    public required int Level { get; init; }
    public required int Attempt { get; init; }
    public required StageConfig Config { get; init; }
    public required string Source { get; init; }
    public required BoardData Board { get; init; }
    public required BoardState State { get; init; }
    public required TintResult Tints { get; init; }
    public required int Depth { get; init; }
    public required double Bot { get; init; }
    public required string MeasuredLabel { get; init; }
    public required bool InWindow { get; init; }
    public required int Complexity { get; init; }
    public required int MaxRealities { get; init; }
    public int Par { get; set; } = -1;
    public int Expanded { get; set; }
    public int EarlyTraps { get; set; } = -1;
    public double Branching { get; set; }
    public double Traps { get; set; }
    public string? Reject { get; set; }
    public bool Measured { get; set; }
    public double D { get; set; }
    public double Quantile { get; set; }

    public bool PoolMember => Measured && Reject is null && InWindow;

    public int K => Tints.Palette.Count;

    public int FreeEmpties => MetricsEngine.FreeEmpties(State);

    public int Empties => MetricsEngine.Empties(State);

    public bool Murky => Board.Hidden.Length > 0;
}

internal sealed record SlotResult(LevelPlan Plan, List<Candidate> Candidates, int Attempts, List<string> Notes, bool MurkyDropped, int Rejected)
{
    /// <summary>Rondas extra de candidatos con Aguas turbias antes del respaldo de T1 (CampaignRunner).</summary>
    public int MurkyRounds { get; init; }
}

internal sealed class SlotGenerator(CampaignSpec spec)
{
    /// <summary>Candidatos por etapa (03 §4.4: «máximo 30 candidatos»): 30 con fill o deep, que cuestan A* o 20.000 nodos por
    /// candidato; con walk, que cuesta milisegundos, se permiten más [P] porque el tope de trampas tempranas (Q16 B) y
    /// par ≥ 2K rechazan muchos tableros de comodidad con 1 vacío.</summary>
    public const int CandidatesPerStage = 30;

    public const int WalkCandidatesPerStage = 150;

    /// <summary>Con Aguas turbias, 120: la ruta segura solo se busca en ventana y los tableros turbios entran poco en ella [P].</summary>
    public const int MurkyCandidatesPerStage = 120;

    public static int AttemptsFor(StageConfig config) =>
        config.Mechanics.Any(m => m.Code == "Tu") ? MurkyCandidatesPerStage
        : config.Source == "walk" ? WalkCandidatesPerStage : CandidatesPerStage;
    public const int Stages = 3;
    public const ulong CandidateStream = 0x47454E00UL;

    /// <summary>Nodos del «deep corto» (03 §4.4: walk/deep corto) en las filas fijas de 02 §13 [P].</summary>
    public const int RowShortDeepNodes = 2000;

    private readonly MetricsEngine _metrics = new();
    private readonly InverseWalk _walk = new();
    private readonly InverseDeep _deep = new();
    private readonly AStarSolver _fillSolver = new();
    private readonly SafePath _safePath = new();

    public MetricsEngine Metrics => _metrics;

    /// <summary>Nivel cuya generación se traza por stderr (variable de entorno LEVELGEN_TRACE; diagnóstico).</summary>
    private static readonly int Trace = int.TryParse(Environment.GetEnvironmentVariable("LEVELGEN_TRACE"), NumberStyles.None, CultureInfo.InvariantCulture, out int t) ? t : -1;

    /// <summary>
    /// Candidatos en ventana que se buscan por slot (03 §7: el pool es ≥ 3× los slots): 6 en retos e hitos (la escalera de
    /// D-009 necesita variedad) y en los niveles que siguen a un reto o al hito (tienen que bajar una banda); 3 en el resto.
    /// </summary>
    public static int PoolTarget(LevelPlan plan) =>
        plan.Role is SlotRole.Challenge or SlotRole.Milestone || (!plan.Ramp && plan.Position is 1 or 7 or 10 or 13 or 16 or 19) ? 6 : 3;

    /// <summary>Genera los candidatos del slot hasta llenar el pool (o agotar las etapas) desde el intento <paramref name="firstAttempt"/>.</summary>
    public SlotResult Generate(LevelPlan plan, int extraTarget = 0, int firstAttempt = 0, bool dropMurky = false)
    {
        var candidates = new List<Candidate>();
        var notes = new List<string>();
        int target = PoolTarget(plan) + extraTarget;
        int attempts = 0;
        int rejected = 0;
        ulong levelSeed = Prng.SeedFrom(plan.SeedText);
        bool harder = false;
        for (int stage = 0; stage < Stages && candidates.Count(c => c.PoolMember) < target; stage++)
        {
            if (stage == 1)
            {
                // Dirección de la relajación: si la mayoría de los candidatos quedaron por debajo de la ventana (demasiado
                // fáciles), se endurece la fuente; si no, se alivia en el orden de 03 §4.4.
                // Los que entraron en la ventana pero se rechazaron (par < 2K, trampas tempranas) piden más espacio, no más
                // dificultad: solo se endurece si ninguno entró y la mayoría quedó por debajo.
                int below = candidates.Count(c => c.Bot < plan.Window.Lo - 1e-9);
                int above = candidates.Count(c => c.Bot > plan.Window.Hi + 1e-9);
                harder = !candidates.Any(c => c.InWindow) && below > 2 * above;
            }

            StageConfig? config = Config(plan, stage, dropMurky, harder);
            if (config is null)
            {
                continue;
            }

            int perStage = AttemptsFor(config);
            for (int a = 0; a < perStage && candidates.Count(c => c.PoolMember) < target; a++)
            {
                int attempt = firstAttempt + (stage * WalkCandidatesPerStage) + a;
                attempts++;
                long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
                Candidate? c = Build(plan, config, attempt, Prng.SubSeed(levelSeed, CandidateStream + (ulong)attempt), out string? why);
                if (Trace == plan.Level && c is null)
                {
                    Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture,
                        $"    traza {plan.Level} intento {attempt} etapa {stage} {config.Source}: sin tablero ({why}) {System.Diagnostics.Stopwatch.GetElapsedTime(t0).TotalMilliseconds:0} ms"));
                }
                if (c is null)
                {
                    rejected++;
                    if (notes.Count < 6 && why is not null && !notes.Contains(why))
                    {
                        notes.Add(why);
                    }

                    continue;
                }

                candidates.Add(c);
                if (c.InWindow)
                {
                    Measure(plan, c, full: true);
                }

                if (Trace == plan.Level)
                {
                    Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture,
                        $"    traza {plan.Level} intento {attempt} etapa {stage} {c.Source} K={config.K} E={config.Empties} chicas={config.Chicas.Length}: bot {c.Bot:0.000} {(c.InWindow ? "EN VENTANA" : "")} {c.Reject} {System.Diagnostics.Stopwatch.GetElapsedTime(t0).TotalMilliseconds:0} ms"));
                }
            }
        }

        // Sin el pool lleno, candidatos de relajación medidos (los más cercanos a la ventana que cumplen lo estructural): uno si
        // no hay ninguno en la ventana; en un reto o un hito, hasta 4 para que la escalera tenga opciones. Un reto que mide
        // menos del 75 % se publica sin etiqueta (c.2); nunca uno con una etiqueta que el slot no admite (Reject).
        int pool = candidates.Count(c => c.PoolMember);
        int wantedFallback = pool == 0 ? 1 : plan.Role is SlotRole.Challenge or SlotRole.Milestone && pool < target ? 4 : 0;
        int found = 0;
        foreach (Candidate c in candidates.Where(c => c.Reject is null && !c.Measured).OrderBy(c => plan.Window.Distance(c.Bot)).ThenBy(c => c.Attempt).Take(40))
        {
            if (found >= wantedFallback)
            {
                break;
            }

            Measure(plan, c, full: true);
            if (c.Reject is null)
            {
                found++;
            }
        }

        if (Trace == plan.Level)
        {
            int lo = candidates.Count(c => c.Bot < plan.Window.Lo - 1e-9);
            int hi = candidates.Count(c => c.Bot > plan.Window.Hi + 1e-9);
            Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"    traza {plan.Level}: {attempts} intentos, {rejected} sin tablero ({string.Join(" | ", notes)}), {lo} bajo la ventana, {hi} sobre la ventana, {candidates.Count(c => c.PoolMember)} en el pool"));
            foreach (IGrouping<string, Candidate> g in candidates.Where(c => c.Reject is not null && c.InWindow).GroupBy(c => c.Reject!.Split(' ')[0] + " " + (c.Reject!.Split(' ').Length > 1 ? c.Reject!.Split(' ')[1] : "")))
            {
                Console.Error.WriteLine($"      en ventana pero rechazados: {g.Key} x{g.Count()}");
            }
        }

        return new SlotResult(plan, candidates, attempts, notes, dropMurky, rejected);
    }

    /// <summary>Par, trampas tempranas (con el tope de su etiqueta, Q16 B) y métricas de la ruta.</summary>
    public void Measure(LevelPlan plan, Candidate c, bool full)
    {
        if (c.Measured)
        {
            return;
        }

        c.Measured = true;
        ParResult par = _metrics.Par(c.State);
        c.Expanded = par.Expanded;
        if (par.Status != SolveStatus.Solved)
        {
            c.Reject = par.Status == SolveStatus.Unsolvable ? "irresoluble" : "A* agoto 300.000";
            return;
        }

        c.Par = par.Par;
        if (c.Par < 2 * c.K)
        {
            c.Reject = string.Create(CultureInfo.InvariantCulture, $"par {c.Par} < 2K = {2 * c.K}");
            return;
        }

        if (plan.Row is RampRow row && !row.ParOk(c.Par))
        {
            // Fila fija de 02 §13 (niveles 11-12): el par [P] de la tabla única del arranque es obligatorio.
            c.Reject = string.Create(CultureInfo.InvariantCulture, $"par {c.Par} fuera de {row.ParMin}-{row.ParMax} (02 sec. 13, nivel {row.Level})");
            return;
        }

        c.EarlyTraps = _metrics.EarlyTraps(c.State);
        int cap = EarlyTrapCap(c.MeasuredLabel);
        if (c.EarlyTraps > cap)
        {
            c.Reject = string.Create(CultureInfo.InvariantCulture, $"{c.EarlyTraps} trampas tempranas (tope {cap} en {c.MeasuredLabel})");
            return;
        }

        if (full)
        {
            PathMetrics path = _metrics.Path(c.State, par.Solution);
            c.Branching = path.Branching;
            c.Traps = path.Traps;
        }
    }

    /// <summary>Tope de trampas tempranas por etiqueta (Q16 B de D-007): 0 en Normal, 1 en Difícil, sin tope en Muy difícil.</summary>
    public static int EarlyTrapCap(string label) => label switch
    {
        "normal" => 0,
        "hard" => 1,
        _ => int.MaxValue,
    };

    /// <summary>Configuración de la etapa (0 = la del plan; 1-2 = relajaciones de 03 §4.4) o null si la etapa no aplica.</summary>
    public StageConfig? Config(LevelPlan plan, int stage, bool dropMurky, bool harder = false)
    {
        var notes = new List<string>();
        List<MechanicInfo> mechs = plan.Mechanics.Where(m => !(dropMurky && m.Code == "Tu")).ToList();
        if (dropMurky)
        {
            notes.Add("sin Aguas turbias (ningun candidato midio Dificil o Muy dificil, T1)");
        }

        var chicas = plan.Capacities.Where(c => c.Id == "chica").Select(c => c.Cap).ToList();
        int alta = plan.Capacities.Where(c => c.Id == "alta").Select(c => c.Cap).DefaultIfEmpty(0).Max();
        bool liana = mechs.Any(m => m.Code == "Li");
        if (liana && (chicas.Count > 0 || alta > 0 || mechs.Any(m => m.Code == "Pi")))
        {
            // Bloqueo C1-9 de AF.Core: --validate-spec (LIANA-CAPS) rechaza la celda antes de generar; el generador nunca
            // quita en silencio una capacidad o la cueva de la celda.
            throw new InvalidOperationException($"nivel {plan.Level}: Liana con capacidades mixtas en la celda {plan.Cell.Short} (LIANA-CAPS)");
        }

        int k = plan.K;
        int empties = plan.Empties;
        string source = plan.Source;
        if (plan.Role is SlotRole.Challenge or SlotRole.Milestone && plan.Level > spec.ChallengeTwoEmptiesUntil)
        {
            source = "mixed";
        }
        int deepNodes = InverseDeep.DefaultMaxNodes;
        bool mechanicDebut = plan.DebutBoard && plan.Mechanics.Count > 0;
        bool debutLike = plan.Ramp || (plan.DebutBoard && plan.Role == SlotRole.Comfort);
        switch (stage)
        {
            case 0:
                break;
            case 1 or 2 when plan.Row is not null:
                // Fila fija de 02 §13 (niveles 11-12): K y vacíos no se relajan; si walk no da el par de la tabla, barajado en
                // profundidad corto y después el de 03 §4.2 (más profundidad, más par).
                source = "deep";
                deepNodes = stage == 1 ? RowShortDeepNodes : InverseDeep.DefaultMaxNodes;
                notes.Add(stage == 1 ? "fila de 02 sec. 13: deep corto" : "fila de 02 sec. 13: deep");
                break;
            case 1 or 2 when harder:
                // Demasiado fácil: barajado inverso en profundidad (03 §4.2), más largo en la segunda etapa.
                source = "deep";
                deepNodes = stage == 1 ? InverseDeep.DefaultMaxNodes : 60000;
                notes.Add(stage == 1 ? "endurecimiento: deep" : "endurecimiento: deep con 60.000 nodos");

                break;
            case 1:
                if (plan.Role == SlotRole.Recovery || (plan.Role is SlotRole.Comfort or SlotRole.Remanso && !debutLike && empties >= 2))
                {
                    empties++;
                    notes.Add("relajacion: un vacio mas");
                }
                else if (debutLike)
                {
                    k--;
                    notes.Add("relajacion: K - 1");
                }
                else if (plan.Role is SlotRole.Comfort or SlotRole.Remanso && plan.Cell.IsPure)
                {
                    // Un nivel puro (C, S) no lleva capacidades (d.4): se pasa directamente a 2 vacíos.
                    empties++;
                    notes.Add("relajacion: 2 vacios (03 sec. 4.4; nivel puro, sin Cuia chica)");
                }
                else if (plan.Role is SlotRole.Comfort or SlotRole.Remanso)
                {
                    if (liana || chicas.Count + (mechs.Any(m => m.Code == "Pi") ? 1 : 0) >= 2)
                    {
                        return null;
                    }

                    chicas.Add(2);
                    notes.Add("relajacion: Cuia chica de 2 como medio vacio (03 sec. 4.4)");
                }
                else if (plan.Role == SlotRole.Challenge && plan.Level <= spec.ChallengeTwoEmptiesUntil)
                {
                    source = "fill";
                    empties = 1;
                    notes.Add("relajacion: fill con 1 vacio");
                }
                else if (mechs.Any(m => m.Code == "Tu"))
                {
                    // Turbias: la etapa 0 quita el vacío del rol (solo holgura); aliviar es devolverlo, con deep.
                    source = "deep";
                    notes.Add("relajacion: turbias con el vacio del rol y la holgura");
                }
                else
                {
                    source = "fill";
                    empties = 2;
                    notes.Add("relajacion: fill con 2 vacios (03 sec. 4.4)");
                }

                break;
            default:
                if (plan.Role == SlotRole.Recovery)
                {
                    k--;
                    empties++;
                    notes.Add("relajacion: K - 1 y un vacio mas");
                }
                else if (debutLike)
                {
                    empties++;
                    notes.Add("relajacion: un vacio mas");
                }
                else if (plan.Role is SlotRole.Comfort or SlotRole.Remanso && plan.Cell.IsPure)
                {
                    empties++;
                    k--;
                    notes.Add("relajacion: 2 vacios y K - 1 (nivel puro)");
                }
                else if (plan.Role is SlotRole.Comfort or SlotRole.Remanso)
                {
                    empties++;
                    notes.Add(empties > 2 ? "relajacion: un vacio mas" : "relajacion: 2 vacios (03 sec. 4.4)");
                }
                else if (plan.Role == SlotRole.Challenge && plan.Level <= spec.ChallengeTwoEmptiesUntil)
                {
                    source = "walk";
                    notes.Add("relajacion: walk con 2 vacios");
                }
                else if (plan.Role == SlotRole.Milestone)
                {
                    source = "deep";
                    deepNodes = 60000;
                    notes.Add("relajacion: deep con 60.000 nodos");
                }
                else
                {
                    source = "walk";
                    empties = 1;
                    notes.Add("relajacion: walk con 1 vacio");
                }

                break;
        }

        // La Cuia chica «cuenta como vacío para la aritmética del nivel, pero de valor reducido» (02 §8): en los niveles con
        // Cuia chica, cada una ocupa uno de los vacíos del rol, dejando al menos un vacío de 4 [P]. La cueva de la Piraña
        // es una mecánica y no cuenta. Excepción: el estreno de una capacidad fuera de la rampa y del hito (la Cuia chica de 3
        // en el 22, p2) conserva los 2 vacíos libres de su posición, como los estrenos de 02 §9.2 regla 2.
        bool capacityDebut = plan.DebutBoard && !plan.Ramp && plan.Mechanics.Count == 0 && plan.Role is SlotRole.Recovery or SlotRole.Comfort;
        int standard = capacityDebut ? empties : Math.Max(1, empties - chicas.Count);
        // Mecánicas que ocupan un vacío (Hoja, Matapi, Lluvia): en los tableros de estreno la primera ocupa uno de los 2
        // vacíos (ejemplo de 08 §4.1); en comodidad cada una trae su vacío; en retos e hitos ocupan los vacíos del rol
        // (un vacío reservado ya da casi tanto margen como uno libre) salvo al aliviar [P].
        int occupying = mechs.Count(m => m.Code is "Ho" or "Ma" or "Ll");
        bool hardSlot = plan.Role is SlotRole.Challenge or SlotRole.Milestone;
        if (hardSlot && mechs.Any(m => m.Code == "Tu") && (stage == 0 || harder))
        {
            // Aguas turbias en un reto o un hito: la cuia de holgura pone el espacio en cuias a medias (las turbias tienen 2-3
            // dosis, T3) y el vacío del rol se quita, para que el tablero no quede más holgado que uno de 1 vacío [P].
            standard = Math.Max(0, standard - 1);
        }

        int extra = mechanicDebut ? Math.Max(0, occupying - 1)
            : (hardSlot && (stage == 0 || harder)) || (!hardSlot && harder) ? Math.Max(0, occupying - standard)
            : occupying;
        int baseEmpties = standard + extra;
        // Cuia de holgura para tener cuias a medias: el Brote y las Aguas turbias la necesitan siempre; el Barro solo fuera de
        // retos e hitos (en ellos la dosis desplazada va al vacío y el tablero queda más apretado) [P].
        bool slack = mechs.Any(m => m.Code is "Br" or "Tu") || (mechs.Any(m => m.Code == "Ba") && !hardSlot);
        int caves = mechs.Count(m => m.Code == "Pi");
        int total = k + baseEmpties + chicas.Count + (slack ? 1 : 0) + caves;
        if (total > 14 && slack)
        {
            slack = false;
            total--;
            notes.Add("sin cuia de holgura (tope de 14 cuias, 02 R1)");
        }

        if (total > 14)
        {
            return null;
        }

        return new StageConfig(stage, k, baseEmpties, source, [.. chicas], alta, slack, mechs, notes, deepNodes);
    }

    /// <summary>Un candidato: base, superposición, tintes, orden visual, validación y bot. Null si se rechaza.</summary>
    private Candidate? Build(LevelPlan plan, StageConfig config, int attempt, ulong seed, out string? why)
    {
        why = null;
        int k = config.K;
        // Capacidades del tablero base (orden de InverseSpec): Cuia alta con su fruta abundante, casas de 4, Cuias chicas,
        // vacíos y la cuia de holgura.
        var caps = new List<int>();
        var units = new int[k];
        for (int c = 0; c < k; c++)
        {
            units[c] = c == 0 && config.AltaCap > 0 ? config.AltaCap : Cuia.DefaultCapacity;
            caps.Add(units[c]);
        }

        caps.AddRange(config.Chicas);
        for (int e = 0; e < config.Empties; e++)
        {
            caps.Add(Cuia.DefaultCapacity);
        }

        if (config.Slack)
        {
            caps.Add(Cuia.DefaultCapacity);
        }

        int emptyTarget = config.Empties + config.Chicas.Length;
        BoardState? baseState;
        int depth;
        // Retos e hitos desde el 41: deep y fill alternos por intento [P] (03 §4.4 admite las dos fuentes; dan D distintos y la
        // escalera de D-009 necesita variedad: deep da par cercano a 2K y fill par cercano a 3K).
        // El Brote y las Aguas turbias necesitan cuias a medias, que fill no deja (llena las casas): con ellas, deep.
        bool needsPartial = config.Mechanics.Any(m => m.Code is "Br" or "Tu");
        string source = config.Source == "mixed" ? (attempt % 2 == 0 || needsPartial ? "deep" : "fill")
            : config.Source == "fill" && needsPartial ? "deep" : config.Source;
        try
        {
            switch (source)
            {
                case "fill":
                {
                    FillResult fill = FillSource.Generate([.. caps], units, k, seed, _fillSolver);
                    baseState = fill.State;
                    depth = fill.Candidates;
                    break;
                }

                case "deep":
                {
                    var inverse = new InverseSpec([.. caps], units, emptyTarget, -1, -1, PourMode.Run);
                    InverseResult r = _deep.Generate(inverse, seed, config.DeepNodes);
                    baseState = r.State;
                    depth = r.Depth;
                    break;
                }

                default:
                {
                    var inverse = new InverseSpec([.. caps], units, emptyTarget, -1, -1, PourMode.Run);
                    InverseResult r = _walk.Generate(inverse, seed);
                    baseState = r.State;
                    depth = r.Depth;
                    break;
                }
            }
        }
        catch (ArgumentException e)
        {
            why = "fuente: " + e.Message;
            return null;
        }

        if (baseState is null)
        {
            why = $"la fuente {source} no produjo tablero";
            return null;
        }

        GenBoard board = GenBoard.From(baseState);
        var ctx = new SuperpositionContext
        {
            Plan = plan,
            Spec = spec,
            Colors = k,
            Units = units,
            MurkyDebut = plan.DebutBoard && plan.Mechanics.Any(m => m.Code == "Tu"),
        };
        var rng = new Prng(Prng.SubSeed(seed, 1));
        foreach (string code in Superposition.Order)
        {
            foreach (MechanicInfo m in config.Mechanics.Where(m => m.Code == code))
            {
                why = Superposition.Apply(board, m, ctx, rng);
                if (why is not null)
                {
                    return null;
                }
            }
        }

        TintResult? tints = TintAssigner.Assign(spec, plan, k, ctx.FixedTints, board, new Prng(Prng.SubSeed(seed, 2)));
        if (tints is null)
        {
            why = "sin tintes que cumplan debuts y pares de riesgo (03 sec. 4.5)";
            return null;
        }

        board.MapColors(tints.Map);
        board = board.Shuffled(new Prng(Prng.SubSeed(seed, 3)));
        BoardData data = board.ToData();
        BoardBuildResult built = data.Build();
        if (!built.IsValid)
        {
            why = "BoardBuilder: " + built.Error;
            return null;
        }

        BoardState state = built.State!;
        string? invalid = LevelValidation.Validate(state);
        if (invalid is not null)
        {
            why = "LevelValidation: " + invalid;
            return null;
        }

        double bot = _metrics.BotFail(state, plan.SeedText);
        string label = spec.MeasuredLabel(plan.Level, bot);
        bool labelAllowed = CampaignSpec.LabelRank(label) <= CampaignSpec.LabelRank(plan.AllowedLabel);
        // Contrato 6: el 40 es el primer Difícil y el 60 el primer Muy difícil; antes ningún nivel mide por encima, tampoco al
        // relajar (MeasuredLabel lo publicaría con la etiqueta inferior y la escalera 58 → 60 no se notaría).
        string? early = spec.EarlyMeasure(plan.Level, bot);
        bool inWindow = plan.Window.Contains(bot) && labelAllowed && early is null;
        int realities = 0;
        string? reject = !labelAllowed ? $"etiqueta {label} no permitida en el slot" : early;
        if (data.Hidden.Length > 0)
        {
            // La ruta segura (03 §8) es cara: solo se busca para los candidatos que entran en la ventana.
            if (!inWindow)
            {
                reject ??= "Aguas turbias fuera de la ventana (sin buscar ruta segura)";
            }
            else
            {
                SafePathResult safe = _safePath.Find(state, SafeMoves.ForPipeline(PourMode.Run));
                if (!safe.IsOk)
                {
                    why = "Aguas turbias sin ruta segura: " + safe.Status;
                    return null;
                }

                realities = safe.MaxRealities;
            }
        }

        return new Candidate
        {
            Level = plan.Level,
            Attempt = attempt,
            Config = config,
            Source = source,
            Board = data,
            State = state,
            Tints = tints,
            Depth = depth,
            Bot = bot,
            MeasuredLabel = label,
            InWindow = inWindow,
            Complexity = MetricsEngine.Complexity(state),
            MaxRealities = realities,
            Reject = reject,
        };
    }
}
