// Elección de tintes (GDD 03 §4.5): el generador trabaja con colores abstractos 1..K y aquí se asignan K tintes del pool
// del mundo con debut anterior o igual al nivel (glosario §8-§9):
//   - obligatorios: el tinte que debuta en el mundo durante sus 3 primeros niveles, el agua del mundo si hay Lluvia
//     (02 §9.1 M6: 10 en agua blanca o clara, 14 en negra) y la llave de los tableros de estreno (Hoja aguaje, Liana
//     taperebá, Brote camu camu; D-007 y especificación);
//   - se rechazan las combinaciones con más pares de riesgo que max_pares_riesgo del nivel (0 en 1-40, 1 en 41-80, 2 en
//     81-200), hasta 50 barajados del pool;
//   - con max_pares_riesgo > 0 se evita que dos tintes de un par de riesgo queden juntos en una cuia permutando la
//     asignación índice → tinte (hasta 50 permutaciones); si ninguna lo logra se acepta la de menos contactos y se marca
//     risk_adjacent en el informe (el esquema no tiene ese campo).
using AF.Core.Board;
using AF.Core.Determinism;
using AmazonFill.Tools.LevelGenerator.Campaign;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal sealed record TintResult(int[] Map, IReadOnlyList<TintInfo> Palette, int RiskPairs, int RiskAdjacent);

internal static class TintAssigner
{
    public const int MaxTries = 50;

    /// <summary>
    /// Asigna tintes a los colores abstractos 1..<paramref name="colors"/> del tablero. <paramref name="fixedTints"/> fija
    /// color abstracto → tinte. Devuelve null si no hay combinación válida.
    /// </summary>
    public static TintResult? Assign(CampaignSpec spec, LevelPlan plan, int colors, IReadOnlyDictionary<int, string> fixedTints, GenBoard board, Prng rng)
    {
        IReadOnlyList<TintInfo> pool = spec.PoolAt(plan.Level);
        var required = new List<TintInfo>();
        foreach (string id in fixedTints.Values.Distinct().Order(StringComparer.Ordinal))
        {
            required.Add(spec.Tint(id));
        }

        foreach (TintInfo t in pool.Where(t => plan.Level - t.Debut is >= 0 and <= 2 && t.Debut >= spec.FirstLevel))
        {
            if (!required.Contains(t))
            {
                required.Add(t);
            }
        }

        if (required.Count > colors || required.Any(t => !pool.Contains(t)) || RiskCount(spec, required) > plan.MaxRiskPairs)
        {
            return null;
        }

        List<TintInfo>? chosen = null;
        for (int attempt = 0; attempt < MaxTries && chosen is null; attempt++)
        {
            List<TintInfo> rest = pool.Where(t => !required.Contains(t)).ToList();
            Shuffle(rest, rng);
            var set = new List<TintInfo>(required);
            foreach (TintInfo t in rest)
            {
                if (set.Count == colors)
                {
                    break;
                }

                set.Add(t);
                if (RiskCount(spec, set) > plan.MaxRiskPairs)
                {
                    set.RemoveAt(set.Count - 1);
                }
            }

            if (set.Count == colors)
            {
                chosen = set;
            }
        }

        if (chosen is null)
        {
            return null;
        }

        // Asignación: los colores con tinte fijo primero; el resto, permutaciones al azar.
        int[] best = [];
        int bestAdjacent = int.MaxValue;
        List<TintInfo> free = chosen.Where(t => !fixedTints.Values.Contains(t.Id)).ToList();
        int[] freeColors = Enumerable.Range(1, colors).Where(c => !fixedTints.ContainsKey(c)).ToArray();
        int tries = plan.MaxRiskPairs > 0 ? MaxTries : 1;
        for (int attempt = 0; attempt < tries; attempt++)
        {
            Shuffle(free, rng);
            var map = new int[Cuia.MaxColor + 1];
            map[Cuia.MudColor] = Cuia.MudColor;
            foreach ((int c, string id) in fixedTints)
            {
                map[c] = spec.Tint(id).Engine;
            }

            for (int i = 0; i < freeColors.Length; i++)
            {
                map[freeColors[i]] = free[i].Engine;
            }

            int adjacent = Adjacent(spec, board, map);
            if (adjacent < bestAdjacent)
            {
                best = map;
                bestAdjacent = adjacent;
            }

            if (adjacent == 0)
            {
                break;
            }
        }

        return new TintResult(best, chosen.OrderBy(t => t.Engine).ToList(), RiskCount(spec, chosen), bestAdjacent);
    }

    public static int RiskCount(CampaignSpec spec, IEnumerable<TintInfo> tints)
    {
        var engines = tints.Select(t => t.Engine).ToHashSet();
        return spec.RiskPairs.Count(p => engines.Contains(p.A) && engines.Contains(p.B));
    }

    public static int RiskCountEngines(CampaignSpec spec, IEnumerable<int> engines)
    {
        var set = engines.ToHashSet();
        return spec.RiskPairs.Count(p => set.Contains(p.A) && set.Contains(p.B));
    }

    /// <summary>Contactos entre tintes de un par de riesgo en capas vecinas de una misma cuia.</summary>
    public static int Adjacent(CampaignSpec spec, GenBoard board, int[] map)
    {
        int count = 0;
        foreach (List<int> cuia in board.Cuias)
        {
            for (int k = 1; k < cuia.Count; k++)
            {
                int a = cuia[k - 1] == Cuia.MudColor ? 0 : map[cuia[k - 1]];
                int b = cuia[k] == Cuia.MudColor ? 0 : map[cuia[k]];
                if (spec.RiskPairs.Any(p => (p.A == a && p.B == b) || (p.A == b && p.B == a)))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static void Shuffle<T>(List<T> list, Prng rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
