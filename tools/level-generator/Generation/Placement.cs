// Colocación de los candidatos en el ciclo de 20 de D-009 (propuesta c.3, reglas del escalón 1-7; 03 §12) con una
// programación dinámica por ciclo. Restricciones duras (las comprueba también tools/level-qa):
//   - tras cada nivel con etiqueta, el siguiente baja al menos una banda entera de D (y su D es menor);
//   - escalera desde el 61: D(p6) ≤ D(p9) ≤ D(p12) ≤ D(p15) ≤ D(p18) < D(p20);
//   - escalera en 41-59: D(p6) ≤ D(p12) ≤ D(p18) < D(p20), con D(p9) < D(p6) y D(p15) < D(p12);
//   - anti-repetición (03 §17 #9): forma distinta de la de los 200 niveles anteriores y Hamming ≥ 25 % con los 20 anteriores.
// Blandas (penalizan y se registran): salto entre niveles consecutivos de +2,2 / −3,4 bandas continuas (03 §12).
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
        Func<Candidate, bool> excluded, bool strictBand = true)
    {
        int n = block.Count;
        var options = new List<Candidate>[n];
        for (int i = 0; i < n; i++)
        {
            // El pool en ventana primero; después los candidatos de relajación medidos (fuera de la ventana), que la
            // programación dinámica solo usa si hace falta (penalización OutOfWindowPenalty).
            options[i] = block[i].Candidates.Where(c => c.PoolMember && !excluded(c))
                .Concat(block[i].Candidates.Where(c => c.Measured && c.Reject is null && !c.InWindow && !excluded(c)))
                .ToList();

            if (options[i].Count == 0)
            {
                return null;
            }
        }

        // Estado: (candidato elegido en el nivel i, último reto de la escalera o null). Coste: penalización blanda.
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
                Candidate? ladderCand = LadderCandidate(block, options, ladder);
                for (int c = 0; c < options[i].Count; c++)
                {
                    Candidate cand = options[i][c];
                    if (!Transition(prev, cand, strictBand) || !LadderOk(spec, plan, cand, ladderCand))
                    {
                        continue;
                    }

                    int newLadder = IsLadderSlot(spec, plan) ? Encode(i, c) : ladder;
                    double newCost = cost + JumpPenalty(prev, cand) + (BandDropMissing(prev, cand) ? BandPenalty : 0)
                        + (cand.InWindow ? 0 : OutOfWindowPenalty + (10 * plan.Window.Distance(cand.Bot))) + (c * 1e-6);
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

    private static int Encode(int slot, int cand) => (slot * 1024) + cand;

    private static Candidate? LadderCandidate(IReadOnlyList<SlotResult> block, List<Candidate>[] options, int ladder) =>
        ladder < 0 ? null : options[ladder / 1024][ladder % 1024];

    /// <summary>Los slots que entran en la escalera: retos con etiqueta posible (41-59: p6, p12, p18; 61+: los cinco).</summary>
    public static bool IsLadderSlot(CampaignSpec spec, LevelPlan plan) =>
        plan.Level > spec.FirstHard && plan.Role == SlotRole.Challenge && plan.AllowedLabel is not null;

    private static bool LadderOk(CampaignSpec spec, LevelPlan plan, Candidate cand, Candidate? last)
    {
        if (plan.Level <= spec.FirstHard || last is null)
        {
            return true;
        }

        double d = Rounded(cand.D);
        double l = Rounded(last.D);
        if (plan.Role == SlotRole.Milestone)
        {
            return d > l;
        }

        if (plan.Role != SlotRole.Challenge)
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
