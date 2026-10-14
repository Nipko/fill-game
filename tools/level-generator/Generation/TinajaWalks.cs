// Filtros de la tinaja (M13; D-019, Q-Ti1 A; 03 §8; propuesta de la tinaja c.3 punto 3 y c.8), con AF.Core y AF.Solver:
//   RUTA_SEGURA     SafePath desde S0 (03 §8): cada estado de la ruta tiene un vertido seguro en todas las realidades;
//   paseos seguros  20 por nivel [P], cada uno con un vertido seguro al azar hasta ganar: se rechaza el nivel si alguno llega
//                   a un estado sin vertido seguro que no sea victoria (o no termina en 400 vertidos);
//   paseos casuales unos 30 por nivel [P], con cualquier vertido útil al azar (02 §1.4, el criterio del juego): el primer
//                   estado sin vertido seguro de cada paseo se clasifica con A* en la realidad verdadera; si es resoluble,
//                   habría que adivinar y el nivel se rechaza; si no, es un error del jugador. Es un filtro, no una prueba;
//   S0 sin trampas  solo en los tableros de estreno: todo vertido útil de S0 es seguro (PourRules.CollectUsefulMoves frente
//                   a SafeMoves, por hash canónico);
//   coste de Morpho w = 2 y 40.000 nodos por realidad (hint_weight y hint_budget_nodes, 08 §9), desde cero en S0, en cada
//                   estado de la ruta y en los de los paseos (los casuales, hasta su primer estado sin vertido seguro): Morpho
//                   tiene que encontrar un vertido seguro y sus nodos, con el factor del teléfono (MorphoDeviceFactor, 8× [P]
//                   hasta PF1), caber en hint_timeout_ms (150 ms). Contar nodos es determinista.
// Todas las búsquedas van sin poda de fútiles (SafeMoves.ForPipeline(mode, level) y SolverOptions.ForLevel; C1-8). El motivo
// del rechazo separa «sin vertido seguro» de «presupuesto agotado» (SafeMoves.Outcome; c.2). Una instancia por hilo: el memo
// de SafeMoves se vacía al empezar cada tablero, así que el resultado no depende de qué tableros midió antes el hilo.
using System.Globalization;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Rules;
using AF.Solver.Murky;
using AF.Solver.Search;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

/// <summary>Resultado de <see cref="TinajaWalks.Check"/> (para el candidato y el informe).</summary>
internal sealed record TinajaCheckResult
{
    /// <summary>Motivo del rechazo (null si el tablero pasa todos los filtros).</summary>
    public string? Reject { get; init; }

    /// <summary>Causa del rechazo para el informe: una de las constantes de <see cref="TinajaWalks"/> (Kind*).</summary>
    public string? Kind { get; init; }

    public int Doses { get; init; }
    public int MaxRealities { get; init; }
    public int RoutePours { get; init; }
    public int SafeWalks { get; init; }
    public int SafeWalksWon { get; init; }
    public int CasualWalks { get; init; }
    public int CasualWon { get; init; }
    public int CasualLost { get; init; }
    public int CasualGuess { get; init; }
    public int CasualOpen { get; init; }

    /// <summary>Vertidos útiles de S0 que no son seguros (−1 si no se comprobó: solo en los tableros de estreno).</summary>
    public int S0Traps { get; init; } = -1;

    /// <summary>Estados distintos en los que se midió Morpho (0 si no se midió).</summary>
    public int MorphoStates { get; init; }

    /// <summary>Máximo de nodos expandidos por Morpho en un estado, desde cero.</summary>
    public long MorphoMaxNodes { get; init; }
}

internal sealed class TinajaWalks
{
    /// <summary>Paseos seguros por nivel (propuesta de la tinaja c.3 punto 3; 03 §8) [P].</summary>
    public const int SafeWalkCount = 20;

    /// <summary>Paseos casuales por nivel («unos 30»; propuesta de la tinaja c.3 punto 3; 03 §8) [P].</summary>
    public const int CasualWalkCount = 30;

    /// <summary>Vertidos máximos de un paseo (como la ruta segura, 03 §8: «hasta 400»).</summary>
    public const int MaxSteps = SafePath.MaxSteps;

    /// <summary><c>hint_budget_nodes</c> por defecto (08 §9): presupuesto de Morpho por realidad.</summary>
    public const int MorphoRealityBudget = 40000;

    /// <summary><c>hint_weight</c> por defecto (08 §9): A* ponderado de Morpho.</summary>
    public const int MorphoWeight = 2;

    /// <summary><c>hint_timeout_ms</c> por defecto (08 §9): tiempo que tiene Morpho en el teléfono.</summary>
    public const int HintTimeoutMs = 150;

    /// <summary>
    /// Factor del teléfono de clase A sobre este PC [P]: 8×, el extremo conservador de la estimación de 4-8× de la propuesta
    /// de la tinaja (c.3 punto 3). Se sustituye por la medida de PF1 (pregunta 13; 08 §5.4 y §14.1).
    /// </summary>
    public const int MorphoDeviceFactor = 8;

    /// <summary>
    /// Nodos expandidos por milisegundo de AF.Solver en el PC del pipeline [E]: medido con <c>--self-test tinaja</c> (consultas
    /// de Morpho desde cero, w = 2, sobre los tableros de muestra; .NET 10 Release, equipo de escritorio x64 del dueño) y
    /// redondeado a la baja. Con <see cref="MorphoDeviceFactor"/> convierte el tope de tiempo en un tope de nodos determinista.
    /// </summary>
    public const int PcNodesPerMs = 1000;

    /// <summary>Tope de nodos de una consulta de Morpho: 150 ms × nodos por ms del PC ÷ factor del teléfono.</summary>
    public const long MorphoNodeCap = (long)HintTimeoutMs * PcNodesPerMs / MorphoDeviceFactor;

    public const string KindNoSafeMove = "sin vertido seguro";
    public const string KindBudget = "presupuesto agotado";
    public const string KindRealities = "realidades";
    public const string KindWalkOpen = "paseo seguro sin terminar";
    public const string KindGuess = "adivinar";
    public const string KindS0Trap = "trampa en S0";
    public const string KindMorpho = "coste de Morpho";
    public const string KindStuck = "R14";

    private const ulong SafeWalkStream = 0x54494E00UL;
    private const ulong CasualWalkStream = 0x54494E80UL;

    private readonly SafePath _safePath = new();
    private readonly SafeMoves _morpho = new();
    private readonly AStarSolver _solver = new();
    private readonly Pour[] _useful = new Pour[PourRules.MaxPours];
    private readonly HashSet<ulong> _gated = [];

    /// <summary>Opciones de Morpho para el nivel: w = 2 y 40.000 nodos por realidad, sin poda de fútiles con tinaja.</summary>
    public static SolverOptions MorphoOptions(BoardState level) =>
        new SolverOptions(MorphoRealityBudget, MorphoWeight, true, PourMode.Run).ForLevel(level);

    /// <summary>
    /// Pasa los filtros de la tinaja sobre el tablero inicial <paramref name="s0"/> (con los colores reales en las posiciones
    /// ocultas). <paramref name="debut"/>: tablero de estreno («S0 sin trampas»). <paramref name="morphoGate"/>: medir el coste
    /// de Morpho (las variantes de alivio solo repiten la ruta y los paseos, 03 §13). Las semillas de los paseos salen de
    /// <paramref name="seedText"/>. <paramref name="relief"/>: variante de alivio, que se verifica con las opciones de Morpho
    /// (<see cref="MorphoOptions"/>) en lugar de las del pipeline [P]: un alivio tiene más espacio libre que su nivel (un vacío
    /// más o un color menos) y, sin poda de fútiles, A* óptimo agota los 20.000 nodos por realidad de 03 §8 en tableros que se
    /// ganan; lo que cuenta en la variante es que Morpho, con lo que tiene en el teléfono, encuentre siempre un vertido seguro.
    /// </summary>
    public TinajaCheckResult Check(BoardState s0, bool debut, string seedText, bool morphoGate = true, bool relief = false)
    {
        SolverOptions options = relief ? MorphoOptions(s0) : SafeMoves.ForPipeline(PourMode.Run, s0);
        SolverOptions morpho = MorphoOptions(s0);
        SafeMoves safe = _safePath.Moves;
        safe.ClearMemo();
        _gated.Clear();
        var stats = new Stats { Doses = TinajaDoses(s0) };

        // 1. RUTA_SEGURA (03 §8).
        SafePathResult route = _safePath.Find(s0, options);
        stats.MaxRealities = route.MaxRealities;
        stats.RoutePours = route.MoveCount;
        if (!route.IsOk)
        {
            (string kind, string why) = route.Status switch
            {
                SafePathStatus.NoSafeMove => (KindOf(route.NoSafeMoveOutcome), "sin ruta segura: " + Outcome(route.NoSafeMoveOutcome)),
                SafePathStatus.TooManyRealities => (KindRealities, "sin ruta segura: mas de 24 realidades"),
                _ => (KindNoSafeMove, "sin ruta segura: " + route.Status),
            };
            return stats.Result(kind, "tinaja " + why);
        }

        if (morphoGate)
        {
            BoardState s = s0;
            for (int i = 0; i <= route.MoveCount; i++)
            {
                if (Gate(s, s0, morpho, stats) is string why)
                {
                    return stats.Result(KindMorpho, why + string.Create(CultureInfo.InvariantCulture, $" (ruta, paso {i})"));
                }

                if (i < route.MoveCount)
                {
                    Pour p = route.GetMove(i);
                    s = PourRules.Apply(s, p.Source, p.Target, PourMode.Run);
                }
            }
        }

        // 2. S0 sin trampas (solo en los tableros de estreno; c.3 punto 3).
        if (debut)
        {
            stats.S0Traps = S0Traps(s0, options);
            if (stats.S0Traps > 0)
            {
                return stats.Result(KindS0Trap, string.Create(CultureInfo.InvariantCulture, $"tinaja: {stats.S0Traps} vertido(s) util(es) de S0 no son seguros (estreno sin trampas)"));
            }
        }

        // 3. Paseos seguros.
        ulong seed = Prng.SeedFrom(seedText);
        for (int w = 0; w < SafeWalkCount; w++)
        {
            var rng = new Prng(Prng.SubSeed(seed, SafeWalkStream + (ulong)w));
            BoardState s = s0;
            stats.SafeWalks++;
            for (int step = 0; ; step++)
            {
                if (s.IsWon())
                {
                    stats.SafeWalksWon++;
                    break;
                }

                if (step == MaxSteps)
                {
                    return stats.Result(KindWalkOpen, string.Create(CultureInfo.InvariantCulture, $"tinaja: el paseo seguro {w + 1} no gana en {MaxSteps} vertidos"));
                }

                if (Find(safe, s, options, s0, stats) is string bad)
                {
                    return stats.Result(KindRealities, bad);
                }

                if (safe.Count == 0)
                {
                    return stats.Result(KindOf(safe.Outcome), string.Create(CultureInfo.InvariantCulture,
                        $"tinaja: el paseo seguro {w + 1} llega a un estado sin vertido seguro tras {step} vertidos ({Outcome(safe.Outcome)})"));
                }

                Pour pour = safe.GetMove(rng.NextInt(safe.Count));
                if (morphoGate && Gate(s, s0, morpho, stats) is string why)
                {
                    return stats.Result(KindMorpho, why + string.Create(CultureInfo.InvariantCulture, $" (paseo seguro {w + 1}, paso {step})"));
                }

                s = PourRules.Apply(s, pour.Source, pour.Target, PourMode.Run);
            }
        }

        // 4. Paseos casuales.
        SolverOptions truth = new SolverOptions(MetricsEngine.ParBudget, 1, true, PourMode.Run).ForLevel(s0);
        for (int w = 0; w < CasualWalkCount; w++)
        {
            var rng = new Prng(Prng.SubSeed(seed, CasualWalkStream + (ulong)w));
            BoardState s = s0;
            stats.CasualWalks++;
            for (int step = 0; ; step++)
            {
                if (s.IsWon())
                {
                    stats.CasualWon++;
                    break;
                }

                if (step == MaxSteps)
                {
                    stats.CasualOpen++; // vaivén sin fin: el paseo no decide nada
                    break;
                }

                if (Find(safe, s, options, s0, stats) is string bad)
                {
                    return stats.Result(KindRealities, bad);
                }

                if (safe.Count == 0)
                {
                    // Primer estado sin vertido seguro: ¿la realidad verdadera aún tiene solución?
                    SolveStatus status = _solver.Solve(s, truth).Status;
                    if (status == SolveStatus.Unsolvable)
                    {
                        stats.CasualLost++;
                        break;
                    }

                    stats.CasualGuess += status == SolveStatus.Solved ? 1 : 0;
                    return status == SolveStatus.Solved
                        ? stats.Result(KindGuess, string.Create(CultureInfo.InvariantCulture,
                            $"tinaja: el paseo casual {w + 1} llega tras {step} vertidos a un estado sin vertido seguro con la realidad verdadera resoluble (habria que adivinar)"))
                        : stats.Result(KindBudget, string.Create(CultureInfo.InvariantCulture,
                            $"tinaja: el paseo casual {w + 1} llega tras {step} vertidos a un estado sin vertido seguro y A* agota {MetricsEngine.ParBudget} nodos en la realidad verdadera"));
                }

                if (morphoGate && Gate(s, s0, morpho, stats) is string why)
                {
                    return stats.Result(KindMorpho, why + string.Create(CultureInfo.InvariantCulture, $" (paseo casual {w + 1}, paso {step})"));
                }

                int useful = PourRules.CollectUsefulMoves(s, true, PourMode.Run, _useful);
                if (useful == 0)
                {
                    // R14: con un vertido seguro siempre hay alguno útil (las exenciones de futil de la tinaja y su cuia).
                    return stats.Result(KindStuck, string.Create(CultureInfo.InvariantCulture,
                        $"tinaja: el paseo casual {w + 1} queda sin vertidos utiles con un vertido seguro (R14)"));
                }

                Pour pour = _useful[rng.NextInt(useful)];
                s = PourRules.Apply(s, pour.Source, pour.Target, PourMode.Run);
            }
        }

        return stats.Result(null, null);
    }

    /// <summary>Vertidos útiles de S0 (02 §1.4) cuyo tablero resultante no es el de ningún vertido seguro (por hash canónico).</summary>
    public int S0Traps(BoardState s0, SolverOptions options)
    {
        SafeMoves safe = _safePath.Moves;
        if (safe.Find(s0, options, s0) != RealityStatus.Ok)
        {
            return int.MaxValue;
        }

        var safeChildren = new HashSet<ulong>();
        for (int k = 0; k < safe.Count; k++)
        {
            Pour p = safe.GetMove(k);
            safeChildren.Add(PourRules.Apply(s0, p.Source, p.Target, PourMode.Run).CanonicalHash());
        }

        int useful = PourRules.CollectUsefulMoves(s0, true, PourMode.Run, _useful);
        int traps = 0;
        for (int m = 0; m < useful; m++)
        {
            if (!safeChildren.Contains(PourRules.Apply(s0, _useful[m].Source, _useful[m].Target, PourMode.Run).CanonicalHash()))
            {
                traps++;
            }
        }

        return traps;
    }

    /// <summary>Consulta de Morpho desde cero en <paramref name="state"/>: nodos expandidos y resultado (para el self-test).</summary>
    public (SafeMovesOutcome Outcome, long Nodes) MorphoQuery(BoardState state, BoardState s0)
    {
        _morpho.ClearMemo();
        RealityStatus status = _morpho.Find(state, MorphoOptions(s0), s0);
        return (status == RealityStatus.Ok ? _morpho.Outcome : SafeMovesOutcome.RealitiesRejected, _morpho.Expanded);
    }

    /// <summary>Dosis de la tinaja en <paramref name="state"/> (0 si no la lleva).</summary>
    public static int TinajaDoses(BoardState state)
    {
        for (int i = 0; i < state.Count; i++)
        {
            if (state[i].Mechanic == AF.Core.Mechanics.MechanicType.Tinaja)
            {
                return state[i].Count;
            }
        }

        return 0;
    }

    private static string? Find(SafeMoves safe, BoardState s, SolverOptions options, BoardState s0, Stats stats)
    {
        RealityStatus status = safe.Find(s, options, s0);
        if (status != RealityStatus.Ok)
        {
            return "tinaja: realidades rechazadas en un paseo (" + status + ")";
        }

        stats.MaxRealities = Math.Max(stats.MaxRealities, safe.RealityCount);
        return null;
    }

    /// <summary>Compuerta de coste de Morpho en un estado (una vez por estado canónico): null si cabe.</summary>
    private string? Gate(BoardState state, BoardState s0, SolverOptions morpho, Stats stats)
    {
        if (state.IsWon() || !_gated.Add(state.CanonicalHash()))
        {
            return null;
        }

        _morpho.ClearMemo();
        RealityStatus status = _morpho.Find(state, morpho, s0);
        long nodes = _morpho.Expanded;
        stats.MorphoStates++;
        stats.MorphoMaxNodes = Math.Max(stats.MorphoMaxNodes, nodes);
        if (status != RealityStatus.Ok || _morpho.Outcome != SafeMovesOutcome.Found)
        {
            return "tinaja: Morpho (w = 2, 40.000 nodos por realidad) no da vertido seguro: " + (status == RealityStatus.Ok ? Outcome(_morpho.Outcome) : status.ToString());
        }

        return nodes > MorphoNodeCap
            ? string.Create(CultureInfo.InvariantCulture,
                $"tinaja: Morpho expande {nodes} nodos en un estado; con el factor {MorphoDeviceFactor}x del telefono pasa de {HintTimeoutMs} ms (tope {MorphoNodeCap})")
            : null;
    }

    private static string KindOf(SafeMovesOutcome outcome) => outcome == SafeMovesOutcome.BudgetExceeded ? KindBudget : KindNoSafeMove;

    private static string Outcome(SafeMovesOutcome outcome) => outcome switch
    {
        SafeMovesOutcome.NoExit => "ninguna realidad tiene solucion",
        SafeMovesOutcome.SomeRealitySolvable => "alguna realidad tiene solucion pero ningun vertido es seguro en todas",
        SafeMovesOutcome.BudgetExceeded => "presupuesto agotado",
        _ => outcome.ToString(),
    };

    private sealed class Stats
    {
        public int Doses;
        public int MaxRealities;
        public int RoutePours;
        public int SafeWalks;
        public int SafeWalksWon;
        public int CasualWalks;
        public int CasualWon;
        public int CasualLost;
        public int CasualGuess;
        public int CasualOpen;
        public int S0Traps = -1;
        public int MorphoStates;
        public long MorphoMaxNodes;

        public TinajaCheckResult Result(string? kind, string? reject) => new()
        {
            Reject = reject,
            Kind = kind,
            Doses = Doses,
            MaxRealities = MaxRealities,
            RoutePours = RoutePours,
            SafeWalks = SafeWalks,
            SafeWalksWon = SafeWalksWon,
            CasualWalks = CasualWalks,
            CasualWon = CasualWon,
            CasualLost = CasualLost,
            CasualGuess = CasualGuess,
            CasualOpen = CasualOpen,
            S0Traps = S0Traps,
            MorphoStates = MorphoStates,
            MorphoMaxNodes = MorphoMaxNodes,
        };
    }
}
