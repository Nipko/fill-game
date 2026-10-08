// Espacio de estados alcanzable de un tablero pequeño (docs/dev/prototipo-v0.md b: BFS sobre PourRules, ≤ 8 cuias)
// en un modo de vertido (D-005: Run, la racha que cabe, por defecto; Single mueve una dosis, solo pruebas;
// docs/dev/decisiones.md).
// Todas las reglas salen de AF.Core: vertidos legales (PourRules.CollectUsefulMoves con el kill-switch apagado da
// todos los legales en orden (A, B) ascendente), vertidos útiles y fútiles, aplicación (PourRules.Apply con el modo),
// victoria y Aguas estancadas (estas dos y legal/fútil no dependen del modo; lo que cambia es qué estados se alcanzan). Los estados se identifican por su forma canónica (03 §3: cuias ordenadas por Cuia.Packed con
// BoardCanon.Sort), con una clave exacta (no un hash) para que no haya colisiones.
// Determinismo: los diccionarios solo se consultan; todo lo que decide recorre listas en orden de descubrimiento.
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Core.Rules;

namespace AmazonFill.Tools.LevelGenerator.Handmade;

internal sealed class StateGraph
{
    private readonly Pour[] _buffer = new Pour[PourRules.MaxPours];
    private readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);
    private readonly List<BoardState> _rep = new();
    private readonly List<string> _keys = new();
    private readonly List<int> _parent = new();
    private readonly List<Pour> _parentMove = new();
    private readonly List<int> _depth = new();
    private readonly List<int[]> _succ = new();
    private int[] _distToWin = [];

    public StateGraph(BoardState start, PourMode mode)
    {
        if (!PourModes.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Modo de vertido desconocido.");
        }

        Start = start;
        Mode = mode;
        Add(start, -1, default, 0);
        for (int node = 0; node < _rep.Count; node++)
        {
            BoardState s = _rep[node];
            Pour[] legal = LegalPours(s);
            var outs = new int[legal.Length];
            for (int k = 0; k < legal.Length; k++)
            {
                BoardState t = Apply(s, legal[k]);
                string key = Key(t);
                if (!_index.TryGetValue(key, out int child))
                {
                    child = Add(t, node, legal[k], _depth[node] + 1, key);
                }

                outs[k] = child;
            }

            _succ.Add(outs);
        }

        ComputeDistancesToWin();
    }

    /// <summary>Tablero de partida (orden real de las cuias).</summary>
    public BoardState Start { get; }

    /// <summary>Modo de vertido de todas las transiciones (D-005: Run por defecto; Single, pruebas).</summary>
    public PourMode Mode { get; }

    /// <summary>Estados canónicos alcanzables desde el inicio.</summary>
    public int StateCount => _rep.Count;

    /// <summary>Estados alcanzables desde los que ya no se puede ganar.</summary>
    public int DeadStateCount => _distToWin.Count(d => d < 0);

    /// <summary>
    /// Vertidos legales de <paramref name="state"/> en orden (A, B) ascendente (todos, fútiles incluidos), con la
    /// cantidad de <see cref="Mode"/>.
    /// </summary>
    public Pour[] LegalPours(BoardState state) => Collect(state, futileRuleEnabled: false);

    /// <summary>
    /// <c>movimientos_utiles(S)</c> de 02 §1.4 (legales y no fútiles), en orden (A, B) ascendente, con la cantidad de
    /// <see cref="Mode"/>.
    /// </summary>
    public Pour[] UsefulPours(BoardState state) => Collect(state, futileRuleEnabled: true);

    /// <summary>Distancia mínima a la victoria (par desde ese estado); null si ya no se puede ganar.</summary>
    public int? Distance(BoardState state)
    {
        if (!_index.TryGetValue(Key(state), out int node))
        {
            throw new InvalidOperationException("El estado no es alcanzable desde el inicio.");
        }

        int d = _distToWin[node];
        return d < 0 ? null : d;
    }

    /// <summary>
    /// Solución óptima: en cada paso, el primer vertido legal (orden (A, B)) que baja la distancia en 1.
    /// Null si el tablero no tiene solución.
    /// </summary>
    public List<Pour>? OptimalSolution()
    {
        int? d = Distance(Start);
        if (d is null)
        {
            return null;
        }

        var path = new List<Pour>();
        BoardState s = Start;
        while (d > 0)
        {
            bool advanced = false;
            foreach (Pour m in LegalPours(s))
            {
                BoardState t = Apply(s, m);
                if (Distance(t) == d - 1)
                {
                    path.Add(m);
                    s = t;
                    d--;
                    advanced = true;
                    break;
                }
            }

            if (!advanced)
            {
                throw new InvalidOperationException("La distancia a la victoria no es consistente con la exploración.");
            }
        }

        return path;
    }

    /// <summary>
    /// Camino más corto (en vertidos reales desde el inicio) a un estado en Aguas estancadas (02 R13 con
    /// <c>futile_rule_enabled</c> = true); a igual profundidad, el primero descubierto. Null si no hay ninguno.
    /// </summary>
    public List<Pour>? ShortestStuckPath()
    {
        for (int node = 0; node < _rep.Count; node++) // BFS: orden de descubrimiento = profundidad creciente
        {
            if (PourRules.IsStuck(_rep[node], futileRuleEnabled: true))
            {
                return PathTo(node);
            }
        }

        return null;
    }

    /// <summary>
    /// Mayor aumento de la distancia a la victoria con un vertido útil desde un estado resoluble (null si no hay
    /// vertidos útiles) y número de vertidos útiles que llevan a un estado sin salida.
    /// </summary>
    public (int? WorstDetour, int ToDead) Detours()
    {
        int? worst = null;
        int toDead = 0;
        for (int node = 0; node < _rep.Count; node++)
        {
            int d = _distToWin[node];
            if (d <= 0)
            {
                continue;
            }

            BoardState s = _rep[node];
            foreach (Pour m in UsefulPours(s))
            {
                int? dt = Distance(Apply(s, m));
                if (dt is null)
                {
                    toDead++;
                }
                else if (worst is null || dt.Value - d > worst.Value)
                {
                    worst = dt.Value - d;
                }
            }
        }

        return (worst, toDead);
    }

    /// <summary>
    /// Número de soluciones óptimas distintas: secuencias de vertidos reales (pares de índices; el tablero no se canoniza)
    /// que bajan la distancia a la victoria en 1 en cada paso. 0 si el tablero no tiene solución.
    /// </summary>
    public long CountOptimalSolutions()
    {
        return Distance(Start) is null ? 0 : CountFrom(Start, new Dictionary<string, long>(StringComparer.Ordinal));
    }

    /// <summary>
    /// Toda solución óptima repite al menos <paramref name="times"/> veces un mismo vertido A→B (no necesariamente
    /// seguidos). En <see cref="PourMode.Single"/> (solo pruebas desde D-005; era el modo de D-001) una racha de n dosis
    /// solo pasa de A a B con n vertidos A→B: si se cumple, quien juega con el par no puede evitar repetir el vertido.
    /// False si el tablero no tiene solución.
    /// </summary>
    public bool EveryOptimalSolutionRepeatsAPour(int times)
    {
        if (times < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(times), times, "Al menos 1 vertido.");
        }

        if (Distance(Start) is null)
        {
            return false;
        }

        var counts = new int[Start.Count * Start.Count];
        return !CanAvoid(Start, counts, times, new Dictionary<string, bool>(StringComparer.Ordinal));
    }

    /// <summary>Vertidos de <paramref name="s"/> que bajan la distancia a la victoria en 1, con su estado siguiente.</summary>
    private List<(Pour Move, BoardState Next)> OptimalSteps(BoardState s)
    {
        int d = Distance(s)!.Value;
        var steps = new List<(Pour, BoardState)>();
        foreach (Pour m in LegalPours(s))
        {
            BoardState t = Apply(s, m);
            if (Distance(t) == d - 1)
            {
                steps.Add((m, t));
            }
        }

        return steps;
    }

    private long CountFrom(BoardState s, Dictionary<string, long> memo)
    {
        if (Distance(s) == 0)
        {
            return 1;
        }

        string key = ExactKey(s);
        if (memo.TryGetValue(key, out long cached))
        {
            return cached;
        }

        long total = 0;
        foreach ((Pour _, BoardState next) in OptimalSteps(s))
        {
            total += CountFrom(next, memo);
        }

        memo[key] = total;
        return total;
    }

    // ¿Hay una continuación óptima desde s en la que ningún vertido A→B llegue a `times` usos? counts[A·T + B] = usos.
    private bool CanAvoid(BoardState s, int[] counts, int times, Dictionary<string, bool> memo)
    {
        if (Distance(s) == 0)
        {
            return true;
        }

        string key = ExactKey(s) + "|" + string.Join(",", counts);
        if (memo.TryGetValue(key, out bool cached))
        {
            return cached;
        }

        bool found = false;
        foreach ((Pour m, BoardState next) in OptimalSteps(s))
        {
            int k = m.Source * s.Count + m.Target;
            if (counts[k] + 1 >= times)
            {
                continue;
            }

            counts[k]++;
            found = CanAvoid(next, counts, times, memo);
            counts[k]--;
            if (found)
            {
                break;
            }
        }

        memo[key] = found;
        return found;
    }

    /// <summary>Clave exacta del tablero real (sin ordenar las cuias).</summary>
    private static string ExactKey(BoardState state)
    {
        var sb = new StringBuilder(state.Count * 17);
        for (int i = 0; i < state.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('.');
            }

            sb.Append(state[i].Packed.ToString("x", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    /// <summary>Aplica <paramref name="move"/> en <see cref="Mode"/> (PourRules.Apply; el vertido debe ser legal).</summary>
    public BoardState Apply(BoardState state, Pour move) => PourRules.Apply(state, move.Source, move.Target, Mode);

    private Pour[] Collect(BoardState state, bool futileRuleEnabled)
    {
        int count = PourRules.CollectUsefulMoves(state, futileRuleEnabled, Mode, _buffer);
        var result = new Pour[count];
        Array.Copy(_buffer, result, count);
        return result;
    }

    private List<Pour> PathTo(int node)
    {
        var moves = new List<Pour>();
        while (_parent[node] >= 0)
        {
            moves.Add(_parentMove[node]);
            node = _parent[node];
        }

        moves.Reverse();
        return moves;
    }

    private int Add(BoardState state, int parent, Pour move, int depth, string? key = null)
    {
        int node = _rep.Count;
        key ??= Key(state);
        _index.Add(key, node);
        _keys.Add(key);
        _rep.Add(state);
        _parent.Add(parent);
        _parentMove.Add(move);
        _depth.Add(depth);
        return node;
    }

    private void ComputeDistancesToWin()
    {
        int n = _rep.Count;
        var predecessors = new List<int>[n];
        for (int i = 0; i < n; i++)
        {
            predecessors[i] = new List<int>();
        }

        for (int i = 0; i < n; i++)
        {
            foreach (int child in _succ[i])
            {
                predecessors[child].Add(i);
            }
        }

        _distToWin = new int[n];
        Array.Fill(_distToWin, -1);
        var queue = new Queue<int>();
        for (int i = 0; i < n; i++)
        {
            if (_rep[i].IsWon())
            {
                _distToWin[i] = 0;
                queue.Enqueue(i);
            }
        }

        while (queue.Count > 0)
        {
            int k = queue.Dequeue();
            foreach (int p in predecessors[k])
            {
                if (_distToWin[p] < 0)
                {
                    _distToWin[p] = _distToWin[k] + 1;
                    queue.Enqueue(p);
                }
            }
        }
    }

    /// <summary>Clave canónica exacta: <see cref="Cuia.Packed"/> de las cuias ordenadas con <see cref="BoardCanon.Sort"/>.</summary>
    private static string Key(BoardState state)
    {
        var cuias = new Cuia[state.Count];
        for (int i = 0; i < cuias.Length; i++)
        {
            cuias[i] = state[i];
        }

        BoardCanon.Sort(cuias, 0, cuias.Length);
        var sb = new StringBuilder(cuias.Length * 17);
        for (int i = 0; i < cuias.Length; i++)
        {
            if (i > 0)
            {
                sb.Append('.');
            }

            sb.Append(cuias[i].Packed.ToString("x", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }
}
