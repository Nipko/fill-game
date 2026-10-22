// Elección de tintes (GDD 03 §4.5): el generador trabaja con colores abstractos 1..K y aquí se asignan K tintes del pool
// del mundo con debut anterior o igual al nivel (glosario §8-§9):
//   - obligatorios: el tinte que debuta en el mundo durante sus 3 primeros niveles, el agua del mundo si hay Lluvia
//     (02 §9.1 M6: 10 en agua blanca o clara, 14 en negra) y la llave de los tableros de estreno (Hoja aguaje, Liana
//     taperebá, Brote camu camu; D-007 y especificación);
//   - se rechazan las combinaciones con más pares de riesgo que max_pares_riesgo del nivel (0 en 1-40, 1 en 41-80, 2 en
//     81-200), hasta 50 barajados del pool;
//   - con max_pares_riesgo > 0 se evita que dos tintes de un par de riesgo queden juntos en una cuia permutando la
//     asignación índice → tinte (hasta 50 permutaciones); si ninguna lo logra se acepta la de menos contactos y se marca
//     risk_adjacent en el informe (el esquema no tiene ese campo);
//   - pares del simulador de daltonismo (cvd_pairs; Q-B9-13 bis (b) y (A); 07 §3, regla 3, y glosario §9): no cuentan en
//     risk_pairs ni en max_pares_riesgo. La asignación de arriba no cambia; si deja dos tintes de un par del simulador juntos
//     en una cuia, CvdAlternatives propone otras permutaciones de los tintes libres (con su propia subsemilla, sin tocar el
//     PRNG de arriba) sin contactos del simulador y sin más contactos de pares de riesgo, y el generador solo acepta una si el
//     bot (y todo lo que se mide del tablero) da lo mismo que con la de hoy (SlotGenerator y DailyRunner). Si ninguna de las 50
//     lo cumple, se queda la de hoy y se marca cvd_adjacent (level-qa: QA-CVD-ADJ, aviso).
using AF.Core.Board;
using AF.Core.Determinism;
using AmazonFill.Tools.LevelGenerator.Campaign;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal sealed record TintResult(int[] Map, IReadOnlyList<TintInfo> Palette, int RiskPairs, int RiskAdjacent, int CvdAdjacent = 0);

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

    /// <summary>Pares del simulador de daltonismo (cvd_pairs) presentes en un conjunto de tintes; no cuentan en risk_pairs.</summary>
    public static int CvdCount(CampaignSpec spec, IEnumerable<TintInfo> tints)
    {
        var engines = tints.Select(t => t.Engine).ToHashSet();
        return spec.CvdPairs.Count(p => engines.Contains(p.A) && engines.Contains(p.B));
    }

    public static int RiskCountEngines(CampaignSpec spec, IEnumerable<int> engines)
    {
        var set = engines.ToHashSet();
        return spec.RiskPairs.Count(p => set.Contains(p.A) && set.Contains(p.B));
    }

    /// <summary>Contactos entre tintes de un par de riesgo en capas vecinas de una misma cuia.</summary>
    public static int Adjacent(CampaignSpec spec, GenBoard board, int[] map) => Contacts(spec.RiskPairs, board.Cuias, map);

    /// <summary>Contactos entre tintes de un par del simulador de daltonismo (cvd_pairs) en capas vecinas de una misma cuia.</summary>
    public static int CvdAdjacent(CampaignSpec spec, GenBoard board, int[] map) => Contacts(spec.CvdPairs, board.Cuias, map);

    /// <summary>
    /// Contactos de los pares <paramref name="pairs"/> en capas vecinas de una misma cuia; <paramref name="map"/> traduce cada
    /// color de la cuia a su tinte (null: las cuias ya llevan el índice de motor, como en un pack publicado). El Barro no cuenta.
    /// </summary>
    public static int Contacts(IReadOnlyList<(int A, int B)> pairs, IEnumerable<IReadOnlyList<int>> cuias, int[]? map)
    {
        if (pairs.Count == 0)
        {
            return 0;
        }

        int count = 0;
        foreach (IReadOnlyList<int> cuia in cuias)
        {
            for (int k = 1; k < cuia.Count; k++)
            {
                int a = cuia[k - 1] == Cuia.MudColor ? 0 : map is null ? cuia[k - 1] : map[cuia[k - 1]];
                int b = cuia[k] == Cuia.MudColor ? 0 : map is null ? cuia[k] : map[cuia[k]];
                foreach ((int pa, int pb) in pairs)
                {
                    if ((pa == a && pb == b) || (pa == b && pb == a))
                    {
                        count++;
                        break;
                    }
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Otras asignaciones de los <paramref name="colors"/> colores abstractos sin contactos del simulador de daltonismo y sin más
    /// contactos de pares de riesgo que <paramref name="map"/> (Q-B9-13 bis (A)): hasta <see cref="MaxTries"/> barajados de los
    /// tintes libres (los de <paramref name="fixedColors"/> no se mueven) con el PRNG <paramref name="rng"/>, en el orden en que
    /// salen y sin repetidas. Vacía si <paramref name="map"/> no tiene contactos del simulador.
    /// </summary>
    public static List<int[]> CvdAlternatives(CampaignSpec spec, GenBoard board, int[] map, int colors, ICollection<int> fixedColors, Prng rng)
    {
        var result = new List<int[]>();
        if (CvdAdjacent(spec, board, map) == 0)
        {
            return result;
        }

        int risk = Adjacent(spec, board, map);
        int[] freeColors = Enumerable.Range(1, colors).Where(c => !fixedColors.Contains(c)).ToArray();
        List<int> free = freeColors.Select(c => map[c]).ToList();
        var seen = new HashSet<string>(StringComparer.Ordinal) { string.Join(",", free) };
        for (int attempt = 0; attempt < MaxTries; attempt++)
        {
            Shuffle(free, rng);
            if (!seen.Add(string.Join(",", free)))
            {
                continue;
            }

            int[] alt = (int[])map.Clone();
            for (int i = 0; i < freeColors.Length; i++)
            {
                alt[freeColors[i]] = free[i];
            }

            if (CvdAdjacent(spec, board, alt) == 0 && Adjacent(spec, board, alt) <= risk)
            {
                result.Add(alt);
            }
        }

        return result;
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
