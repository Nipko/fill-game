// Paleta de los tableros del Reto del río (R3; 03 §4.5; R-Q1 y la nota docs/dev/reto-del-rio.md §1.1). Copia adaptada de
// Generation/TintAssigner.cs, que no se cambia: aquel elige los tintes de un nivel de campaña (LevelPlan, con el tinte que
// debuta en el mundo y las llaves de las mecánicas obligatorios); aquí no hay mecánicas ni debuts que forzar, así que:
//   - el pool de la banda son los tintes con debut <= palette.debut_until (CampaignSpec.PoolAt de tints_from);
//   - se enumeran todos los conjuntos de K tintes del pool (como mucho 2^12 máscaras con los 12 tintes de B3) y se elige uno al azar, con el PRNG
//     del candidato, entre los que tienen como mucho el tope efectivo de pares de riesgo (exacto, sin los 50 barajados de
//     TintAssigner, que pueden fallar aunque exista un conjunto válido);
//   - el tope es max_risk_pairs de la banda; con "libre" (R-Q1b (a): B3) entran todos los conjuntos. Si con algún K de la
//     banda el mínimo de pares posible supera el tope, la especificación no se puede cumplir: error (DailySpecException) y
//     no se genera nada (R-Q1b (a) decidió B2 solo con K = 8 justo por eso; nota §1.1);
//   - la asignación índice -> tinte evita que dos tintes de un par de riesgo queden juntos en una cuia, como TintAssigner
//     (hasta 50 permutaciones, con TintAssigner.Adjacent; si ninguna lo logra, la de menos contactos).
using AF.Core.Board;
using AF.Core.Determinism;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;

namespace AmazonFill.Tools.LevelGenerator.Daily;

internal sealed record DailyPalette(int[] Map, IReadOnlyList<TintInfo> Palette, int RiskPairs, int RiskAdjacent);

internal sealed class DailyTints
{
    private readonly CampaignSpec _tints;
    private readonly Dictionary<(int Band, int K), (List<TintInfo[]> Sets, int Cap, int MinPairs)> _sets = [];

    public DailyTints(CampaignSpec tints, DailySpec spec)
    {
        _tints = tints;
        foreach (DailyBand band in spec.Bands)
        {
            IReadOnlyList<TintInfo> pool = tints.PoolAt(band.DebutUntil);
            for (int k = band.KMin; k <= band.KMax; k++)
            {
                if (k > pool.Count)
                {
                    throw new DailySpecException(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                        $"banda {band.Band}: K = {k} y el pool de tintes con debut <= {band.DebutUntil} solo tiene {pool.Count}"));
                }

                var all = new List<(TintInfo[] Set, int Pairs)>();
                for (int mask = 0; mask < 1 << pool.Count; mask++)
                {
                    if (System.Numerics.BitOperations.PopCount((uint)mask) != k)
                    {
                        continue;
                    }

                    var set = new TintInfo[k];
                    int n = 0;
                    for (int i = 0; i < pool.Count; i++)
                    {
                        if ((mask & (1 << i)) != 0)
                        {
                            set[n++] = pool[i];
                        }
                    }

                    all.Add((set, TintAssigner.RiskCount(tints, set)));
                }

                int min = all.Min(s => s.Pairs);
                int cap = band.MaxRiskPairs;
                if (min > cap)
                {
                    throw new DailySpecException(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                        $"banda {band.Band}: con K = {k} el minimo de pares de riesgo es {min}, por encima del tope {cap} (R-Q1 y R-Q1b; nota sec. 1.1)"));
                }

                _sets[(band.Band, k)] = (all.Where(s => s.Pairs <= cap).Select(s => s.Set).ToList(), cap, min);
            }
        }
    }

    public IReadOnlyList<TintInfo> Pool(DailyBand band) => _tints.PoolAt(band.DebutUntil);

    /// <summary>Mínimo de pares de riesgo posible con K tintes del pool de la banda.</summary>
    public int MinPairs(int band, int k) => _sets[(band, k)].MinPairs;

    /// <summary>Tope de la banda (con «libre», todos los pares de 03 §4.5).</summary>
    public int EffectiveCap(int band, int k) => _sets[(band, k)].Cap;

    /// <summary>Conjuntos de K tintes dentro del tope efectivo.</summary>
    public int SetCount(int band, int k) => _sets[(band, k)].Sets.Count;

    public DailyPalette Assign(DailyBand band, int k, GenBoard board, Prng rng)
    {
        List<TintInfo[]> sets = _sets[(band.Band, k)].Sets;
        List<TintInfo> free = [.. sets[rng.NextInt(sets.Count)]];
        int risk = TintAssigner.RiskCount(_tints, free);
        int[] best = [];
        int bestAdjacent = int.MaxValue;
        int tries = risk > 0 ? TintAssigner.MaxTries : 1;
        for (int attempt = 0; attempt < tries; attempt++)
        {
            for (int i = free.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                (free[i], free[j]) = (free[j], free[i]);
            }

            var map = new int[Cuia.MaxColor + 1];
            map[Cuia.MudColor] = Cuia.MudColor;
            for (int c = 1; c <= k; c++)
            {
                map[c] = free[c - 1].Engine;
            }

            int adjacent = TintAssigner.Adjacent(_tints, board, map);
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

        return new DailyPalette(best, free.OrderBy(t => t.Engine).ToList(), risk, bestAdjacent);
    }
}
