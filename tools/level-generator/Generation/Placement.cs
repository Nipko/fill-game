// Colocación de los candidatos en el ciclo de 20 de D-009 (propuesta c.3, reglas del escalón 1-7; 03 §12) con una
// programación dinámica por ciclo. Restricciones duras (las comprueba también tools/level-qa):
//   - ventana del slot: solo un reto o el hito pueden quedar fuera de ella (c.2; LevelPlan.OutOfWindowAllowed); en
//     recuperación, comodidad, estreno, remanso y rampa, nunca (03 §17 #3; 08 §4.1; QA-WINDOW);
//   - banda medida por rol (03 §7: «la banda coloca»; cuantil de D en el pool del rango; QA-BAND-Q):
//       · comodidad, estreno, remanso y rampa: como mucho banda de comodidad (q ≤ 0,70);
//       · hito (p20, «Pico» de c.3): banda de hito (q > 0,95) o, como mínimo, par − 2K ≥ la media del pool del rango;
//       · recuperación (p1-p2, c.3 regla 3): banda de recuperación (q ≤ 0,10) o, como mínimo, D por debajo de la comodidad
//         más fácil del ciclo (PlaceCycle);
//   - tras cada nivel con etiqueta, el siguiente baja al menos una banda entera de D (y su D es menor);
//   - escalera desde el 61: D(p6) ≤ D(p9) ≤ D(p12) ≤ D(p15) ≤ D(p18) < D(p20);
//   - escalera en 41-59: D(p6) ≤ D(p12) ≤ D(p18) < D(p20), con D(p9) < D(p6) y D(p15) < D(p12);
//   - en 21-40 (retos suaves sin etiqueta): todos los retos por debajo del hito 40, el primer Difícil;
//   - anti-repetición (03 §17 #9): forma distinta de la de los 200 niveles anteriores y Hamming ≥ 25 % con los 20 anteriores;
//   - remanso (p19) más fácil que la media de comodidad del ciclo (c.4; c.3 regla 5): D(p19) < media de D de las comodidades
//     que no son tableros de estreno (PlaceCycle).
// Blandas (penalizan y se registran): salto entre niveles consecutivos de +2,2 / −3,4 bandas continuas (03 §12); una
// recuperación fuera de su banda o un hito fuera de la suya (cuando cumplen el mínimo de arriba) penalizan fuerte.
// «Nunca dos etiquetados seguidos», el remanso antes del hito y las dos recuperaciones salen de los roles del calendario.
// D se compara redondeado a 2 decimales, como se publica.
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal sealed record PlacementResult(Dictionary<int, Candidate> Chosen, List<string> Violations, List<int> Infeasible);

internal static class Placement
{
    public const double MaxJumpUp = 2.2;
    public const double MaxJumpDown = 3.4;
    public const double MinHamming = BoardShape.MinHamming;
    public const int ShapeWindow = BoardShape.ShapeWindow;
    public const int HammingWindow = BoardShape.HammingWindow;

    public static double Rounded(double d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);

    public static bool Labeled(Candidate c) => c.MeasuredLabel != "normal";

    /// <summary>
    /// Coloca un bloque de niveles consecutivos (un ciclo o la rampa) después de <paramref name="previous"/> (el último nivel
    /// ya colocado, o null). Devuelve null si ninguna asignación cumple las restricciones duras.
    /// </summary>
    public static Dictionary<int, Candidate>? PlaceBlock(
        CampaignSpec spec, IReadOnlyList<SlotResult> block, Candidate? previous, IReadOnlyList<Candidate> history,
        Func<Candidate, bool> excluded, IReadOnlyDictionary<RangeInfo, RangeStats> stats, bool strictBand = true)
    {
        int n = block.Count;
        var options = new List<Candidate>[n];
        for (int i = 0; i < n; i++)
        {
            options[i] = Options(block[i], stats[block[i].Plan.Range], excluded);
            if (options[i].Count == 0)
            {
                return null;
            }
        }

        // Estado: (candidato elegido en el nivel i, reto de referencia de la escalera o null). Coste: penalización blanda.
        var layer = new Dictionary<(int Cand, int Ladder), (double Cost, (int Cand, int Ladder) Prev)>();
        var steps = new List<Dictionary<(int Cand, int Ladder), (double Cost, (int Cand, int Ladder) Prev)>>();
        for (int i = 0; i < n; i++)
        {
            LevelPlan plan = block[i].Plan;
            var next = new Dictionary<(int Cand, int Ladder), (double Cost, (int Cand, int Ladder) Prev)>();
            IEnumerable<((int Cand, int Ladder) Key, double Cost)> sources = i == 0
                ? [((-1, -1), 0.0)]
                : layer.OrderBy(kv => kv.Key.Cand).ThenBy(kv => kv.Key.Ladder).Select(kv => (kv.Key, kv.Value.Cost));
            foreach (((int prevCand, int ladder), double cost) in sources)
            {
                Candidate? prev = i == 0 ? previous : options[i - 1][prevCand];
                Candidate? ladderCand = LadderCandidate(options, ladder);
                for (int c = 0; c < options[i].Count; c++)
                {
                    Candidate cand = options[i][c];
                    if (!Transition(prev, cand, strictBand) || !LadderOk(spec, plan, cand, ladderCand))
                    {
                        continue;
                    }

                    int newLadder = NextLadder(spec, plan, ladder, ladderCand, cand, Encode(i, c));
                    double newCost = cost + JumpPenalty(prev, cand) + (BandDropMissing(prev, cand) ? BandPenalty : 0)
                        + (cand.InWindow ? 0 : OutOfWindowPenalty + (10 * plan.Window.Distance(cand.Bot)))
                        + RoleBandPenalty(plan, cand) + (c * 1e-6);
                    var key = (c, newLadder);
                    if (!next.TryGetValue(key, out var best) || newCost < best.Cost - 1e-12)
                    {
                        next[key] = (newCost, (i == 0 ? -1 : prevCand, ladder));
                    }
                }
            }

            if (next.Count == 0)
            {
                return null;
            }

            steps.Add(next);
            layer = next;
        }

        // Mejor final y reconstrucción.
        var end = layer.OrderBy(kv => kv.Value.Cost).ThenBy(kv => kv.Key.Cand).ThenBy(kv => kv.Key.Ladder).First();
        var chosen = new Dictionary<int, Candidate>();
        (int Cand, int Ladder) state = end.Key;
        for (int i = n - 1; i >= 0; i--)
        {
            chosen[block[i].Plan.Level] = options[i][state.Cand];
            state = steps[i][state].Prev;
        }

        // Anti-repetición: si choca con la historia o dentro del bloque, se excluye el candidato y se repite (lo hace el llamante).
        return chosen;
    }

    /// <summary>
    /// Candidatos que la colocación puede usar en el slot: el pool en ventana primero; después, solo en un reto o el hito
    /// (c.2), los candidatos de relajación medidos fuera de la ventana, que la programación dinámica solo usa si hace falta
    /// (penalización OutOfWindowPenalty). Todos cumplen la banda de su rol (<see cref="BandAdmissible"/>).
    /// </summary>
    public static List<Candidate> Options(SlotResult slot, RangeStats stats, Func<Candidate, bool> excluded)
    {
        LevelPlan plan = slot.Plan;
        IEnumerable<Candidate> relaxed = plan.OutOfWindowAllowed
            ? slot.Candidates.Where(c => c.Measured && c.Reject is null && !c.InWindow)
            : [];
        return slot.Candidates.Where(c => c.PoolMember).Concat(relaxed)
            .Where(c => !excluded(c) && BandAdmissible(plan, c, stats))
            .ToList();
    }

    /// <summary>
    /// Restricción de banda de un candidato que no depende del resto del ciclo (03 §7: «la banda coloca»): comodidad,
    /// estreno, remanso y rampa como mucho en banda de comodidad; el hito en banda de hito o con par − 2K ≥ la media del pool.
    /// La de la recuperación depende de las comodidades colocadas: la impone <see cref="PlaceCycle"/>.
    /// </summary>
    public static bool BandAdmissible(LevelPlan plan, Candidate c, RangeStats stats) => plan.Role switch
    {
        SlotRole.Comfort or SlotRole.Remanso => ComfortBandOk(c.Quantile),
        SlotRole.Milestone => MilestoneOk(c.Quantile, c.Par, c.K, stats),
        _ => true,
    };

    /// <summary>Comodidad, estreno, remanso y rampa: como mucho banda de comodidad (q ≤ 0,70; 03 §7).</summary>
    public static bool ComfortBandOk(double quantile) => Pool.BandIndex(quantile) <= 1;

    /// <summary>
    /// Hito (p20, «Pico» de c.3; 03 §7: hito > 0,95): banda de hito o, como mínimo, par − 2K ≥ la media de par − 2K del pool
    /// del rango (un hito corto que solo castiga la apertura no es el pico del ciclo).
    /// </summary>
    public static bool MilestoneOk(double quantile, int par, int k, RangeStats stats) =>
        Pool.BandIndex(quantile) == 3 || par - (2 * k) >= stats.ParExcess.Mean - 1e-9;

    /// <summary>
    /// Recuperación (p1-p2; c.3 regla 3: tras el hito, dos niveles de recuperación; 03 §7: recuperación ≤ 0,10): banda de
    /// recuperación o, como mínimo, D (redondeado) por debajo de la comodidad más fácil del ciclo (sin estrenos). Sin
    /// comodidades de referencia, solo vale la banda.
    /// </summary>
    public static bool RecoveryOk(double quantile, double roundedD, double? comfortMin) =>
        Pool.BandIndex(quantile) == 0 || (comfortMin is double m && roundedD < m - 1e-9);

    /// <summary>
    /// <see cref="PlaceBlock"/> con las restricciones que dependen de la colocación de todo el ciclo:
    ///   - recuperación (c.3 regla 3): <see cref="RecoveryOk"/> con la comodidad más fácil del ciclo. Se resuelve con un
    ///     umbral T: las recuperaciones fuera de la banda de recuperación tienen D ≤ T y todas las comodidades de referencia
    ///     D &gt; T. Primero sin ninguna recuperación fuera de su banda (T = −∞) y después con los D de las recuperaciones
    ///     candidatas en orden creciente: gana el primer T con colocación (la recuperación más fácil posible);
    ///   - remanso (propuesta c.4: p19 «puro, más fácil que la media de comodidad»; c.3 regla 5: respiro antes del pico):
    ///     D(p19) &lt; media de D de las comodidades del ciclo que no son tableros de estreno. La media depende de la
    ///     colocación: se coloca, se comprueba y se excluyen del p19 los candidatos con D ≥ media hasta que se cumple.
    /// Null si ningún umbral tiene colocación (el llamante pide más candidatos).
    /// </summary>
    public static Dictionary<int, Candidate>? PlaceCycle(
        CampaignSpec spec, IReadOnlyList<SlotResult> block, Candidate? previous, IReadOnlyList<Candidate> history,
        Func<Candidate, bool> excluded, IReadOnlyDictionary<RangeInfo, RangeStats> stats, bool strictBand = true)
    {
        var recoveryLevels = block.Where(s => !s.Plan.Ramp && s.Plan.Role == SlotRole.Recovery).Select(s => s.Plan.Level).ToHashSet();
        var comfortLevels = block.Where(s => s.Plan.IsComfortReference).Select(s => s.Plan.Level).ToHashSet();
        var thresholds = new List<double?> { null };
        if (comfortLevels.Count > 0)
        {
            thresholds.AddRange(block.Where(s => recoveryLevels.Contains(s.Plan.Level))
                .SelectMany(s => Options(s, stats[s.Plan.Range], excluded))
                .Where(c => Pool.BandIndex(c.Quantile) > 0)
                .Select(c => Rounded(c.D)).Distinct().Order().Select(d => (double?)d));
        }

        foreach (double? t in thresholds)
        {
            bool OutOfThreshold(Candidate c) =>
                (recoveryLevels.Contains(c.Level) && Pool.BandIndex(c.Quantile) > 0 && (t is not double r || Rounded(c.D) > r + 1e-9))
                || (t is double u && comfortLevels.Contains(c.Level) && Rounded(c.D) <= u + 1e-9);

            Dictionary<int, Candidate>? placed = PlaceWithRemanso(spec, block, previous, history, c => excluded(c) || OutOfThreshold(c), stats, strictBand);
            if (placed is not null)
            {
                return placed;
            }
        }

        return null;
    }

    private static Dictionary<int, Candidate>? PlaceWithRemanso(
        CampaignSpec spec, IReadOnlyList<SlotResult> block, Candidate? previous, IReadOnlyList<Candidate> history,
        Func<Candidate, bool> excluded, IReadOnlyDictionary<RangeInfo, RangeStats> stats, bool strictBand)
    {
        SlotResult? remanso = block.FirstOrDefault(s => s.Plan.Role == SlotRole.Remanso);
        var tooHard = new HashSet<Candidate>();
        for (int guard = 0; guard < 256; guard++)
        {
            Dictionary<int, Candidate>? placed = PlaceBlock(spec, block, previous, history, c => excluded(c) || tooHard.Contains(c), stats, strictBand);
            if (placed is null || remanso is null)
            {
                return placed;
            }

            double? mean = ComfortMean(block.Select(s => s.Plan), placed);
            if (mean is not double m || Rounded(placed[remanso.Plan.Level].D) < m - 1e-9)
            {
                return placed;
            }

            foreach (Candidate c in remanso.Candidates.Where(c => Rounded(c.D) >= m - 1e-9))
            {
                tooHard.Add(c);
            }
        }

        return null;
    }

    /// <summary>
    /// Media de D (redondeado como se publica) de las comodidades del bloque que no son tableros de estreno (c.4), o null si no
    /// hay ninguna. La misma regla la aplica tools/level-qa (QA-REMANSO) sobre el pack.
    /// </summary>
    public static double? ComfortMean(IEnumerable<LevelPlan> plans, IReadOnlyDictionary<int, Candidate> placed)
    {
        List<double> ds = ComfortDs(plans, placed);
        return ds.Count == 0 ? null : ds.Average();
    }

    /// <summary>D (redondeado) de la comodidad más fácil del bloque sin estrenos, o null (recuperación; QA-BAND-Q).</summary>
    public static double? ComfortMin(IEnumerable<LevelPlan> plans, IReadOnlyDictionary<int, Candidate> placed)
    {
        List<double> ds = ComfortDs(plans, placed);
        return ds.Count == 0 ? null : ds.Min();
    }

    private static List<double> ComfortDs(IEnumerable<LevelPlan> plans, IReadOnlyDictionary<int, Candidate> placed) =>
        plans.Where(p => p.IsComfortReference && placed.ContainsKey(p.Level)).Select(p => Rounded(placed[p.Level].D)).ToList();

    private static int Encode(int slot, int cand) => (slot * 1024) + cand;

    private static Candidate? LadderCandidate(List<Candidate>[] options, int ladder) =>
        ladder < 0 ? null : options[ladder / 1024][ladder % 1024];

    /// <summary>Ciclo 21-40: retos suaves sin etiqueta (03 §12; c.3 regla 7: 40 = primer Difícil) y sin escalera.</summary>
    public static bool SoftCycle(CampaignSpec spec, LevelPlan plan) => !plan.Ramp && plan.Level <= spec.FirstHard;

    /// <summary>
    /// Los slots que entran en la escalera: retos con etiqueta posible (41-59: p6, p12, p18; 61+: los cinco) y, en 21-40,
    /// todos los retos (solo para que el hito 40 quede por encima de todos).
    /// </summary>
    public static bool IsLadderSlot(CampaignSpec spec, LevelPlan plan) =>
        plan.Role == SlotRole.Challenge && (SoftCycle(spec, plan) || (plan.Level > spec.FirstHard && plan.AllowedLabel is not null));

    /// <summary>
    /// Reto de referencia tras colocar <paramref name="cand"/>: desde el 41, el último reto de la escalera (que es el más
    /// difícil, porque la escalera no baja); en 21-40, el reto de D más alto hasta ahí.
    /// </summary>
    private static int NextLadder(CampaignSpec spec, LevelPlan plan, int ladder, Candidate? ladderCand, Candidate cand, int encoded)
    {
        if (!IsLadderSlot(spec, plan))
        {
            return ladder;
        }

        return SoftCycle(spec, plan) && ladderCand is not null && Rounded(ladderCand.D) >= Rounded(cand.D) ? ladder : encoded;
    }

    private static bool LadderOk(CampaignSpec spec, LevelPlan plan, Candidate cand, Candidate? last)
    {
        if (last is null || plan.Ramp)
        {
            return true;
        }

        double d = Rounded(cand.D);
        double l = Rounded(last.D);
        if (plan.Role == SlotRole.Milestone)
        {
            // Desde el 41, por encima del último reto de la escalera; en el 40, por encima de todos los retos suaves de 21-39.
            return d > l;
        }

        if (plan.Role != SlotRole.Challenge || SoftCycle(spec, plan))
        {
            return true;
        }

        // Retos suaves de 41-59 (p9, p15): por debajo del reto etiquetado anterior.
        return plan.AllowedLabel is null ? d < l : d >= l;
    }

    /// <summary>Penalización de un nivel tras una etiqueta que no baja una banda entera (modo no estricto).</summary>
    public const double BandPenalty = 10;

    /// <summary>Penalización de un candidato de relajación fuera de la ventana del slot (se registra en el informe).</summary>
    public const double OutOfWindowPenalty = 20;

    /// <summary>
    /// Penalización fuerte de una recuperación fuera de la banda de recuperación o de un hito fuera de la banda de hito cuando
    /// solo cumplen el mínimo de <see cref="RecoveryOk"/> o <see cref="MilestoneOk"/>, más la distancia en bandas continuas.
    /// </summary>
    public const double RoleBandMissPenalty = 15;

    private static double RoleBandPenalty(LevelPlan plan, Candidate cand)
    {
        if (plan.Ramp)
        {
            return 0;
        }

        double band = Pool.ContinuousBand(cand.Quantile);
        return plan.Role switch
        {
            SlotRole.Recovery when Pool.BandIndex(cand.Quantile) > 0 => RoleBandMissPenalty + (band - 1),
            SlotRole.Milestone when Pool.BandIndex(cand.Quantile) < 3 => RoleBandMissPenalty + (3 - band),
            _ => 0,
        };
    }

    /// <summary>
    /// Tras un nivel con etiqueta, el siguiente tiene D menor (siempre) y baja al menos una banda entera (en modo estricto;
    /// si ninguna colocación lo logra, el modo no estricto lo penaliza y el informe lo registra) [P].
    /// </summary>
    public static bool Transition(Candidate? prev, Candidate cand, bool strictBand = true)
    {
        if (prev is null || !Labeled(prev))
        {
            return true;
        }

        return Rounded(cand.D) < Rounded(prev.D) && (!strictBand || !BandDropMissing(prev, cand));
    }

    public static bool BandDropMissing(Candidate? prev, Candidate cand) =>
        prev is not null && Labeled(prev) && Pool.BandIndex(cand.Quantile) > Pool.BandIndex(prev.Quantile) - 1;

    private static double JumpPenalty(Candidate? prev, Candidate cand)
    {
        if (prev is null)
        {
            return 0;
        }

        double jump = Pool.ContinuousBand(cand.Quantile) - Pool.ContinuousBand(prev.Quantile);
        return jump > MaxJumpUp ? jump - MaxJumpUp : jump < -MaxJumpDown ? -MaxJumpDown - jump : 0;
    }

    public static string? JumpViolation(Candidate? prev, Candidate cand)
    {
        if (prev is null)
        {
            return null;
        }

        double jump = Pool.ContinuousBand(cand.Quantile) - Pool.ContinuousBand(prev.Quantile);
        return jump > MaxJumpUp ? $"sube {jump:0.00} bandas (> {MaxJumpUp})"
            : jump < -MaxJumpDown ? $"baja {-jump:0.00} bandas (> {MaxJumpDown})"
            : null;
    }

    /// <summary>Forma del tablero (03 §3; ver BoardShape).</summary>
    public static string Shape(BoardData board) =>
        BoardShape.Shape(board.Caps, board.Cuias, board.Attrs.Select(a => (int)a.Type).ToArray());

    /// <summary>Hamming entre dos tableros (03 §17 #9; ver BoardShape).</summary>
    public static double Hamming(BoardData a, BoardData b) => BoardShape.Hamming(a.Cuias, b.Cuias);
}
