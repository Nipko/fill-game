// Filtros de la tinaja (M13; D-019, Q-Ti1 A; 03 §8; propuesta de la tinaja c.3 punto 3 y c.8), con AF.Core y AF.Solver:
//   RUTA_SEGURA     SafePath desde S0 (03 §8): cada estado de la ruta tiene un vertido seguro en todas las realidades;
//   paseos seguros  20 por nivel [P], cada uno con un vertido seguro al azar hasta ganar: se rechaza el nivel si alguno llega
//                   a un estado sin vertido seguro que no sea victoria (o no termina en 400 vertidos);
//   paseos casuales unos 30 por nivel [P], con cualquier vertido útil al azar (02 §1.4, el criterio del juego): el primer
//                   estado sin vertido seguro de cada paseo se clasifica por SafeMoves.Outcome y, si hace falta, con A* en la
//                   realidad verdadera (ClassifyCasual; FIXG): si la verdadera no tiene solución, es un error del jugador;
//                   si la tiene y SafeMoves demostró que ningún vertido es seguro en todas las realidades, habría que
//                   adivinar y el nivel se rechaza; si SafeMoves agotó su presupuesto, se rechaza por «presupuesto
//                   agotado», nunca por «adivinar». Es un filtro, no una prueba;
//   S0 sin trampas  solo en los tableros de estreno: todo vertido útil de S0 es seguro (PourRules.CollectUsefulMoves frente
//                   a SafeMoves, por hash canónico); uno que no es seguro sin que ninguna realidad quede demostrada sin
//                   solución tras él (solo faltó presupuesto) se rechaza por «presupuesto agotado», no como «trampa en S0»;
//   coste de Morpho w = 2 y 40.000 nodos por realidad (hint_weight y hint_budget_nodes, 08 §9), desde cero en S0, en cada
//                   estado de la ruta y en los de los paseos (los casuales, hasta su primer estado sin vertido seguro): Morpho
//                   tiene que encontrar un vertido seguro y sus nodos, con el factor del teléfono (MorphoDeviceFactor, 8× [P]
//                   hasta PF1), caber en hint_timeout_ms (150 ms). Contar nodos es determinista.
// Las variantes de alivio repiten la ruta segura y los paseos con ReliefOptions (las de Morpho) [P], pendiente de decisión
// del tech-lead y del dueño; level-qa (P3, ReliefChecks) usa las mismas.
// Todas las búsquedas van sin poda de fútiles (SafeMoves.ForPipeline(mode, level) y SolverOptions.ForLevel; C1-8). El motivo
// del rechazo separa «sin vertido seguro» de «presupuesto agotado» (SafeMoves.Outcome; c.2) en la ruta, en los paseos seguros
// y casuales y en S0 (03 §8: «El informe separa…»; FIXG). Una instancia por hilo: el memo
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

    /// <summary>
    /// Estado en el que Morpho expandió <see cref="MorphoMaxNodes"/> (null si no se midió): con él, <c>--self-test tinaja</c>
    /// mide los nodos por milisegundo del PC (<see cref="TinajaWalks.PcNodesPerMs"/>) en las consultas que cuentan.
    /// </summary>
    public BoardState? MorphoMaxState { get; init; }
}

/// <summary>
/// Cómo acaba un paseo casual en su primer estado sin vertido seguro (03 §8; FIXG): lo decide
/// <see cref="TinajaWalks.ClassifyCasual"/> con el motivo de <see cref="SafeMoves"/> y, si hace falta, con A* en la realidad
/// verdadera.
/// </summary>
internal enum CasualStop
{
    /// <summary>
    /// Error del jugador (<see cref="TinajaCheckResult.CasualLost"/>; el paseo acaba y sigue el siguiente): la realidad
    /// verdadera no tiene solución, así que desde aquí ningún vertido puede ser seguro.
    /// </summary>
    Lost,

    /// <summary>
    /// Habría que adivinar (<see cref="TinajaWalks.KindGuess"/> y <see cref="TinajaCheckResult.CasualGuess"/>):
    /// <see cref="SafeMoves"/> demostró que ningún vertido es seguro en todas las realidades
    /// (<see cref="SafeMovesOutcome.SomeRealitySolvable"/>) y la realidad verdadera tiene solución.
    /// </summary>
    Guess,

    /// <summary>
    /// <see cref="SafeMoves"/> agotó su presupuesto por realidad antes de decidir (<see cref="SafeMovesOutcome.BudgetExceeded"/>)
    /// y la realidad verdadera tiene solución o A* tampoco la decide: <see cref="TinajaWalks.KindBudget"/>, sin
    /// <see cref="TinajaCheckResult.CasualGuess"/> (con una sola realidad, «adivinar» es imposible por definición).
    /// </summary>
    SafeMovesBudget,

    /// <summary>
    /// <see cref="SafeMoves"/> demostró que ningún vertido es seguro en todas las realidades, pero A* agota su presupuesto
    /// (<see cref="MetricsEngine.ParBudget"/>) en la realidad verdadera: <see cref="TinajaWalks.KindBudget"/>.
    /// </summary>
    TruthBudget,
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
    /// Nodos expandidos por milisegundo de Morpho (AF.Solver) en el PC del pipeline [E], redondeado a la baja. Lo mide
    /// <c>--self-test tinaja</c> (INFO «calibracion»): Morpho desde cero (w = 2, 40.000 nodos por realidad) en el estado más
    /// pesado de los filtros de los retos 329 y 332 y del hito 340 del pack de muestra, de 8.298-10.606 nodos por consulta.
    /// Medido el 2026-10-14 en el equipo de escritorio x64 del dueño (AMD Ryzen 9 9900X, Windows 11, .NET 10.0.402 Release) con
    /// otros agentes en marcha: 1.000-1.463 nodos/ms por nivel en cuatro ejecuciones (6-10 ms por consulta; medias de
    /// 1.083-1.377). 1.000 es el mínimo medido, con el equipo cargado; con más carga podría bajar. Es una medida de un PC, no
    /// del teléfono: con <see cref="MorphoDeviceFactor"/> convierte el tope de tiempo en un tope de nodos determinista, y PF1
    /// sustituye el factor por la medida en un teléfono de clase A.
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
    private readonly RealityEnumerator _realities = new();
    private readonly Pour[] _useful = new Pour[PourRules.MaxPours];
    private readonly HashSet<ulong> _gated = [];

    /// <summary>Opciones de Morpho para el nivel: w = 2 y 40.000 nodos por realidad, sin poda de fútiles con tinaja.</summary>
    public static SolverOptions MorphoOptions(BoardState level) =>
        new SolverOptions(MorphoRealityBudget, MorphoWeight, true, PourMode.Run).ForLevel(level);

    /// <summary>
    /// Opciones de la ruta segura y de los paseos en una variante de alivio con tinaja (03 §13: «las variantes repiten la ruta
    /// segura y los paseos») [P], PENDIENTE DE DECISIÓN del tech-lead y del dueño (revisión de P1; D-023 o la siguiente): las
    /// de Morpho (<see cref="MorphoOptions"/>: w = 2 y 40.000 nodos por realidad) en lugar de las del pipeline de 03 §8
    /// (<c>SafeMoves.ForPipeline</c>: w = 1 y 20.000 nodos por realidad, la fila «Aguas turbias (seguridad por realidad)» de la
    /// tabla de presupuestos de 03 §5). Motivo: un alivio tiene más espacio libre que su nivel (un vacío más o un color menos)
    /// y, sin poda de fútiles, A* óptimo agota los 20.000 nodos por realidad en tableros que se ganan; lo que cuenta en la
    /// variante es que Morpho, con lo que tiene en el teléfono, encuentre siempre un vertido seguro (un «seguro» solo sale de
    /// soluciones encontradas, así que el criterio no se relaja). Es el contrato entre el generador y level-qa: P3
    /// (<c>ReliefChecks</c>) tiene que usar exactamente estas opciones. Si el dueño lo rechaza, aquí va
    /// <c>SafeMoves.ForPipeline(PourMode.Run, relief)</c> y las variantes que agotan el presupuesto se tratan aparte (03 §13
    /// exige <c>relief</c> no vacío desde el 31).
    /// </summary>
    public static SolverOptions ReliefOptions(BoardState relief) => MorphoOptions(relief);

    /// <summary>
    /// Pasa los filtros de la tinaja sobre el tablero inicial <paramref name="s0"/> (con los colores reales en las posiciones
    /// ocultas). <paramref name="debut"/>: tablero de estreno («S0 sin trampas»). <paramref name="morphoGate"/>: medir el coste
    /// de Morpho (las variantes de alivio solo repiten la ruta y los paseos, 03 §13). Las semillas de los paseos salen de
    /// <paramref name="seedText"/>. <paramref name="relief"/>: variante de alivio, que se verifica con
    /// <see cref="ReliefOptions"/> [P] (pendiente de decisión) en lugar de con las opciones del pipeline.
    /// </summary>
    public TinajaCheckResult Check(BoardState s0, bool debut, string seedText, bool morphoGate = true, bool relief = false)
    {
        SolverOptions options = relief ? ReliefOptions(s0) : SafeMoves.ForPipeline(PourMode.Run, s0);
        SolverOptions morpho = MorphoOptions(s0);
        SafeMoves safe = _safePath.Moves;
        safe.ClearMemo();
        _gated.Clear();
        var stats = new Stats { Doses = TinajaDoses(s0) };
        string budget = BudgetText(options, relief);

        // 1. RUTA_SEGURA (03 §8).
        SafePathResult route = _safePath.Find(s0, options);
        stats.MaxRealities = route.MaxRealities;
        stats.RoutePours = route.MoveCount;
        if (!route.IsOk)
        {
            (string kind, string why) = route.Status switch
            {
                SafePathStatus.NoSafeMove => (KindOf(route.NoSafeMoveOutcome), "sin ruta segura: " + Outcome(route.NoSafeMoveOutcome, budget)),
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

        // 2. S0 sin trampas (solo en los tableros de estreno; c.3 punto 3). Una trampa es un vertido útil demostrado inseguro
        // (alguna realidad queda sin solución tras él); el que solo no se pudo demostrar seguro es «presupuesto agotado».
        if (debut)
        {
            stats.S0Traps = S0Traps(s0, options, out int unproven);
            if (stats.S0Traps > 0)
            {
                int proven = stats.S0Traps - unproven;
                string more = unproven > 0
                    ? string.Create(CultureInfo.InvariantCulture, $"; {unproven} mas sin seguridad demostrada: presupuesto agotado ({budget})")
                    : "";
                return proven > 0
                    ? stats.Result(KindS0Trap, string.Create(CultureInfo.InvariantCulture,
                        $"tinaja: {proven} vertido(s) util(es) de S0 no son seguros (estreno sin trampas){more}"))
                    : stats.Result(KindBudget, string.Create(CultureInfo.InvariantCulture,
                        $"tinaja: {unproven} vertido(s) util(es) de S0 sin seguridad demostrada (estreno sin trampas): presupuesto agotado ({budget})"));
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
                        $"tinaja: el paseo seguro {w + 1} llega a un estado sin vertido seguro tras {step} vertidos ({Outcome(safe.Outcome, budget)})"));
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
                    // Primer estado sin vertido seguro: se clasifica por el motivo de SafeMoves y, si hace falta, por si la
                    // realidad verdadera aún tiene solución (ClassifyCasual; FIXG).
                    SolveStatus? verdict = CasualNeedsTruth(safe.Outcome) ? _solver.Solve(s, truth).Status : null;
                    switch (ClassifyCasual(safe.Outcome, verdict))
                    {
                        case CasualStop.Lost:
                            stats.CasualLost++;
                            break;
                        case CasualStop.Guess:
                            stats.CasualGuess++;
                            return stats.Result(KindGuess, string.Create(CultureInfo.InvariantCulture,
                                $"tinaja: el paseo casual {w + 1} llega tras {step} vertidos a un estado sin vertido seguro con la realidad verdadera resoluble (habria que adivinar)"));
                        case CasualStop.SafeMovesBudget:
                            return stats.Result(KindBudget, string.Create(CultureInfo.InvariantCulture,
                                $"tinaja: el paseo casual {w + 1} llega tras {step} vertidos a un estado sin vertido seguro demostrado: presupuesto agotado ({budget})"));
                        default: // CasualStop.TruthBudget
                            return stats.Result(KindBudget, string.Create(CultureInfo.InvariantCulture,
                                $"tinaja: el paseo casual {w + 1} llega tras {step} vertidos a un estado sin vertido seguro y A* agota {MetricsEngine.ParBudget} nodos en la realidad verdadera"));
                    }

                    break; // error del jugador: el paseo acaba aquí
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
    public int S0Traps(BoardState s0, SolverOptions options) => S0Traps(s0, options, out _);

    /// <summary>
    /// Como <see cref="S0Traps(BoardState, SolverOptions)"/>; <paramref name="unproven"/> dice cuántos de esos vertidos no son
    /// trampas demostradas (FIXG): tras ellos, A* con <paramref name="options"/> no deja ninguna realidad de S0 demostrada sin
    /// solución, así que solo faltó presupuesto para demostrarlos seguros («presupuesto agotado», no «trampa en S0»).
    /// </summary>
    public int S0Traps(BoardState s0, SolverOptions options, out int unproven)
    {
        unproven = 0;
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
        bool enumerated = false;
        for (int m = 0; m < useful; m++)
        {
            if (!safeChildren.Contains(PourRules.Apply(s0, _useful[m].Source, _useful[m].Target, PourMode.Run).CanonicalHash()))
            {
                traps++;
                if (!enumerated)
                {
                    // Las mismas realidades que SafeMoves.Find(s0, options, s0): T4 con S0 de referencia y el tope de 24.
                    _realities.Enumerate(s0, s0);
                    enumerated = true;
                }

                if (!ProvenUnsafe(_useful[m], options))
                {
                    unproven++;
                }
            }
        }

        return traps;
    }

    /// <summary>
    /// ¿Qué hace un paseo casual en su primer estado sin vertido seguro? (03 §8: «si el primer estado sin vertido seguro es
    /// resoluble en la realidad verdadera, habría que adivinar, y el nivel se rechaza»; «El informe separa «sin vertido
    /// seguro» de «presupuesto agotado»»; FIXG). Pura: <paramref name="outcome"/> es el motivo de <see cref="SafeMoves"/>
    /// (<see cref="SafeMoves.Outcome"/>) y <paramref name="truth"/>, el de A* en la realidad verdadera, que solo se mira
    /// cuando <see cref="CasualNeedsTruth"/> lo pide (null si no se resolvió).
    /// <list type="bullet">
    /// <item><description><see cref="SafeMovesOutcome.NoExit"/>: ninguna realidad tiene solución, tampoco la verdadera
    /// (demostrado): <see cref="CasualStop.Lost"/>.</description></item>
    /// <item><description><see cref="SafeMovesOutcome.SomeRealitySolvable"/>: ningún vertido es seguro en todas (demostrado);
    /// con la verdadera resoluble, <see cref="CasualStop.Guess"/>; sin solución, <see cref="CasualStop.Lost"/>; si A* agota
    /// su presupuesto, <see cref="CasualStop.TruthBudget"/>.</description></item>
    /// <item><description><see cref="SafeMovesOutcome.BudgetExceeded"/>: «sin vertido seguro» no está demostrado, así que nunca
    /// es «adivinar»: <see cref="CasualStop.SafeMovesBudget"/>, salvo que A* demuestre que la verdadera no tiene solución;
    /// entonces ningún vertido puede ser seguro (lo sería también en la verdadera) y es un error del jugador,
    /// <see cref="CasualStop.Lost"/>, como antes de FIXG: rechazar ahí sería una regla que 03 §8 no pone.</description></item>
    /// </list>
    /// </summary>
    /// <exception cref="ArgumentException">Falta <paramref name="truth"/> cuando <see cref="CasualNeedsTruth"/> lo pide.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outcome"/> no es un motivo de «ningún vertido seguro»
    /// (<see cref="SafeMovesOutcome.Found"/>, <see cref="SafeMovesOutcome.Won"/> o
    /// <see cref="SafeMovesOutcome.RealitiesRejected"/>, que el paseo filtra antes).</exception>
    public static CasualStop ClassifyCasual(SafeMovesOutcome outcome, SolveStatus? truth)
    {
        if (outcome == SafeMovesOutcome.NoExit)
        {
            return CasualStop.Lost;
        }

        if (!CasualNeedsTruth(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "No es un motivo de «ningun vertido seguro».");
        }

        if (truth is not SolveStatus status)
        {
            throw new ArgumentException("Falta el resultado de A* en la realidad verdadera para " + outcome + ".", nameof(truth));
        }

        if (status == SolveStatus.Unsolvable)
        {
            return CasualStop.Lost;
        }

        if (outcome == SafeMovesOutcome.BudgetExceeded)
        {
            return CasualStop.SafeMovesBudget;
        }

        return status == SolveStatus.Solved ? CasualStop.Guess : CasualStop.TruthBudget;
    }

    /// <summary>
    /// ¿Hace falta A* en la realidad verdadera para clasificar el primer estado sin vertido seguro de un paseo casual
    /// (<see cref="ClassifyCasual"/>)? Solo si <see cref="SafeMoves"/> no lo decidió ya (<see cref="SafeMovesOutcome.NoExit"/>).
    /// </summary>
    public static bool CasualNeedsTruth(SafeMovesOutcome outcome) =>
        outcome is SafeMovesOutcome.SomeRealitySolvable or SafeMovesOutcome.BudgetExceeded;

    /// <summary>Causa del rechazo de una clasificación de <see cref="ClassifyCasual"/> (null si es un error del jugador).</summary>
    public static string? KindOf(CasualStop stop) => stop switch
    {
        CasualStop.Guess => KindGuess,
        CasualStop.SafeMovesBudget or CasualStop.TruthBudget => KindBudget,
        _ => null,
    };

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
        if (nodes > stats.MorphoMaxNodes)
        {
            stats.MorphoMaxNodes = nodes;
            stats.MorphoMaxState = state;
        }

        if (status != RealityStatus.Ok || _morpho.Outcome != SafeMovesOutcome.Found)
        {
            return "tinaja: Morpho (w = 2, 40.000 nodos por realidad) no da vertido seguro: " + (status == RealityStatus.Ok ? Outcome(_morpho.Outcome) : status.ToString());
        }

        return nodes > MorphoNodeCap
            ? string.Create(CultureInfo.InvariantCulture,
                $"tinaja: Morpho expande {nodes} nodos en un estado; con el factor {MorphoDeviceFactor}x del telefono pasa de {HintTimeoutMs} ms (tope {MorphoNodeCap})")
            : null;
    }

    /// <summary>
    /// ¿Hay una realidad de S0 (las de <see cref="_realities"/>) que A* con <paramref name="options"/> demuestra sin solución
    /// tras <paramref name="pour"/>? Lo mismo que mira <see cref="SafeMoves"/> en cada realidad, para separar la trampa del
    /// presupuesto agotado (FIXG).
    /// </summary>
    private bool ProvenUnsafe(Pour pour, SolverOptions options)
    {
        if (_realities.Status != RealityStatus.Ok)
        {
            return false;
        }

        for (int r = 0; r < _realities.Count; r++)
        {
            BoardState child = PourRules.Apply(_realities.GetReality(r), pour.Source, pour.Target, PourMode.Run);
            if (!child.IsWon() && _solver.Solve(child, options).Status == SolveStatus.Unsolvable)
            {
                return true;
            }
        }

        return false;
    }

    private static string KindOf(SafeMovesOutcome outcome) => outcome == SafeMovesOutcome.BudgetExceeded ? KindBudget : KindNoSafeMove;

    /// <summary>
    /// Presupuesto por realidad de <see cref="SafeMoves"/> en el motivo de un rechazo por «presupuesto agotado»: el del
    /// pipeline (03 §8: 20.000) o, en una variante de alivio, el de <see cref="ReliefOptions"/>.
    /// </summary>
    private static string BudgetText(SolverOptions options, bool relief)
    {
        int n = options.NodeBudget;
        string nodes = n >= 1000
            ? string.Create(CultureInfo.InvariantCulture, $"{n / 1000}.{n % 1000:000}")
            : n.ToString(CultureInfo.InvariantCulture);
        return nodes + " nodos por realidad de " + (relief ? "ReliefOptions" : "SafeMoves");
    }

    /// <summary>Motivo de <see cref="SafeMoves"/> en un rechazo; <paramref name="budget"/>, el presupuesto que se agotó (si no va ya en el motivo).</summary>
    private static string Outcome(SafeMovesOutcome outcome, string? budget = null) => outcome switch
    {
        SafeMovesOutcome.NoExit => "ninguna realidad tiene solucion",
        SafeMovesOutcome.SomeRealitySolvable => "alguna realidad tiene solucion pero ningun vertido es seguro en todas",
        SafeMovesOutcome.BudgetExceeded => budget is null ? "presupuesto agotado" : "presupuesto agotado: " + budget,
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
        public BoardState? MorphoMaxState;

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
            MorphoMaxState = MorphoMaxState,
        };
    }
}
