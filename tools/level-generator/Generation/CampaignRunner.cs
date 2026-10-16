// --spec: generación de la campaña (GDD 03 §2: generador → solver → métricas → índice D → colocación en el ciclo de 20 →
// alivio → JSON). Fases:
//   1. candidatos de cada slot en paralelo (cada slot depende solo de su semilla: el resultado no depende de los hilos);
//   2. estadísticos del pool por rango de 03 §11 (congelados) e índice D;
//   3. colocación por bloque (rampa 11-20 y ciclos de 20) con la programación dinámica de Placement, la banda medida de cada
//      rol (03 §7) y el remanso más fácil que la media de comodidad (c.4). Antes de colocar, los slots sin ningún candidato
//      de la banda de su rol (recuperación ≤ 0,10, comodidad ≤ 0,70, hito > 0,95) piden más candidatos (hasta BandRounds
//      rondas); si un ciclo no tiene asignación, se piden más a sus slots (hasta 2 rondas);
//   4. alivio (relief) de los niveles colocados desde el 31;
//   5. un pack por mundo (PackWriter) y el informe (CampaignReport) y, con --curation, el de curación para el dueño
//      (CurationReport, P7a; con --decisions, la decisión del dueño de CurationDecisions), o --check: comparación byte a byte con el disco.
// D depende del pool de cada rango: un pack solo se reproduce generando los mismos --levels (rangos completos).
// Generate() hace las fases 1-5 en memoria; la usa también tools/level-qa para comprobar la reproducibilidad (03 §17 #10).
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

    /// <summary>Rondas de banda que pidió el slot antes de colocar (CampaignRunner.BandRounds).</summary>
    public int BandRounds { get; set; }
}

/// <summary>Un pack generado en memoria (fase 5).</summary>
internal sealed record GeneratedPack(WorldInfo World, string PackId, byte[] Bytes, int Levels);

/// <summary>Resultado de <see cref="CampaignRunner.Generate"/>: niveles colocados, estadísticos del pool y packs.</summary>
internal sealed record CampaignResult(
    List<GeneratedLevel> Levels, Dictionary<RangeInfo, RangeStats> Stats, List<GeneratedPack> Packs, List<GeneratedLevel> NoRelief);

internal static class CampaignRunner
{
    public const int MaxExtraRounds = 2;

    /// <summary>
    /// Rondas de rescate [P] (P7b): si un ciclo sigue sin colocación tras las <see cref="MaxExtraRounds"/> rondas extra y el
    /// último recurso (la bajada de banda tras una etiqueta como penalización), más candidatos para los mismos slots (retos,
    /// hito, recuperaciones y los que siguen a un reto o al hito), hasta este número de rondas, con la colocación estricta y la
    /// del último recurso tras cada una. Solo corren en un ciclo que antes no se generaba (salía con 1), así que los packs que
    /// ya se generaban no cambian (--check de 11-300).
    /// </summary>
    public const int RescueRounds = 6;

    /// <summary>Primer intento de las rondas de rescate (las extra usan 1000-2999, las turbias 10000-16999 y las de banda 20000-25999).</summary>
    public const int RescueRoundFirstAttempt = 30000;

    /// <summary>Rondas de candidatos para los slots sin ningún candidato de la banda de su rol (antes de colocar) [P].</summary>
    public const int BandRounds = 6;

    /// <summary>Primer intento de las rondas de banda (las de la colocación usan 1000-2999 y las turbias 11000-16999).</summary>
    public const int BandRoundFirstAttempt = 20000;

    /// <summary>Candidatos en ventana de más que pide cada ronda de banda.</summary>
    public const int BandRoundExtraTarget = 6;

    /// <summary>Rondas extra de candidatos con Aguas turbias antes del respaldo de T1 (publicar sin turbias) [P].</summary>
    public const int MurkyExtraRounds = 6;

    /// <summary>Primer intento de las rondas turbias extra (las de la colocación usan 1000-2999).</summary>
    public const int MurkyRoundFirstAttempt = 10000;

    /// <summary>
    /// Modo --spec: genera los niveles <paramref name="levelsArg"/> (null = todos los de la especificación, first_level-last_level)
    /// con la variante <paramref name="variant"/> (null = la publicada; «b» con los respaldos de "fallback", salida B de D-019).
    /// </summary>
    public static int Run(string specPath, string? levelsArg, string outDir, string? reportPath, bool check, string? variant = null, string? curationPath = null,
        string? decisionsPath = null)
    {
        var total = Stopwatch.StartNew();
        CampaignSpec spec;
        try
        {
            spec = CampaignSpec.Load(specPath, variant);
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

        // Decisiones del dueño para el informe de curación (--decisions): se leen antes de generar para fallar pronto.
        CurationDecisions? decisions = null;
        if (decisionsPath is not null)
        {
            try
            {
                decisions = CurationDecisions.Load(decisionsPath, specPath, spec.FirstLevel, spec.LastLevel);
            }
            catch (CurationDecisionsException e)
            {
                Console.Error.WriteLine(AsciiText.Of($"level-generator: {e.Message}"));
                return 2;
            }
        }

        levelsArg ??= string.Create(CultureInfo.InvariantCulture, $"{spec.FirstLevel}-{spec.LastLevel}");
        if (!TryParseLevels(levelsArg, spec, out int first, out int last))
        {
            Console.Error.WriteLine($"level-generator: --levels debe ser a-b dentro de {spec.FirstLevel}-{spec.LastLevel}: {levelsArg}");
            return 2;
        }

        CampaignResult? result = Generate(spec, first, last, Console.WriteLine, verbose: true);
        if (result is null)
        {
            return 1;
        }

        if (decisions is not null)
        {
            string where = decisions.Path.Replace('\\', '/');
            (List<string> errors, List<string> stale) = CurationReport.Check(decisions, result.Levels);
            foreach (string msg in stale)
            {
                Console.WriteLine(AsciiText.Of($"aviso: {where}: {msg}"));
            }

            if (errors.Count > 0)
            {
                foreach (string msg in errors)
                {
                    Console.Error.WriteLine(AsciiText.Of($"level-generator: {where}: {msg}"));
                }

                return 2;
            }
        }

        // Fase 5: packs e informe.
        bool differs = false;
        var packs = new List<(WorldInfo World, string Path, byte[] Bytes, int Levels)>();
        foreach (GeneratedPack pack in result.Packs)
        {
            string path = Path.Combine(outDir, pack.PackId + ".json");
            packs.Add((pack.World, path, pack.Bytes, pack.Levels));
            if (check)
            {
                byte[]? onDisk = File.Exists(path) ? File.ReadAllBytes(path) : null;
                bool same = onDisk is not null && onDisk.AsSpan().SequenceEqual(pack.Bytes);
                differs |= !same;
                Console.WriteLine($"--check: {path.Replace('\\', '/')} {(same ? "coincide" : "NO coincide")} con el generado");
            }
            else if (result.NoRelief.Count == 0)
            {
                Directory.CreateDirectory(outDir);
                File.WriteAllBytes(path, pack.Bytes);
                Console.WriteLine($"pack: {path.Replace('\\', '/')} ({pack.Bytes.Length} bytes, {pack.Levels} niveles)");
            }
        }

        if (reportPath is not null && !check)
        {
            string? dir = Path.GetDirectoryName(Path.GetFullPath(reportPath));
            if (dir is not null)
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(reportPath, PackWriter.Bytes(CampaignReport.Build(spec, result.Levels, result.Stats, packs, levelsArg, reportPath)));
            Console.WriteLine($"informe: {reportPath.Replace('\\', '/')}");
        }

        if (curationPath is not null && !check && result.NoRelief.Count == 0)
        {
            string? dir = Path.GetDirectoryName(Path.GetFullPath(curationPath));
            if (dir is not null)
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(curationPath, PackWriter.Bytes(CurationReport.Build(spec, result.Levels, result.Stats, packs, levelsArg, curationPath, decisions)));
            Console.WriteLine($"curacion: {curationPath.Replace('\\', '/')}");
        }

        Console.WriteLine($"tiempo total: {total.Elapsed.TotalSeconds:0.0} s");
        return result.NoRelief.Count == 0 && !differs ? 0 : 1;
    }

    /// <summary>
    /// Fases 1-5 en memoria para los niveles <paramref name="first"/>-<paramref name="last"/>: candidatos, pool e índice D,
    /// colocación, alivio y un pack por mundo. Null si algún bloque no tiene colocación (el motivo va por stderr). Los
    /// mensajes de progreso van a <paramref name="log"/> (uno por nivel y por ronda de banda solo con
    /// <paramref name="verbose"/>).
    /// </summary>
    public static CampaignResult? Generate(CampaignSpec spec, int first, int last, Action<string> log, bool verbose)
    {
        List<LevelPlan> plans = Enumerable.Range(first, last - first + 1).Select(spec.Plan).ToList();
        var levels = plans.Select(p => new GeneratedLevel { Plan = p, Slot = null! }).ToList();
        log($"campana {spec.SpecId}: niveles {first}-{last} ({levels.Count}), {Environment.ProcessorCount} hilos");

        // Fase 1: candidatos.
        var phase = Stopwatch.StartNew();
        var generators = new ThreadLocal<SlotGenerator>(() => new SlotGenerator(spec), trackAllValues: false);
        Parallel.ForEach(levels, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, level =>
        {
            var sw = Stopwatch.StartNew();
            SlotGenerator gen = generators.Value!;
            SlotResult slot = gen.Generate(level.Plan);
            bool murky = level.Plan.Mechanics.Any(m => m.Code == "Tu");
            for (int round = 1; murky && round <= MurkyExtraRounds && !slot.Candidates.Any(c => c.PoolMember); round++)
            {
                // Antes de publicar sin turbias, más candidatos turbios (los que entran en la ventana a menudo no tienen ruta
                // segura, 03 §8): sub-semillas de otro tramo de intentos, sin solaparse con las rondas de la colocación.
                SlotResult more = gen.Generate(level.Plan, firstAttempt: MurkyRoundFirstAttempt + (round * 1000));
                slot.Candidates.AddRange(more.Candidates);
                slot = slot with { Attempts = slot.Attempts + more.Attempts, Rejected = slot.Rejected + more.Rejected, MurkyRounds = round };
            }

            if (murky && !slot.Candidates.Any(c => c.PoolMember))
            {
                // T1: si ningún candidato con turbias mide Difícil o Muy difícil, se publica sin turbias y se registra.
                SlotResult plain = gen.Generate(level.Plan, firstAttempt: 2000, dropMurky: true);
                plain.Notes.Insert(0, string.Create(CultureInfo.InvariantCulture,
                    $"sin Aguas turbias: ningun candidato turbio entro en la ventana ({slot.Attempts} intentos turbios)"));
                slot = plain with { MurkyRounds = slot.MurkyRounds };
            }

            level.Slot = slot;
            if (verbose)
            {
                log(AsciiText.Of(string.Create(CultureInfo.InvariantCulture,
                    $"  nivel {level.Plan.Level,3} {level.Plan.Cell.Short,-18} {level.Plan.RoleName,-12} {slot.Candidates.Count(c => c.PoolMember)} en ventana de {slot.Attempts} intentos ({sw.Elapsed.TotalSeconds:0.0} s)")));
            }
        });
        log($"fase 1 (candidatos): {phase.Elapsed.TotalSeconds:0.0} s");

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
            // Rondas de banda (03 §7: «la banda coloca»): cada slot necesita algún candidato de la banda de su rol.
            for (int round = 0; round < BandRounds; round++)
            {
                List<GeneratedLevel> wanting = block.Where(WantsBandCandidates).ToList();
                if (wanting.Count == 0)
                {
                    break;
                }

                if (verbose)
                {
                    log($"  bloque {block[0].Plan.Level}-{block[^1].Plan.Level}: ronda de banda {round + 1} para {string.Join(", ", wanting.Select(l => l.Plan.Level))}");
                }

                int bandRound = round;
                Parallel.ForEach(wanting, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, l =>
                {
                    SlotResult more = generators.Value!.Generate(l.Plan, extraTarget: BandRoundExtraTarget,
                        firstAttempt: BandRoundFirstAttempt + (bandRound * 1000), dropMurky: l.Slot.MurkyDropped);
                    Pool.Score(more, stats[l.Plan.Range]);
                    l.Slot.Candidates.AddRange(more.Candidates);
                    l.BandRounds = bandRound + 1;
                });
            }

            var excluded = new HashSet<Candidate>();
            Dictionary<int, Candidate>? placed = null;
            for (int round = 0; round <= MaxExtraRounds; round++)
            {
                for (int guard = 0; guard < 50; guard++)
                {
                    placed = Placement.PlaceCycle(spec, block.Select(b => b.Slot).ToList(), history.LastOrDefault(), history, excluded.Contains, stats);
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

                log(placed is null
                    ? $"  ciclo {block[0].Plan.Level}-{block[^1].Plan.Level}: sin colocacion valida; ronda extra {round + 1}"
                    : $"  ciclo {block[0].Plan.Level}-{block[^1].Plan.Level}: {missed.Count} reto(s) fuera de ventana; ronda extra {round + 1}");
                // Más candidatos para los slots que deciden la escalera (retos e hito), para los que siguen a un reto o al hito
                // y para las recuperaciones (su banda depende de las comodidades colocadas).
                var extra = placed is not null ? missed : block.Where(l => !l.Plan.Ramp && (l.Plan.Role is SlotRole.Challenge or SlotRole.Milestone or SlotRole.Recovery
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
                    Dictionary<int, Candidate>? retry = Placement.PlaceCycle(spec, block.Select(b => b.Slot).ToList(), history.LastOrDefault(), history, excluded.Contains, stats);
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
                    placed = Placement.PlaceCycle(spec, block.Select(b => b.Slot).ToList(), history.LastOrDefault(), history, excluded.Contains, stats, strictBand: false);
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
                    log($"  ciclo {block[0].Plan.Level}-{block[^1].Plan.Level}: colocado sin exigir la bajada de banda tras cada etiqueta (registrado)");
                }
            }

            // Rondas de rescate (P7b): ni el último recurso coloca el ciclo; más candidatos para los slots que deciden la escalera
            // y, tras cada ronda, la colocación estricta y después la del último recurso. Solo corren en un ciclo que antes no se
            // generaba, así que los packs ya publicados (11-300) no cambian.
            for (int rescue = 0; placed is null && rescue < RescueRounds; rescue++)
            {
                log($"  ciclo {block[0].Plan.Level}-{block[^1].Plan.Level}: sin colocacion valida; ronda de rescate {rescue + 1}");
                var extra = block.Where(l => !l.Plan.Ramp && (l.Plan.Role is SlotRole.Challenge or SlotRole.Milestone or SlotRole.Recovery
                    || block.Any(o => o.Plan.Level == l.Plan.Level - 1 && o.Plan.Role is SlotRole.Challenge or SlotRole.Milestone))).ToList();
                int rescueRound = rescue;
                Parallel.ForEach(extra, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, l =>
                {
                    SlotResult more = generators.Value!.Generate(l.Plan, firstAttempt: RescueRoundFirstAttempt + (rescueRound * 1000), dropMurky: l.Slot.MurkyDropped);
                    Pool.Score(more, stats[l.Plan.Range]);
                    l.Slot.Candidates.AddRange(more.Candidates);
                    l.Rounds = MaxExtraRounds + 1 + rescueRound;
                });

                foreach (bool strict in new[] { true, false })
                {
                    for (int guard = 0; guard < 50; guard++)
                    {
                        placed = Placement.PlaceCycle(spec, block.Select(b => b.Slot).ToList(), history.LastOrDefault(), history, excluded.Contains, stats, strictBand: strict);
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
                        if (!strict)
                        {
                            log($"  ciclo {block[0].Plan.Level}-{block[^1].Plan.Level}: colocado sin exigir la bajada de banda tras cada etiqueta (registrado)");
                        }

                        break;
                    }
                }
            }

            if (placed is null)
            {
                Console.Error.WriteLine($"level-generator: el bloque {block[0].Plan.Level}-{block[^1].Plan.Level} no tiene colocacion que cumpla el escalon de D-009");
                foreach (GeneratedLevel l in block)
                {
                    List<Candidate> opts = Placement.Options(l.Slot, stats[l.Plan.Range], _ => false);
                    Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture,
                        $"  {l.Plan.Level} {l.Plan.RoleName,-12} opciones {opts.Count}: {string.Join(" ", opts.Select(c => $"{c.MeasuredLabel[0]}/D{Placement.Rounded(c.D):0.00}/q{c.Quantile:0.00}{(c.InWindow ? "" : "/fuera")}"))}"));
                }

                return null;
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

        log($"fase 3 (colocacion): {phase.Elapsed.TotalSeconds:0.0} s");

        // Fase 4: alivio.
        phase.Restart();
        Parallel.ForEach(levels.Where(l => l.Plan.Level >= 31), new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, l =>
        {
            Candidate c = l.Chosen!;
            (List<ReliefBoard> published, List<ReliefBoard> rejected) = Relief.Compute(generators.Value!.Metrics, c.Board, c.Par, c.Bot, l.Plan.SeedText);
            l.Relief = published;
            l.ReliefRejected = rejected;
        });
        log($"fase 4 (alivio): {phase.Elapsed.TotalSeconds:0.0} s");
        List<GeneratedLevel> noRelief = levels.Where(l => l.Plan.Level >= 31 && l.Relief.Count == 0).ToList();
        foreach (GeneratedLevel l in noRelief)
        {
            Console.Error.WriteLine($"level-generator: el nivel {l.Plan.Level} no tiene ninguna variante de alivio que cumpla el criterio de 03 sec. 17 #8");
        }

        // Fase 5: un pack por mundo, en memoria.
        var packs = new List<GeneratedPack>();
        foreach (WorldInfo world in spec.Worlds)
        {
            List<GeneratedLevel> inWorld = levels.Where(l => l.Plan.World == world).ToList();
            if (inWorld.Count == 0)
            {
                continue;
            }

            if (inWorld.Count != world.Last - world.First + 1)
            {
                log($"  aviso: el mundo {world.World} queda incompleto ({inWorld.Count} niveles)");
            }

            string text = PackWriter.Write(spec, world, inWorld.Select(l => new PackLevel(l.Plan, l.Chosen!, l.Relief)).ToList());
            packs.Add(new GeneratedPack(world, PackWriter.PackId(world, spec.Lap), PackWriter.Bytes(text), inWorld.Count));
        }

        return new CampaignResult(levels, stats, packs, noRelief);
    }

    /// <summary>
    /// El slot no tiene ningún candidato de la banda de su rol (03 §7): recuperación sin ninguno en ventana y en banda de
    /// recuperación; comodidad, estreno, remanso o rampa sin ninguno en ventana con q ≤ 0,70; hito sin ninguno en banda de
    /// hito. Los retos los decide la escalera y piden candidatos en la colocación.
    /// </summary>
    private static bool WantsBandCandidates(GeneratedLevel l)
    {
        List<Candidate> cands = l.Slot.Candidates;
        return l.Plan.Role switch
        {
            SlotRole.Recovery when !l.Plan.Ramp => !cands.Any(c => c.PoolMember && Pool.BandIndex(c.Quantile) == 0),
            SlotRole.Comfort or SlotRole.Remanso => !cands.Any(c => c.PoolMember && Placement.ComfortBandOk(c.Quantile)),
            SlotRole.Milestone => !cands.Any(c => c.Measured && c.Reject is null && Pool.BandIndex(c.Quantile) == 3),
            _ => false,
        };
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
