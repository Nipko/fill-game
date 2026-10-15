// Tablero mutable del generador (colores abstractos 1..K mientras se superponen mecánicas; índices de motor después de
// TintAssigner). Solo datos: la validez la deciden BoardBuilder y LevelValidation de AF.Core.
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal sealed class GenBoard
{
    public List<int> Caps { get; } = [];
    public List<List<int>> Cuias { get; } = [];
    public List<MechanicSpec> Attrs { get; } = [];
    public List<(int Cuia, int Layer)> Hidden { get; } = [];

    public int Count => Caps.Count;

    public static GenBoard From(BoardState state)
    {
        var b = new GenBoard();
        for (int i = 0; i < state.Count; i++)
        {
            Cuia c = state[i];
            b.Caps.Add(c.Capacity);
            var doses = new List<int>(c.Count);
            for (int k = 0; k < c.Count; k++)
            {
                doses.Add(c.ColorAt(k));
            }

            b.Cuias.Add(doses);
            b.Attrs.Add(MechanicSpec.None);
        }

        return b;
    }

    public GenBoard Clone()
    {
        var b = new GenBoard();
        b.Caps.AddRange(Caps);
        foreach (List<int> c in Cuias)
        {
            b.Cuias.Add([.. c]);
        }

        b.Attrs.AddRange(Attrs);
        b.Hidden.AddRange(Hidden);
        return b;
    }

    /// <summary>Añade una cuia vacía al final y devuelve su índice.</summary>
    public int AddEmpty(int cap, MechanicSpec attr)
    {
        Caps.Add(cap);
        Cuias.Add([]);
        Attrs.Add(attr);
        return Caps.Count - 1;
    }

    public bool IsHidden(int cuia, int layer) => Hidden.Contains((cuia, layer));

    public bool HasHidden(int cuia) => Hidden.Any(h => h.Cuia == cuia);

    public int Top(int i) => Cuias[i].Count == 0 ? 0 : Cuias[i][^1];

    /// <summary>Colores (no Barro) presentes y sus dosis.</summary>
    public SortedDictionary<int, int> Units()
    {
        var u = new SortedDictionary<int, int>();
        foreach (int c in Cuias.SelectMany(x => x).Where(c => c != Cuia.MudColor))
        {
            u[c] = u.TryGetValue(c, out int n) ? n + 1 : 1;
        }

        for (int i = 0; i < Count; i++)
        {
            if (Attrs[i].Type == MechanicType.Lluvia)
            {
                int key = Attrs[i].Key;
                u[key] = (u.TryGetValue(key, out int n) ? n : 0) + Attrs[i].N;
            }
        }

        return u;
    }

    /// <summary>
    /// Cambia los colores con <paramref name="map"/>[color] (el Barro, 15, no cambia), también las llaves: <c>key</c> y la
    /// segunda llave de la Flor, <c>key2</c> (P5).
    /// </summary>
    public void MapColors(int[] map)
    {
        foreach (List<int> c in Cuias)
        {
            for (int k = 0; k < c.Count; k++)
            {
                if (c[k] != Cuia.MudColor)
                {
                    c[k] = map[c[k]];
                }
            }
        }

        for (int i = 0; i < Count; i++)
        {
            MechanicSpec a = Attrs[i];
            if (!a.IsNone && a.Key != 0)
            {
                Attrs[i] = new MechanicSpec(a.Type, map[a.Key], a.Partner, a.N, a.K, a.Key2 != 0 ? map[a.Key2] : 0);
            }
        }
    }

    /// <summary>Reordena las cuias en orden visual: la nueva posición j es la antigua <paramref name="order"/>[j].</summary>
    public GenBoard Permuted(int[] order)
    {
        var inverse = new int[order.Length];
        for (int j = 0; j < order.Length; j++)
        {
            inverse[order[j]] = j;
        }

        var b = new GenBoard();
        foreach (int old in order)
        {
            b.Caps.Add(Caps[old]);
            b.Cuias.Add([.. Cuias[old]]);
            MechanicSpec a = Attrs[old];
            b.Attrs.Add(a.Type == MechanicType.Remolino ? new MechanicSpec(a.Type, a.Key, inverse[a.Partner], a.N, a.K, a.Key2) : a);
        }

        foreach ((int cuia, int layer) in Hidden)
        {
            b.Hidden.Add((inverse[cuia], layer));
        }

        b.Hidden.Sort();
        return b;
    }

    /// <summary>Permutación visual al azar (Fisher-Yates con el PRNG del núcleo).</summary>
    public GenBoard Shuffled(Prng rng)
    {
        int[] order = Enumerable.Range(0, Count).ToArray();
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = rng.NextInt(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        return Permuted(order);
    }

    public BoardData ToData() => new(
        [.. Caps],
        Cuias.Select(c => c.ToArray()).ToArray(),
        [.. Attrs],
        Hidden.OrderBy(h => h.Cuia).ThenBy(h => h.Layer).Select(h => new[] { h.Cuia, h.Layer }).ToArray());
}
