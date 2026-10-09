// Pool del rango, índice D y bandas (GDD 03 §7): los z-scores salen de TODOS los candidatos en ventana de los slots del
// rango de 03 §11 (11-30, 31-60, 61-100, 101-200), con al menos 3 por slot (03 §2: el pool es ≥ 3× los slots). La banda
// es el cuantil de D dentro de ese pool: recuperación ≤ 0,10 · comodidad ≤ 0,70 · reto ≤ 0,95 · hito > 0,95. Para medir
// saltos (03 §12: +2,2 / −3,4 bandas) se usa la banda continua [P]: 0-1 recuperación, 1-2 comodidad, 2-3 reto, 3-4 hito,
// lineal en el cuantil dentro de cada tramo. Los estadísticos se congelan tras la primera pasada: los candidatos extra que
// pida la colocación se califican con ellos.
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal sealed class RangeStats
{
    public required RangeInfo Range { get; init; }
    public required (double Mean, double Sd) Bot { get; init; }
    public required (double Mean, double Sd) ParExcess { get; init; }
    public required (double Mean, double Sd) Branching { get; init; }
    public required (double Mean, double Sd) Traps { get; init; }
    public required double[] SortedD { get; init; }
    public required int PoolSize { get; init; }
    public required int Slots { get; init; }

    public double D(Candidate c) =>
        (DIndex.WBot * DIndex.Z(c.Bot, Bot))
        + (DIndex.WPar * DIndex.Z(c.Par - (2 * c.K), ParExcess))
        + (DIndex.WBranching * DIndex.Z(c.Branching, Branching))
        + (DIndex.WTraps * DIndex.Z(c.Traps, Traps))
        + (DIndex.WNarrow * (c.FreeEmpties == 1 ? 1 : 0));

    /// <summary>Cuantil de rango medio de <paramref name="d"/> en el pool del rango.</summary>
    public double Quantile(double d)
    {
        if (SortedD.Length == 0)
        {
            return 0.5;
        }

        int below = 0;
        int equal = 0;
        foreach (double x in SortedD)
        {
            if (x < d - 1e-12)
            {
                below++;
            }
            else if (Math.Abs(x - d) <= 1e-12)
            {
                equal++;
            }
        }

        return (below + (0.5 * equal)) / SortedD.Length;
    }
}

internal static class Pool
{
    public static double ContinuousBand(double q) =>
        q <= 0.10 ? q / 0.10
        : q <= 0.70 ? 1 + ((q - 0.10) / 0.60)
        : q <= 0.95 ? 2 + ((q - 0.70) / 0.25)
        : 3 + Math.Min(1, (q - 0.95) / 0.05);

    /// <summary>Banda entera (0 recuperación, 1 comodidad, 2 reto, 3 hito) del cuantil (03 §7).</summary>
    public static int BandIndex(double q) => q <= 0.10 ? 0 : q <= 0.70 ? 1 : q <= 0.95 ? 2 : 3;

    public static string BandName(int band) => band switch
    {
        0 => "recuperacion",
        1 => "comodidad",
        2 => "reto",
        _ => "hito",
    };

    public static Dictionary<RangeInfo, RangeStats> Compute(CampaignSpec spec, IReadOnlyList<SlotResult> slots)
    {
        var result = new Dictionary<RangeInfo, RangeStats>();
        foreach (RangeInfo range in spec.Ranges)
        {
            List<SlotResult> inRange = slots.Where(s => s.Plan.Range == range).ToList();
            if (inRange.Count == 0)
            {
                continue;
            }

            List<Candidate> pool = inRange.SelectMany(s => s.Candidates.Where(c => c.PoolMember)).ToList();
            var stats = new RangeStats
            {
                Range = range,
                Bot = DIndex.Stats(pool.Select(c => c.Bot).ToList()),
                ParExcess = DIndex.Stats(pool.Select(c => (double)(c.Par - (2 * c.K))).ToList()),
                Branching = DIndex.Stats(pool.Select(c => c.Branching).ToList()),
                Traps = DIndex.Stats(pool.Select(c => c.Traps).ToList()),
                SortedD = [],
                PoolSize = pool.Count,
                Slots = inRange.Count,
            };
            double[] ds = pool.Select(stats.D).Order().ToArray();
            result[range] = new RangeStats
            {
                Range = range,
                Bot = stats.Bot,
                ParExcess = stats.ParExcess,
                Branching = stats.Branching,
                Traps = stats.Traps,
                SortedD = ds,
                PoolSize = pool.Count,
                Slots = inRange.Count,
            };
        }

        return result;
    }

    /// <summary>Calcula D y el cuantil de los candidatos medidos del slot con los estadísticos (congelados) de su rango.</summary>
    public static void Score(SlotResult slot, RangeStats stats)
    {
        foreach (Candidate c in slot.Candidates.Where(c => c.Measured && c.Reject is null))
        {
            c.D = stats.D(c);
            c.Quantile = stats.Quantile(c.D);
        }
    }
}
