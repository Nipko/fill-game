// Ruta óptima de referencia para las métricas de la ruta (03 §7: ramificación y trampas «a lo largo de la solución
// óptima»). Con varias soluciones del mismo largo, la métrica depende de cuál se mida: AF.Solver expande en el orden
// canónico de las cuias y el prototipo (03 §18, cifras [V-proto]) en el orden real del tablero. Esta búsqueda reproduce el
// ASTAR de 03 §5 en el orden del prototipo (encola (f, g, llegada) y expande los MOVIMIENTOS en el orden real) para que
// las métricas sean comparables con 03 §18 [P]. No es una regla: legalidad, fútiles, cantidad, aplicación, victoria y
// heurística son las de AF.Core y AF.Solver (BoardRules, SolverHeuristic). El par sigue saliendo de AF.Solver; esta ruta
// solo se usa si tiene la misma longitud.
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;

namespace AmazonFill.Tools.LevelGenerator.Metrics;

internal static class ReferencePath
{
    private sealed class PriorityComparer : IComparer<(int F, int G, long Order)>
    {
        public static readonly PriorityComparer Instance = new();

        public int Compare((int F, int G, long Order) x, (int F, int G, long Order) y)
        {
            int c = x.F.CompareTo(y.F);
            if (c != 0)
            {
                return c;
            }

            c = x.G.CompareTo(y.G);
            return c != 0 ? c : x.Order.CompareTo(y.Order);
        }
    }

    /// <summary>
    /// <c>MOVIMIENTOS(S)</c> literal de 03 §5 en el orden real: para cada origen i y destino j ≠ i, los vertidos legales y no
    /// fútiles de <see cref="BoardRules"/>; a un vacío, solo el primero por (capacidad, mecánica) de cada origen.
    /// </summary>
    public static IEnumerable<(int Source, int Target)> Moves(Cuia[] cuias, int width, int[] units)
    {
        var used = new HashSet<ulong>();
        for (int i = 0; i < width; i++)
        {
            if (!BoardRules.CanBeSource(cuias, 0, width, units, i))
            {
                continue;
            }

            used.Clear();
            for (int j = 0; j < width; j++)
            {
                if (j == i || !BoardRules.IsLegal(cuias, 0, width, units, i, j) || BoardRules.IsFutile(cuias, 0, width, units, i, j))
                {
                    continue;
                }

                if (cuias[j].IsEmpty && !used.Add(cuias[j].Packed))
                {
                    continue;
                }

                yield return (i, j);
            }
        }
    }

    /// <summary>
    /// ASTAR de 03 §5 (w = 1) en el orden del prototipo. Devuelve la solución (índices reales del tablero de partida) o null
    /// si no la encuentra dentro de <paramref name="budget"/> expandidos.
    /// </summary>
    public static Pour[]? Find(BoardState start, int budget, out int expanded)
    {
        int width = start.Count;
        var units = new int[CuiaRules.UnitsTableLength];
        for (int c = Cuia.MinColor; c <= Cuia.MaxColor; c++)
        {
            units[c] = start.UnitsOf(c);
        }

        var states = new List<Cuia[]>();
        var g = new List<int>();
        var parent = new List<int>();
        var move = new List<(int Source, int Target)>();
        var index = new Dictionary<ulong, int>();
        var open = new PriorityQueue<int, (int F, int G, long Order)>(PriorityComparer.Instance);
        var sorted = new Cuia[width];
        long order = 0;

        Cuia[] root = new Cuia[width];
        for (int i = 0; i < width; i++)
        {
            root[i] = start[i];
        }

        states.Add(root);
        g.Add(0);
        parent.Add(-1);
        move.Add((-1, -1));
        index[Key(root, sorted)] = 0;
        open.Enqueue(0, (SolverHeuristic.Estimate(root, 0, width, PourMode.Run), 0, order++));
        expanded = 0;
        while (open.TryDequeue(out int node, out (int F, int G, long Order) priority))
        {
            if (priority.G > g[node])
            {
                continue; // entrada obsoleta
            }

            Cuia[] s = states[node];
            if (BoardRules.IsWon(s, 0, width, units))
            {
                return Rebuild(node, parent, move, start);
            }

            expanded++;
            if (expanded > budget)
            {
                return null;
            }

            int ng = g[node] + 1;
            foreach ((int src, int tgt) in Moves(s, width, units).ToList())
            {
                var t = (Cuia[])s.Clone();
                var events = default(PourEvents);
                BoardRules.ApplyInPlace(t, 0, width, units, src, tgt, PourMode.Run, false, ref events);
                ulong key = Key(t, sorted);
                if (!index.TryGetValue(key, out int existing))
                {
                    existing = states.Count;
                    states.Add(t);
                    g.Add(ng);
                    parent.Add(node);
                    move.Add((src, tgt));
                    index[key] = existing;
                }
                else if (ng < g[existing])
                {
                    states[existing] = t;
                    g[existing] = ng;
                    parent[existing] = node;
                    move[existing] = (src, tgt);
                }
                else
                {
                    continue;
                }

                open.Enqueue(existing, (ng + SolverHeuristic.Estimate(t, 0, width, PourMode.Run), ng, order++));
            }
        }

        return null;
    }

    private static ulong Key(Cuia[] cuias, Cuia[] sorted)
    {
        Array.Copy(cuias, sorted, cuias.Length);
        BoardCanon.Sort(sorted, 0, cuias.Length);
        return BoardCanon.Hash(sorted, 0, cuias.Length);
    }

    private static Pour[]? Rebuild(int goal, List<int> parent, List<(int Source, int Target)> move, BoardState start)
    {
        var steps = new List<(int Source, int Target)>();
        for (int n = goal; parent[n] >= 0; n = parent[n])
        {
            steps.Add(move[n]);
        }

        steps.Reverse();
        var pours = new Pour[steps.Count];
        BoardState s = start;
        for (int i = 0; i < steps.Count; i++)
        {
            (int src, int tgt) = steps[i];
            if (!PourRules.IsLegal(s, src, tgt))
            {
                return null; // el padre se reabrió con otro orden real: la ruta no se puede rehacer
            }

            int amount = PourRules.Amount(s, src, tgt, PourMode.Run);
            pours[i] = new Pour(src, tgt, amount);
            s = PourRules.Apply(s, src, tgt, PourMode.Run);
        }

        return s.IsWon() ? pours : null;
    }
}
