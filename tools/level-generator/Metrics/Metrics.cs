// Métricas de un nivel (GDD 03 §7) calculadas con AF.Core y AF.Solver, sin reimplementar reglas:
//   par            = longitud de la solución de A* (w = 1, 300.000 expandidos, run; 03 §5);
//   branching      = media de |MOVIMIENTOS(S)| (03 §5 literal: legales y no fútiles de AF.Core, un vacío por capacidad y
//                    mecánica y por origen) a lo largo de la ruta óptima [P: |SolverMoves| funde además cuias idénticas
//                    y no coincide con el prototipo en 14 de 40 niveles; ver tools/out/parity_report.md];
//   traps          = fracción de los pares (estado de la ruta óptima, movimiento) que llevan a un estado sin solución
//                    (A* agotado con 3.000 expandidos o atascado al instante), con 120 pares como máximo muestreados
//                    por el índice entero floor(k·n/120) [P];
//   early_traps    = AF.Solver.EarlyTraps (Q16 B de D-007);
//   complexity     = AF.Solver.Generation.Complexity (filtro, no entra en D);
//   bot_fail       = AF.Solver.Bot.CasualBot ×40 con la semilla del nivel (03 §6).
// D = 0,35·z(bot_fail) + 0,25·z(par − 2K) + 0,20·z(branching) + 0,10·z(traps) + 0,10·[E == 1] con los z-scores del pool
// del rango (03 §7). Una instancia por hilo: reutiliza el solver, el bot y los búferes.
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Bot;
using AF.Solver.Generation;
using AF.Solver.Search;

namespace AmazonFill.Tools.LevelGenerator.Metrics;

/// <summary>Resultado de A* sobre el tablero base.</summary>
internal sealed record ParResult(SolveStatus Status, int Par, int Expanded, Pour[] Solution);

/// <summary>Métricas de la ruta óptima (03 §7).</summary>
/// <remarks>
/// <see cref="Branching"/> cuenta los <c>MOVIMIENTOS</c> literales de 03 §5 (un vacío por capacidad y mecánica y por origen),
/// que es la definición de 03 §7 y la del prototipo; <see cref="SolverMovesBranching"/> cuenta <c>|SolverMoves|</c>, que además
/// funde orígenes y destinos idénticos (solo para el informe de paridad).
/// </remarks>
internal sealed record PathMetrics(double Branching, double Traps, int TrapPairs, int TrapUnknown, double SolverMovesBranching = 0);

internal sealed class MetricsEngine
{
    /// <summary>Presupuesto de A* para el par de campaña (03 §5).</summary>
    public const int ParBudget = SolverOptions.PipelineNodeBudget;

    /// <summary>Presupuesto de A* por par en las trampas (03 §7; prototipo: 3.000).</summary>
    public const int TrapBudget = 3000;

    /// <summary>Pares (estado, vertido) como máximo en las trampas (03 §7).</summary>
    public const int TrapMaxPairs = 120;

    private readonly AStarSolver _solver = new();
    private readonly AStarSolver _trapSolver = new();
    private readonly CasualBot _bot = new();
    private readonly EarlyTraps _early = new();
    private readonly SolverMove[] _moves = new SolverMove[SolverMoves.MaxMoves];
    private readonly int[] _units = new int[CuiaRules.UnitsTableLength];

    public ParResult Par(BoardState state, int budget = ParBudget)
    {
        SolveResult r = _solver.Solve(state, new SolverOptions(budget, 1, true, PourMode.Run));
        return new ParResult(r.Status, r.IsSolved ? r.MoveCount : -1, r.Expanded, r.CopyMoves());
    }

    public double BotFail(BoardState state, string seedText) =>
        _bot.FailRate(state, CasualBot.SeedFor(seedText), BotOptions.ForPipeline(PourMode.Run)).Rate;

    public int EarlyTraps(BoardState state) => _early.Count(state, PourMode.Run);

    public static int Complexity(BoardState state) => AF.Solver.Generation.Complexity.Of(state);

    /// <summary>
    /// Ramificación y trampas sobre los estados de la ruta óptima (03 §7). La ruta es la de
    /// <see cref="ReferencePath"/> (el orden de expansión del prototipo, 03 §18) si tiene la longitud del par de
    /// AF.Solver; si no, la solución de AF.Solver. La ramificación cuenta <c>|SolverMoves|</c> en cada estado; los pares de
    /// las trampas son los <c>MOVIMIENTOS</c> literales de 03 §5 en el orden real del tablero.
    /// </summary>
    public PathMetrics Path(BoardState start, Pour[] solution)
    {
        if (solution.Length == 0)
        {
            return new PathMetrics(0, 0, 0, 0, 0);
        }

        Pour[]? reference = ReferencePath.Find(start, ParBudget, out _);
        if (reference is not null && reference.Length == solution.Length)
        {
            solution = reference;
        }

        int width = start.Count;
        LoadUnits(start);
        var states = new List<Cuia[]>(solution.Length);
        var pairs = new List<(int State, int Source, int Target)>();
        long branchSum = 0;
        BoardState s = start;
        var real = new Cuia[width];
        long solverMovesSum = 0;
        for (int i = 0; i < solution.Length; i++)
        {
            Cuia[] canon = Canonical(s);
            solverMovesSum += SolverMoves.Collect(canon, 0, width, _units, PourMode.Run, true, _moves);
            for (int k = 0; k < width; k++)
            {
                real[k] = s[k];
            }

            states.Add((Cuia[])real.Clone());
            foreach ((int src, int tgt) in ReferencePath.Moves(real, width, _units))
            {
                pairs.Add((i, src, tgt));
                branchSum++;
            }

            Pour p = solution[i];
            s = PourRules.Apply(s, p.Source, p.Target, PourMode.Run);
        }

        double branching = (double)branchSum / solution.Length;
        int n = pairs.Count;
        var sample = new List<(int State, int Source, int Target)>();
        if (n > TrapMaxPairs)
        {
            for (int k = 0; k < TrapMaxPairs; k++)
            {
                sample.Add(pairs[(int)((long)k * n / TrapMaxPairs)]);
            }
        }
        else
        {
            sample.AddRange(pairs);
        }

        var memo = new Dictionary<ulong, SolveStatus>();
        int traps = 0;
        int unknown = 0;
        var child = new Cuia[width];
        var sorted = new Cuia[width];
        var trapOptions = new SolverOptions(TrapBudget, 1, true, PourMode.Run);
        foreach ((int stateIndex, int source, int target) in sample)
        {
            Array.Copy(states[stateIndex], child, width);
            var events = default(PourEvents);
            BoardRules.ApplyInPlace(child, 0, width, _units, source, target, PourMode.Run, false, ref events);
            Array.Copy(child, sorted, width);
            BoardCanon.Sort(sorted, 0, width);
            ulong key = BoardCanon.Hash(sorted, 0, width);
            if (!memo.TryGetValue(key, out SolveStatus status))
            {
                if (BoardRules.IsWon(child, 0, width, _units))
                {
                    status = SolveStatus.Solved;
                }
                else if (BoardRules.IsStuck(child, 0, width, _units, true))
                {
                    status = SolveStatus.Unsolvable;
                }
                else
                {
                    status = _trapSolver.Solve(new BoardState(sorted), trapOptions).Status;
                }

                memo[key] = status;
            }

            if (status == SolveStatus.Unsolvable)
            {
                traps++;
            }
            else if (status == SolveStatus.BudgetExceeded)
            {
                unknown++;
            }
        }

        int total = Math.Max(1, sample.Count);
        return new PathMetrics(branching, (double)traps / total, sample.Count, unknown, (double)solverMovesSum / solution.Length);
    }

    /// <summary>Vacíos libres: cuias sin dosis, sin mecánica y de capacidad ≥ 4 (03 §7: [E == 1] mide la estrechez).</summary>
    public static int FreeEmpties(BoardState state)
    {
        int empties = 0;
        for (int i = 0; i < state.Count; i++)
        {
            Cuia c = state[i];
            if (c.IsEmpty && !c.HasMechanic && c.Capacity >= Cuia.DefaultCapacity)
            {
                empties++;
            }
        }

        return empties;
    }

    /// <summary>Cuias vacías de capacidad ≥ 4, con o sin mecánica (la Hoja del ejemplo de 08 §4.1 cuenta como vacío).</summary>
    public static int Empties(BoardState state)
    {
        int empties = 0;
        for (int i = 0; i < state.Count; i++)
        {
            Cuia c = state[i];
            if (c.IsEmpty && c.Capacity >= Cuia.DefaultCapacity)
            {
                empties++;
            }
        }

        return empties;
    }

    private Cuia[] Canonical(BoardState state)
    {
        var cuias = new Cuia[state.Count];
        for (int i = 0; i < cuias.Length; i++)
        {
            cuias[i] = state[i];
        }

        BoardCanon.Sort(cuias, 0, cuias.Length);
        return cuias;
    }

    private void LoadUnits(BoardState state)
    {
        _units[0] = 0;
        for (int c = Cuia.MinColor; c <= Cuia.MaxColor; c++)
        {
            _units[c] = state.UnitsOf(c);
        }
    }
}

/// <summary>Índice D de 03 §7 con los pesos 0,35 / 0,25 / 0,20 / 0,10 / 0,10.</summary>
internal static class DIndex
{
    public const double WBot = 0.35;
    public const double WPar = 0.25;
    public const double WBranching = 0.20;
    public const double WTraps = 0.10;
    public const double WNarrow = 0.10;

    /// <summary>Media y desviación típica poblacional (como statistics.pstdev del prototipo; 1 si es 0).</summary>
    public static (double Mean, double Sd) Stats(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
        {
            return (values.Count == 1 ? values[0] : 0, 1);
        }

        double mean = values.Sum() / values.Count;
        double variance = values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        double sd = Math.Sqrt(variance);
        return (mean, sd < 1e-12 ? 1 : sd);
    }

    public static double Z(double x, (double Mean, double Sd) s) => (x - s.Mean) / s.Sd;
}
