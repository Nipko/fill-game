// Espacio de estados alcanzable de un tablero pequeño (docs/dev/prototipo-v0.md b: BFS sobre PourRules, ≤ 8 cuias).
// Todas las reglas salen de AF.Core: vertidos legales (PourRules.CollectUsefulMoves con el kill-switch apagado da
// todos los legales en orden (A, B) ascendente), vertidos útiles y fútiles, aplicación (PourRules.Apply), victoria y
// Aguas estancadas. Los estados se identifican por su forma canónica (03 §3: cuias ordenadas por Cuia.Packed con
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

    public StateGraph(BoardState start)
    {
        Start = start;
        Add(start, -1, default, 0);
        for (int node = 0; node < _rep.Count; node++)
        {
            BoardState s = _rep[node];
            Pour[] legal = LegalPours(s);
            var outs = new int[legal.Length];
            for (int k = 0; k < legal.Length; k++)
            {
                BoardState t = PourRules.Apply(s, legal[k].Source, legal[k].Target);
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

    /// <summary>Estados canónicos alcanzables desde el inicio.</summary>
    public int StateCount => _rep.Count;

    /// <summary>Estados alcanzables desde los que ya no se puede ganar.</summary>
    public int DeadStateCount => _distToWin.Count(d => d < 0);

    /// <summary>Vertidos legales de <paramref name="state"/> en orden (A, B) ascendente (todos, fútiles incluidos).</summary>
    public Pour[] LegalPours(BoardState state) => Collect(state, futileRuleEnabled: false);

    /// <summary><c>movimientos_utiles(S)</c> de 02 §1.4 (legales y no fútiles), en orden (A, B) ascendente.</summary>
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
                BoardState t = PourRules.Apply(s, m.Source, m.Target);
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
                int? dt = Distance(PourRules.Apply(s, m.Source, m.Target));
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

    private Pour[] Collect(BoardState state, bool futileRuleEnabled)
    {
        int count = PourRules.CollectUsefulMoves(state, futileRuleEnabled, _buffer);
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
