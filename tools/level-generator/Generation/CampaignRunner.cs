// --spec: generación de la campaña (GDD 03 §2: generador → solver → métricas → índice D → colocación en el ciclo de 20 →
// alivio → JSON). Fases:
//   1. candidatos de cada slot en paralelo (cada slot depende solo de su semilla: el resultado no depende de los hilos);
//   2. estadísticos del pool por rango de 03 §11 (congelados) e índice D;
//   3. colocación por bloque (rampa 11-20 y ciclos de 20) con la programación dinámica de Placement; si un ciclo no tiene
//      asignación, se piden más candidatos a sus slots (hasta 3 rondas);
//   4. alivio (relief) de los niveles colocados desde el 31;
//   5. un pack por mundo (PackWriter) y el informe (CampaignReport), o --check: comparación byte a byte con el disco.
// D depende del pool de cada rango: un pack solo se reproduce generando los mismos --levels (rangos completos).
using System.Diagnostics;
using System.Globalization;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal sealed class GeneratedLevel
{
    public required LevelPlan Plan { get; init; }
    public required SlotResult Slot { get; set; }
    public Candidate? Chosen { get; set; }
    public List<ReliefBoard> Relief { get; set; } = [];
    public List<ReliefBoard> ReliefRejected { get; set; } = [];
    public List<string> Notes { get; } = [];
    public int Rounds { get; set; }
}

internal static class CampaignRunner
{
    public const int MaxExtraRounds = 2;

    public static int Run(string specPath, string levelsArg, string outDir, string? reportPath, bool check)
    {
        var total = Stopwatch.StartNew();
        CampaignSpec spec;
        try
        {
            spec = CampaignSpec.Load(specPath);
        }
        catch (CampaignSpecException e)
        {
            Console.Error.WriteLine(AsciiText.Of($"level-generator: {e.Message}"));
            return 2;
        }

        List<SpecIssue> issues = SpecValidator.Validate(spec);
        if (issues.Any(i => !i.Warning))
        {
            Console.Error.WriteLine("level-generator: la especificacion no pasa --validate-spec; corrigela antes de generar.");
            return 1;
        }

        if (!TryParseLevels(levelsArg, spec, out int first, out int last))
        {
            Console.Error.WriteLine($"level-generator: --levels debe ser a-b dentro de {spec.FirstLevel}-{spec.LastLevel}: {levelsArg}");
            return 2;
        }

        List<LevelPlan> plans = Enumerable.Range(first, last - first + 1).Select(spec.Plan).ToList();
        var levels = plans.Select(p => new GeneratedLevel { Plan = p, Slot = null! }).ToList();
        Console.WriteLine($"campana {spec.SpecId}: niveles {first}-{last} ({levels.Count}), {Environment.ProcessorCount} hilos");

        // Fase 1: candidatos.
        var phase = Stopwatch.StartNew();
        var generators = new ThreadLocal<SlotGenerator>(() => new SlotGenerator(spec), trackAllValues: false);
        Parallel.ForEach(levels, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, level =>
        {
            var sw = Stopwatch.StartNew();
            SlotGenerator gen = generators.Value!;
            SlotResult slot = gen.Generate(level.Plan);
            if (level.Plan.Mechanics.Any(m => m.Code == "Tu") && !slot.Candidates.Any(c => c.PoolMember))
            {
                // T1: si ningún candidato con turbias mide Difícil o Muy difícil, se publica sin turbias y se registra.
                SlotResult plain = gen.Generate(level.Plan, firstAttempt: 2000, dropMurky: true);
                plain.Notes.Insert(0, "sin Aguas turbias: ningun candidato turbio entro en la ventana");
                slot = plain;
            }

            level.Slot = slot;
            Console.WriteLine(AsciiText.Of(string.Create(CultureInfo.InvariantCulture,
                $"  nivel {level.Plan.Level,3} {level.Plan.Cell.Short,-18} {level.Plan.RoleName,-12} {slot.Candidates.Count(c => c.PoolMember)} en ventana de {slot.Attempts} intentos ({sw.Elapsed.TotalSeconds:0.0} s)")));
        });
        Console.WriteLine($"fase 1 (candidatos): {phase.Elapsed.TotalSeconds:0.0} s");

        // Fase 2: estadísticos del pool (congelados) y D.
        Dictionary<RangeInfo, RangeStats> stats = Pool.Compute(spec, levels.Select(l => l.Slot).ToList());
        foreach (GeneratedLevel l in levels)
        {
            Pool.Score(l.Slot, stats[l.Plan.Range]);
        }

        // Fase 3: colocación por bloques.
        phase.Restart();
        var history = new List<Candidate>();
        foreach (List<GeneratedLevel> block in Blocks(spec, levels))
        {
            var excluded = new HashSet<Candidate>();
            Dictionary<int, Candidate>? placed = null;
            for (int round = 0; round <= MaxExtraRounds; round++)
            {
                for (int guard = 0; guard < 50; guard++)
                {
                    placed = Placement.PlaceBlock(spec, block.Select(b => b.Slot).ToList(), history.LastOrDefault(), history, excluded.Contains);
                    if (placed is null)
                    {
                        break;
                    }

                    Candidate? repeated = RepetitionViolation(block, placed, history);
                    if (repeated is null)
                    {
                        break;
                    }

                    excluded.Add(repeated);
                    placed = null;
                }

                // Colocado, pero con un reto o el hito fuera de su ventana (se publicaría sin la etiqueta objetivo, c.2): una ronda
                // más solo para esos slots, por si aparece un candidato en la ventana que la escalera admita.
                List<GeneratedLevel> missed = placed is null ? [] : block.Where(l => !placed[l.Plan.Level].InWindow
                    && l.Plan.Role is SlotRole.Challenge or SlotRole.Milestone && l.Rounds <= round).ToList();
                if ((placed is not null && missed.Count == 0) || round == MaxExtraRounds)
                {
                    break;
                }

                Console.WriteLine(placed is null
                    ? $"  ciclo {block[0].Plan.Level}-{block[^1].Plan.Level}: sin colocacion valida; ronda extra {round + 1}"
                    : $"  ciclo {block[0].Plan.Level}-{block[^1].Plan.Level}: {missed.Count} reto(s) fuera de ventana; ronda extra {round + 1}");
                // Más candidatos para los slots que deciden la escalera (retos e hito) y para los que siguen a un reto o al hito.
                var extra = placed is not null ? missed : block.Where(l => !l.Plan.Ramp && (l.Plan.Role is SlotRole.Challenge or SlotRole.Milestone
                    || block.Any(o => o.Plan.Level == l.Plan.Level - 1 && o.Plan.Role is SlotRole.Challenge or SlotRole.Milestone))).ToList();
                Dictionary<int, Candidate>? previousPlacement = placed;
                Parallel.ForEach(extra, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, l =>
                {
                    SlotResult more = generators.Value!.Generate(l.Plan, firstAttempt: 1000 * (round + 1), dropMurky: l.Slot.MurkyDropped);
                    Pool.Score(more, stats[l.Plan.Range]);
                    l.Slot.Candidates.AddRange(more.Candidates);
                    l.Rounds = round + 1;
                });

                if (previousPlacement is not null)
                {
                    // Se vuelve a colocar con los candidatos nuevos; si no mejora, se queda la colocación anterior.
                    Dictionary<int, Candidate>? retry = Placement.PlaceBlock(spec, block.Select(b => b.Slot).ToList(), history.LastOrDefault(), history, excluded.Contains);
                    placed = retry is not null && RepetitionViolation(block, retry, history) is null ? retry : previousPlacement;
                    break;
                }
            }

            if (placed is null)
            {
                // Último recurso: la bajada de banda tras una etiqueta pasa a penalización (D menor sigue siendo obligatorio) y
                // cada incumplimiento queda en el informe.
                for (int guard = 0; guard < 50; guard++)
                {
                    placed = Placement.PlaceBlock(spec, block.Select(b => b.Slot).ToList(), history.LastOrDefault(), history, excluded.Contains, strictBand: false);
                    if (placed is null)
                    {
                        break;
                    }

                    Candidate? repeated = RepetitionViolation(block, placed, history);
                    if (repeated is null)
                    {
                        break;
                    }

                    excluded.Add(repeated);
                    placed = null;
                }

                if (placed is not null)
                {
                    Console.WriteLine($"  ciclo {block[0].Plan.Level}-{block[^1].Plan.Level}: colocado sin exigir la bajada de banda tras cada etiqueta (registrado)");
                }
            }

            if (placed is null)
            {
                Console.Error.WriteLine($"level-generator: el bloque {block[0].Plan.Level}-{block[^1].Plan.Level} no tiene colocacion que cumpla el escalon de D-009");
                foreach (GeneratedLevel l in block)
                {
                    var opts = l.Slot.Candidates.Where(c => c.PoolMember).ToList();
                    Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture,
                        $"  {l.Plan.Level} {l.Plan.RoleName,-12} pool {opts.Count}: {string.Join(" ", opts.Select(c => $"{c.MeasuredLabel[0]}/D{Placement.Rounded(c.D):0.00}/q{c.Quantile:0.00}"))}"));
                }

                return 1;
            }

            foreach (GeneratedLevel l in block)
            {
                Candidate? prev = history.LastOrDefault();
                l.Chosen = placed[l.Plan.Level];
                if (Placement.BandDropMissing(prev, l.Chosen))
                {
                    l.Notes.Add("tras una etiqueta no baja una banda entera de D (solo baja D)");
                }

                history.Add(l.Chosen);
            }
        }

        Console.WriteLine($"fase 3 (colocacion): {phase.Elapsed.TotalSeconds:0.0} s");

        // Fase 4: alivio.
        phase.Restart();
        Parallel.ForEach(levels.Where(l => l.Plan.Level >= 31), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, l =>
        {
            Candidate c = l.Chosen!;
            (List<ReliefBoard> published, List<ReliefBoard> rejected) = Relief.Compute(generators.Value!.Metrics, c.Board, c.Par, c.Bot, l.Plan.SeedText);
            l.Relief = published;
            l.ReliefRejected = rejected;
        });
        Console.WriteLine($"fase 4 (alivio): {phase.Elapsed.TotalSeconds:0.0} s");
        List<GeneratedLevel> noRelief = levels.Where(l => l.Plan.Level >= 31 && l.Relief.Count == 0).ToList();
        foreach (GeneratedLevel l in noRelief)
        {
            Console.Error.WriteLine($"level-generator: el nivel {l.Plan.Level} no tiene ninguna variante de alivio que cumpla el criterio de 03 sec. 17 #8");
        }

        // Fase 5: packs e informe.
        bool differs = false;
        var packs = new List<(WorldInfo World, string Path, byte[] Bytes, int Levels)>();
        foreach (WorldInfo world in spec.Worlds)
        {
            List<GeneratedLevel> inWorld = levels.Where(l => l.Plan.World == world).ToList();
            if (inWorld.Count == 0)
            {
                continue;
            }

            if (inWorld.Count != world.Last - world.First + 1)
            {
                Console.WriteLine($"  aviso: el mundo {world.World} queda incompleto ({inWorld.Count} niveles)");
            }

            string text = PackWriter.Write(spec, world, inWorld.Select(l => new PackLevel(l.Plan, l.Chosen!, l.Relief)).ToList());
            byte[] bytes = PackWriter.Bytes(text);
            string path = Path.Combine(outDir, PackWriter.PackId(world, spec.Lap) + ".json");
            packs.Add((world, path, bytes, inWorld.Count));
            if (check)
            {
                byte[]? onDisk = File.Exists(path) ? File.ReadAllBytes(path) : null;
                bool same = onDisk is not null && onDisk.AsSpan().SequenceEqual(bytes);
                differs |= !same;
                Console.WriteLine($"--check: {path.Replace('\\', '/')} {(same ? "coincide" : "NO coincide")} con el generado");
            }
            else if (noRelief.Count == 0)
            {
                Directory.CreateDirectory(outDir);
                File.WriteAllBytes(path, bytes);
                Console.WriteLine($"pack: {path.Replace('\\', '/')} ({bytes.Length} bytes, {inWorld.Count} niveles)");
            }
        }

        if (reportPath is not null && !check)
        {
            string? dir = Path.GetDirectoryName(Path.GetFullPath(reportPath));
            if (dir is not null)
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(reportPath, PackWriter.Bytes(CampaignReport.Build(spec, levels, stats, packs, levelsArg)));
            Console.WriteLine($"informe: {reportPath.Replace('\\', '/')}");
        }

        Console.WriteLine($"tiempo total: {total.Elapsed.TotalSeconds:0.0} s");
        return noRelief.Count == 0 && !differs ? 0 : 1;
    }

    private static Candidate? RepetitionViolation(List<GeneratedLevel> block, Dictionary<int, Candidate> placed, List<Candidate> history)
    {
        var seq = new List<Candidate>(history);
        foreach (GeneratedLevel l in block)
        {
            Candidate c = placed[l.Plan.Level];
            string shape = Placement.Shape(c.Board);
            if (seq.TakeLast(Placement.ShapeWindow).Any(h => Placement.Shape(h.Board) == shape))
            {
                return c;
            }

            if (seq.TakeLast(Placement.HammingWindow).Any(h => Placement.Hamming(h.Board, c.Board) < Placement.MinHamming))
            {
                return c;
            }

            seq.Add(c);
        }

        return null;
    }

    /// <summary>Bloques de colocación: la rampa 11-20 y cada ciclo de 20 (o la parte pedida en --levels).</summary>
    private static IEnumerable<List<GeneratedLevel>> Blocks(CampaignSpec spec, List<GeneratedLevel> levels)
    {
        List<GeneratedLevel> ramp = levels.Where(l => l.Plan.Ramp).ToList();
        if (ramp.Count > 0)
        {
            yield return ramp;
        }

        foreach (IGrouping<int, GeneratedLevel> g in levels.Where(l => !l.Plan.Ramp).GroupBy(l => (l.Plan.Level - spec.CycleFrom) / 20))
        {
            yield return g.OrderBy(l => l.Plan.Level).ToList();
        }
    }

    private static bool TryParseLevels(string text, CampaignSpec spec, out int first, out int last)
    {
        first = last = 0;
        string[] parts = text.Split('-');
        return parts.Length == 2
            && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out first)
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out last)
            && first >= spec.FirstLevel && last <= spec.LastLevel && first <= last;
    }
}
