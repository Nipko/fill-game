// --reserve: la reserva del 20 % (D-033, añadido «la reserva del 20 %», Q-B11-7 (a); 03 §17; D-020, pregunta 31 b).
// Un pack de reserva por mundo (reserva_{mm}_v1.json, kind reserve, pack_schema 4): para los niveles del pack de campaña
// publicado que usan una mecánica con kill switch (mech_enabled_*, 08 §9), otro tablero del mismo slot sin mecánicas y en la
// ventana del slot, con id m{mm}_v1_n{nnnn}_r1, semilla amazonfill:v1:m{mm}:v1:n{nnnn}:r1 y replaces = el id de campaña del
// slot. Como mucho el 20 % de los niveles del mundo (redondeo hacia abajo [P]). El cliente solo lo usa desde level_overrides.
//   - Orden [P] («los primeros que necesita el kill switch»): por rondas sobre los kill switches en el orden de 08 §9 (hoja,
//     matapi, liana, remolino, barro, lluvia, brote, pirana, tinaja); en cada ronda, cada mecánica toma su primer nivel aún
//     sin elegir. Así cada mecánica del mundo tiene cubiertos sus primeros niveles antes de que otra tenga más. Un nivel con
//     dos mecánicas las cubre a las dos (el tablero de reserva no lleva ninguna).
//   - Tablero [P]: el plan del slot (rol, K, vacíos, fuente, ventana y etiqueta permitida de la especificación) con la celda
//     sin mecánicas (también sin Aguas turbias, que no tienen kill switch) y con sus capacidades (Cuia chica y alta, que
//     tampoco lo tienen); intro_mechanic null. Se elige, entre los candidatos en la ventana (bot ×40, etiqueta permitida,
//     contrato 6, par >= 2K, trampas tempranas) con el par dentro del objetivo del rango ±30 % (QA-PAR), el de la misma
//     etiqueta que el publicado y el bot_fail más cercano al suyo; desde el 31, con alivio (03 §13; Relief.Compute).
//   - Un slot sin ningún candidato válido tras las rondas se salta (queda en el informe) y entra el siguiente del orden.
//   - D no se publica: depende del pool del rango (03 §7) y la reserva no se coloca en el ciclo; band es la del slot.
// Cada slot depende solo de su semilla: el pack no depende de los hilos. --check compara byte a byte con el disco.
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using AF.Core.Board;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

/// <summary>Nivel del pack de campaña publicado, con lo que la reserva necesita de él.</summary>
internal sealed record ReserveSource(int Level, string Id, int Mask, string Label, double Bot);

/// <summary>Un tablero de reserva generado.</summary>
internal sealed record ReserveLevel(ReserveSource Source, LevelPlan Plan, Candidate Chosen, List<ReliefBoard> Relief, int Rounds);

/// <summary>Un slot que no tuvo tablero de reserva (se salta y entra el siguiente del orden).</summary>
internal sealed record ReserveSkip(ReserveSource Source, string Why);

/// <summary>El pack de reserva de un mundo, en memoria.</summary>
internal sealed record ReserveResult(
    WorldInfo World, string CampaignPath, string PackId, byte[] Bytes, int Cap, List<ReserveSource> WithMechanics,
    List<ReserveSource> Order, List<ReserveLevel> Levels, List<ReserveSkip> Skipped);

internal static class ReserveRunner
{
    /// <summary>Parte de los niveles del mundo que cubre la reserva (03 §17: «+ 20 % reserva»; D-033, Q-B11-7 a).</summary>
    public const int SharePercent = 20;

    /// <summary>Número del tablero de reserva de cada slot (sufijos <c>_r1</c> y <c>:r1</c>).</summary>
    public const int ReserveIndex = 1;

    /// <summary>pack_schema del pack de reserva (el 3 más kind reserve y replaces; contrato 21).</summary>
    public const int PackSchema = 4;

    /// <summary>Rondas de candidatos por slot antes de saltarlo [P] (la 0 es la del plan; las demás, con otras sub-semillas).</summary>
    public const int Rounds = 6;

    /// <summary>Candidatos válidos que se prueban por ronda con el alivio (el alivio es caro: A* de cada −1 color).</summary>
    public const int ReliefTriesPerRound = 3;

    /// <summary>Kill switches de 08 §9 (<c>mech_enabled_*</c>), en el orden de la tabla: el orden de las rondas.</summary>
    public static readonly string[] Switches = ["hoja", "matapi", "liana", "remolino", "barro", "lluvia", "brote", "pirana", "tinaja"];

    /// <summary>Tableros de reserva como mucho en el mundo: el 20 % de sus niveles, hacia abajo [P].</summary>
    public static int Cap(WorldInfo world) => (world.Last - world.First + 1) * SharePercent / 100;

    public static string PackId(WorldInfo world, int lap) => string.Create(CultureInfo.InvariantCulture, $"reserva_{world.World:00}_v{lap}");

    /// <summary>Bit del kill switch de un tipo de attrs (las variantes cuentan como su base, como en el cliente; 08 §9).</summary>
    public static int SwitchBit(string type) => type switch
    {
        "hoja" or "hoja_atada" => 1 << 0,
        "matapi" => 1 << 1,
        "liana" or "flor" => 1 << 2,
        "remolino" => 1 << 3,
        "lluvia" => 1 << 5,
        "brote" => 1 << 6,
        "pirana" => 1 << 7,
        "tinaja" or "cucharon" => 1 << 8,
        _ => 0,
    };

    /// <summary>Bit del Barro (la unidad 15 en cuias, no un tipo de attrs).</summary>
    public const int BarroBit = 1 << 4;

    /// <summary>Índice de motor del Barro (03 §3).</summary>
    public const int BarroUnit = 15;

    /// <summary>Ids de los kill switches de una máscara, en el orden de 08 §9.</summary>
    public static string Describe(int mask) =>
        string.Join("+", Switches.Where((_, i) => (mask & (1 << i)) != 0));

    /// <summary>Niveles del pack de campaña con su máscara de kill switches (attrs y Barro del tablero base).</summary>
    public static (int World, List<ReserveSource> Levels) ReadCampaign(string path)
    {
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllBytes(path));
        JsonElement root = doc.RootElement;
        int world = root.GetProperty("world").GetInt32();
        var list = new List<ReserveSource>();
        foreach (JsonElement lv in root.GetProperty("levels").EnumerateArray())
        {
            int mask = 0;
            foreach (JsonElement a in lv.GetProperty("attrs").EnumerateArray())
            {
                if (a.ValueKind == JsonValueKind.Object)
                {
                    mask |= SwitchBit(a.GetProperty("type").GetString() ?? "");
                }
            }

            foreach (JsonElement cuia in lv.GetProperty("cuias").EnumerateArray())
            {
                if (cuia.EnumerateArray().Any(u => u.GetInt32() == BarroUnit))
                {
                    mask |= BarroBit;
                }
            }

            JsonElement diff = lv.GetProperty("difficulty");
            list.Add(new ReserveSource(
                lv.GetProperty("level").GetInt32(), lv.GetProperty("id").GetString() ?? "", mask,
                diff.GetProperty("label").GetString() ?? "", diff.GetProperty("bot_fail").GetDouble()));
        }

        return (world, list.OrderBy(l => l.Level).ToList());
    }

    /// <summary>
    /// Orden de los slots de la reserva [P]: por rondas sobre los kill switches de 08 §9; en cada ronda, cada mecánica toma su
    /// primer nivel (de menor a mayor) aún sin elegir. Todos los niveles con mecánica salen una vez.
    /// </summary>
    public static List<ReserveSource> Order(IReadOnlyList<ReserveSource> levels)
    {
        var withMechanics = levels.Where(l => l.Mask != 0).OrderBy(l => l.Level).ToList();
        var order = new List<ReserveSource>();
        var taken = new HashSet<int>();
        while (order.Count < withMechanics.Count)
        {
            for (int s = 0; s < Switches.Length; s++)
            {
                ReserveSource? next = withMechanics.FirstOrDefault(l => (l.Mask & (1 << s)) != 0 && !taken.Contains(l.Level));
                if (next is not null)
                {
                    taken.Add(next.Level);
                    order.Add(next);
                }
            }
        }

        return order;
    }

    /// <summary>
    /// Plan del tablero de reserva del slot <paramref name="level"/>: el de la especificación con la celda sin mecánicas (las
    /// capacidades se quedan), sin intro_mechanic y con la semilla y el id de reserva.
    /// </summary>
    public static LevelPlan Plan(CampaignSpec spec, int level)
    {
        LevelPlan p = spec.Plan(level);
        CalendarCell c = p.Cell;
        string? pure = c.Capacities.Count > 0 ? null : p.Role switch
        {
            SlotRole.Recovery => "R",
            SlotRole.Remanso => "S",
            SlotRole.Comfort => "C",
            _ => null,
        };
        CalendarCell cell = c with
        {
            Literal = c.Literal + " [reserva sin mecanicas]",
            Mechanics = [],
            PureCode = c.PureCode ?? pure,
        };
        return new LevelPlan
        {
            Level = p.Level,
            World = p.World,
            Range = p.Range,
            Position = p.Position,
            Ramp = p.Ramp,
            Role = p.Role,
            Cell = cell,
            K = p.K,
            Empties = p.Empties,
            Source = p.Source,
            Window = p.Window,
            AllowedLabel = p.AllowedLabel,
            TargetLabel = p.TargetLabel,
            DebutBoard = p.DebutBoard,
            Row = p.Row,
            Intro = null,
            MaxRiskPairs = p.MaxRiskPairs,
            Season = p.Season,
            Reserve = ReserveIndex,
        };
    }

    /// <summary>Par dentro del objetivo del rango ±30 % (03 §17 #2; QA-PAR).</summary>
    public static bool ParInRange(LevelPlan plan, int par) =>
        par >= (plan.Range.ParMin * 0.7) - 1e-9 && par <= (plan.Range.ParMax * 1.3) + 1e-9;

    /// <summary>Tablero de reserva de un slot, o null (con el motivo) si ninguna ronda da uno válido.</summary>
    public static ReserveLevel? GenerateSlot(SlotGenerator gen, CampaignSpec spec, ReserveSource src, out string why)
    {
        LevelPlan plan = Plan(spec, src.Level);
        int inWindow = 0;
        int parOut = 0;
        int noRelief = 0;
        for (int round = 0; round < Rounds; round++)
        {
            SlotResult slot = gen.Generate(plan, extraTarget: round == 0 ? 0 : CampaignRunner.BandRoundExtraTarget, firstAttempt: round * 1000);
            var valid = new List<Candidate>();
            foreach (Candidate c in slot.Candidates.Where(c => c.PoolMember))
            {
                inWindow++;
                if (ParInRange(plan, c.Par))
                {
                    valid.Add(c);
                }
                else
                {
                    parOut++;
                }
            }

            foreach (Candidate c in valid
                .OrderBy(c => c.MeasuredLabel == src.Label ? 0 : 1)
                .ThenBy(c => Math.Abs(c.Bot - src.Bot))
                .ThenBy(c => c.Attempt)
                .Take(ReliefTriesPerRound))
            {
                List<ReliefBoard> relief = [];
                if (plan.Level >= 31)
                {
                    relief = SlotGenerator.ReliefOf(gen.Metrics, c, plan.SeedText).Published;
                    if (relief.Count == 0)
                    {
                        noRelief++;
                        continue;
                    }
                }

                why = "";
                return new ReserveLevel(src, plan, c, relief, round + 1);
            }
        }

        why = string.Create(CultureInfo.InvariantCulture,
            $"{Rounds} rondas: {inWindow} candidatos en la ventana {plan.Window}, {parOut} con el par fuera del rango, {noRelief} sin alivio");
        return null;
    }

    /// <summary>
    /// El pack de reserva del mundo del pack de campaña <paramref name="campaignPath"/>, en memoria. Null (con el motivo en
    /// <paramref name="error"/>) si el pack no es de esta especificación o si no se llega a ningún tablero.
    /// </summary>
    public static ReserveResult? Generate(CampaignSpec spec, string campaignPath, Action<string> log, out string error)
    {
        (int worldNumber, List<ReserveSource> levels) = ReadCampaign(campaignPath);
        WorldInfo? world = spec.Worlds.FirstOrDefault(w => w.World == worldNumber);
        if (world is null || world.First < spec.FirstLevel || world.Last > spec.LastLevel)
        {
            error = string.Create(CultureInfo.InvariantCulture, $"{campaignPath}: el mundo {worldNumber} no esta entero en la especificacion {spec.SpecId}");
            return null;
        }

        if (levels.Count != world.Last - world.First + 1 || levels[0].Level != world.First || levels[^1].Level != world.Last)
        {
            error = string.Create(CultureInfo.InvariantCulture, $"{campaignPath}: el pack no tiene los niveles {world.First}-{world.Last} del mundo {worldNumber}");
            return null;
        }

        int cap = Cap(world);
        List<ReserveSource> order = Order(levels);
        var withMechanics = levels.Where(l => l.Mask != 0).ToList();
        log(string.Create(CultureInfo.InvariantCulture,
            $"reserva del mundo {world.World} ({world.First}-{world.Last}): {withMechanics.Count} niveles con mecanica, tope {cap} (20 %)"));
        var generators = new ThreadLocal<SlotGenerator>(() => new SlotGenerator(spec), trackAllValues: false);
        var chosen = new List<ReserveLevel>();
        var skipped = new List<ReserveSkip>();
        int next = 0;
        while (chosen.Count < cap && next < order.Count)
        {
            // Un lote con los que faltan, en paralelo; se aceptan en el orden de la lista (el resultado no depende de los hilos).
            List<ReserveSource> batch = order.Skip(next).Take(cap - chosen.Count).ToList();
            next += batch.Count;
            var results = new (ReserveLevel? Level, string Why)[batch.Count];
            Parallel.For(0, batch.Count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            {
                var sw = Stopwatch.StartNew();
                ReserveLevel? r = GenerateSlot(generators.Value!, spec, batch[i], out string why);
                results[i] = (r, why);
                log(AsciiText.Of(string.Create(CultureInfo.InvariantCulture,
                    $"  reserva {batch[i].Level,3} ({Describe(batch[i].Mask)}): {(r is null ? "SIN TABLERO: " + why : $"bot {r.Chosen.Bot:0.000} {r.Chosen.MeasuredLabel}, par {r.Chosen.Par}, ronda {r.Rounds}")} ({sw.Elapsed.TotalSeconds:0.0} s)")));
            });

            for (int i = 0; i < batch.Count; i++)
            {
                if (results[i].Level is ReserveLevel r)
                {
                    chosen.Add(r);
                }
                else
                {
                    skipped.Add(new ReserveSkip(batch[i], results[i].Why));
                }
            }
        }

        if (chosen.Count == 0 && cap > 0 && withMechanics.Count > 0)
        {
            error = string.Create(CultureInfo.InvariantCulture, $"mundo {world.World}: ningun slot con tablero de reserva");
            return null;
        }

        error = "";
        if (chosen.Count == 0)
        {
            return new ReserveResult(world, campaignPath, PackId(world, spec.Lap), [], cap, withMechanics, order, chosen, skipped);
        }

        List<ReserveLevel> sorted = chosen.OrderBy(r => r.Plan.Level).ToList();
        string text = Write(spec, world, sorted);
        return new ReserveResult(world, campaignPath, PackId(world, spec.Lap), PackWriter.Bytes(text), cap, withMechanics, order, sorted, skipped);
    }

    /// <summary>Pack de reserva af-level/1 (pack_schema 4, kind reserve), con el formato de los packs de campaña (PackWriter).</summary>
    public static string Write(CampaignSpec spec, WorldInfo world, IReadOnlyList<ReserveLevel> levels)
    {
        var header = new JObj
        {
            { "pack_schema", PackSchema },
            { "schema_file", PackWriter.SchemaFile },
            { "pack_id", PackId(world, spec.Lap) },
            { "kind", "reserve" },
            { "generator", new JObj { { "tool", "level-generator" }, { "version", PackWriter.GeneratorVersion } } },
            { "world", world.World },
            { "world_id", world.WorldId },
            { "lap", spec.Lap },
            { "water_base", world.WaterBase },
            { "pool", spec.WorldPool(world).Select(t => (object?)t.Id).ToList() },
            { "k_min", levels.Min(l => l.Chosen.K) },
            { "k_max", levels.Max(l => l.Chosen.K) },
            { "max_risk_pairs", levels.Max(l => l.Plan.MaxRiskPairs) },
            { "cap_default", Cuia.DefaultCapacity },
            { "empties_default", world.First <= spec.TwoEmptiesUntil ? 2 : 1 },
        };

        var sb = new StringBuilder();
        string head = PackWriter.Min(header);
        sb.Append(head, 0, head.Length - 1).Append(",\"levels\":[\n");
        for (int i = 0; i < levels.Count; i++)
        {
            sb.Append(PackWriter.Min(Level(spec, levels[i])));
            sb.Append(i + 1 < levels.Count ? ",\n" : "\n");
        }

        sb.Append("]}\n");
        return sb.ToString();
    }

    /// <summary>
    /// Un nivel de reserva: el de PackWriter.Level con <c>replaces</c> tras <c>id</c> y sin <c>D</c> (la reserva no entra en el
    /// pool del rango, 03 §7).
    /// </summary>
    private static JObj Level(CampaignSpec spec, ReserveLevel r)
    {
        JObj level = PackWriter.Level(spec, new PackLevel(r.Plan, r.Chosen, r.Relief));
        var result = new JObj();
        foreach (KeyValuePair<string, object?> kv in level)
        {
            if (kv.Key == "difficulty" && kv.Value is JObj diff)
            {
                var d = new JObj();
                foreach (KeyValuePair<string, object?> dk in diff.Where(dk => dk.Key != "D"))
                {
                    d.Add(dk);
                }

                result.Add("difficulty", d);
                continue;
            }

            result.Add(kv);
            if (kv.Key == "id")
            {
                result.Add("replaces", r.Plan.CampaignId);
            }
        }

        return result;
    }

    /// <summary>
    /// Modo --reserve: un pack de reserva por cada pack de campaña dado, en <paramref name="outDir"/> (o --check), y, con
    /// <paramref name="reportPath"/>, el informe.
    /// </summary>
    public static int Run(string specPath, string? variant, IReadOnlyList<string> campaignPacks, string outDir, string? reportPath, bool check)
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

        var results = new List<ReserveResult>();
        foreach (string campaign in campaignPacks)
        {
            if (!File.Exists(campaign))
            {
                Console.Error.WriteLine($"level-generator: no existe el pack de campana {campaign}");
                return 2;
            }

            ReserveResult? r;
            try
            {
                r = Generate(spec, campaign, Console.WriteLine, out string error);
                if (r is null)
                {
                    Console.Error.WriteLine(AsciiText.Of($"level-generator: {error}"));
                    return 1;
                }
            }
            catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
            {
                Console.Error.WriteLine(AsciiText.Of($"level-generator: no se puede leer {campaign}: {e.Message}"));
                return 2;
            }

            results.Add(r);
        }

        bool differs = false;
        foreach (ReserveResult r in results)
        {
            string path = Path.Combine(outDir, r.PackId + ".json");
            if (r.Levels.Count == 0)
            {
                // Un mundo sin mecánicas con kill switch (el 2: solo capacidades) no tiene pack de reserva.
                bool exists = File.Exists(path);
                differs |= check && exists;
                Console.WriteLine($"{path.Replace('\\', '/')}: sin niveles con mecanica; no hay pack de reserva{(check && exists ? " (y existe en el disco: NO coincide)" : "")}");
                continue;
            }

            if (check)
            {
                byte[]? onDisk = File.Exists(path) ? File.ReadAllBytes(path) : null;
                bool same = onDisk is not null && onDisk.AsSpan().SequenceEqual(r.Bytes);
                differs |= !same;
                Console.WriteLine($"--check: {path.Replace('\\', '/')} {(same ? "coincide" : "NO coincide")} con el generado");
            }
            else
            {
                Directory.CreateDirectory(outDir);
                File.WriteAllBytes(path, r.Bytes);
                Console.WriteLine($"pack: {path.Replace('\\', '/')} ({r.Bytes.Length} bytes, {r.Levels.Count} niveles)");
            }
        }

        if (reportPath is not null && !check)
        {
            string? dir = Path.GetDirectoryName(Path.GetFullPath(reportPath));
            if (dir is not null)
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(reportPath, PackWriter.Bytes(Report(spec, specPath, variant, results, outDir, reportPath)));
            Console.WriteLine($"informe: {reportPath.Replace('\\', '/')}");
        }

        Console.WriteLine($"tiempo total: {total.Elapsed.TotalSeconds:0.0} s");
        return differs ? 1 : 0;
    }

    /// <summary>
    /// --self-test reserve: el orden por rondas (sintético), el tope del 20 % de cada mundo de campaign_v1 y campaign_v2, el
    /// plan sin mecánicas (con las capacidades, sin intro, con los sufijos de reserva y la ventana del slot) y la generación del
    /// mundo 3 dos veces en memoria con el mismo resultado byte a byte. Desde la raíz del repositorio.
    /// </summary>
    public static int SelfTest()
    {
        int fails = 0;
        void Check(bool ok, string what)
        {
            fails += ok ? 0 : 1;
            Console.WriteLine(AsciiText.Of($"{(ok ? "PASS" : "FAIL")}  reserve: {what}"));
        }

        // Orden: hoja = bit 0, matapi = 1, barro = 4. El 10 lleva Hoja y Matapi; el 30, nada.
        var sources = new List<ReserveSource>
        {
            new(10, "a", 0b11, "normal", 0), new(11, "b", 0b1, "normal", 0), new(12, "c", 0b1, "normal", 0),
            new(20, "d", 0b10, "normal", 0), new(21, "e", 0b10, "normal", 0), new(25, "f", BarroBit, "normal", 0), new(30, "g", 0, "normal", 0),
        };
        string order = string.Join(",", Order(sources).Select(s => s.Level.ToString(CultureInfo.InvariantCulture)));
        Check(order == "10,20,25,11,21,12", $"orden por rondas de los kill switches: {order} (se espera 10,20,25,11,21,12)");

        CampaignSpec v1 = CampaignSpec.Load("tools/level-generator/specs/campaign_v1.json", null);
        CampaignSpec v2 = CampaignSpec.Load("tools/level-generator/specs/campaign_v2.json", null);
        string caps = string.Join(",", v1.Worlds.Concat(v2.Worlds).Select(w => Cap(w).ToString(CultureInfo.InvariantCulture)));
        Check(caps == "6,8,12,12,20,24,28,28", $"tope del 20 % de los mundos 2-9: {caps}");

        LevelPlan p = Plan(v1, 43);
        LevelPlan c = v1.Plan(43);
        Check(p.Mechanics.Count == 0 && c.Mechanics.Count == 1 && p.Intro is null && c.Intro == "hoja", "el 43 sin la Hoja ni intro_mechanic");
        Check(p.Id == "m03_v1_n0043_r1" && p.SeedText == "amazonfill:v1:m03:v1:n0043:r1" && p.CampaignId == "m03_v1_n0043", $"ids de reserva: {p.Id}, {p.SeedText}, {p.CampaignId}");
        Check(p.Window == c.Window && p.Role == c.Role && p.K == c.K && p.AllowedLabel == c.AllowedLabel, "misma ventana, rol, K y etiqueta permitida que el slot");
        int withCapacity = Enumerable.Range(v1.FirstLevel, v1.LastLevel - v1.FirstLevel + 1)
            .First(l => v1.Plan(l).Capacities.Count > 0 && v1.Plan(l).Mechanics.Count > 0);
        LevelPlan pc = Plan(v1, withCapacity);
        Check(pc.Mechanics.Count == 0 && pc.Capacities.Count == v1.Plan(withCapacity).Capacities.Count,
            string.Create(CultureInfo.InvariantCulture, $"el {withCapacity} conserva sus capacidades y pierde sus mecanicas"));

        const string World3 = "Assets/_Project/Data/Levels/mundo_03_v1.json";
        ReserveResult? a = Generate(v1, World3, _ => { }, out string errA);
        ReserveResult? b = Generate(v1, World3, _ => { }, out string errB);
        Check(a is not null && b is not null && a.Bytes.AsSpan().SequenceEqual(b.Bytes) && a.Levels.Count == 8,
            $"mundo 3 generado dos veces: mismos bytes y 8 tableros {errA}{errB}");

        Console.WriteLine(fails == 0 ? "--self-test reserve: todo PASS" : string.Create(CultureInfo.InvariantCulture, $"--self-test reserve: {fails} FAIL"));
        return fails == 0 ? 0 : 1;
    }

    /// <summary>Informe Markdown de la reserva (sin tiempos: dos ejecuciones dan los mismos bytes).</summary>
    public static string Report(CampaignSpec spec, string specPath, string? variant, IReadOnlyList<ReserveResult> results, string outDir, string reportPath)
    {
        static string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        void W(string line = "") => sb.Append(line).Append('\n');
        string packs = string.Join(" ", results.Select(r => r.CampaignPath.Replace('\\', '/')));
        string variantArg = variant is null ? "" : " --variant " + variant;
        W(F($"# Reserva del 20 %: {spec.SpecId}"));
        W();
        W(F($"Generado por `dotnet run --project tools/level-generator -c Release -- --reserve {packs} --spec {specPath.Replace('\\', '/')}{variantArg} --out {outDir.Replace('\\', '/')} --report {reportPath.Replace('\\', '/')}`: no se edita a mano."));
        W(F($"QA: `dotnet run --project tools/level-qa -c Release -- --reserve <reserva_NN_v1.json> --campaign <mundo_NN_v1.json> --spec {specPath.Replace('\\', '/')}{variantArg}`."));
        W();
        W("Fuente: D-033, añadido «la reserva del 20 %» (Q-B11-7 (a)); 03 §17; D-020 (pregunta 31 b). Un pack por mundo (`kind` = `reserve`, `pack_schema` 4) con, para los niveles que usan una mecánica con kill switch (`mech_enabled_*`, 08 §9), otro tablero del mismo slot sin mecánicas y en su ventana, hasta el 20 % de los niveles del mundo. El cliente solo los usa desde `level_overrides`.");
        W();
        W("Reglas [P] de esta herramienta: orden por rondas sobre los kill switches de 08 §9 (cada mecánica toma su primer nivel aún sin elegir); tablero con la celda sin mecánicas (ni Aguas turbias) y con sus capacidades; entre los candidatos en la ventana con el par en el objetivo del rango ±30 %, el de la misma etiqueta que el publicado y el `bot_fail` más cercano; alivio desde el 31; sin `D` (no entra en el pool del rango).");
        W();
        W("## Resumen");
        W();
        W("| Mundo | Pack | Niveles del mundo | Con mecánica | Tope (20 %) | Tableros | Saltados | Bytes | SHA-256 |");
        W("|---|---|---|---|---|---|---|---|---|");
        foreach (ReserveResult r in results)
        {
            string sha = r.Bytes.Length == 0 ? "—" : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(r.Bytes)).ToLowerInvariant();
            W(F($"| {r.World.World} ({r.World.First}-{r.World.Last}) | {(r.Levels.Count == 0 ? "—" : "`" + r.PackId + ".json`")} | {r.World.Last - r.World.First + 1} | {r.WithMechanics.Count} | {r.Cap} | {r.Levels.Count} | {r.Skipped.Count} | {r.Bytes.Length} | {(r.Bytes.Length == 0 ? "—" : "`" + sha + "`")} |"));
        }

        W();
        W("## Cobertura por mecánica");
        W();
        W("Niveles del mundo que usan cada kill switch y cuántos tienen tablero de reserva (el primero sin cubrir, si lo hay).");
        W();
        W("| Mundo | Mecánica | Niveles | Con reserva | Primer nivel sin reserva |");
        W("|---|---|---|---|---|");
        foreach (ReserveResult r in results)
        {
            var covered = r.Levels.Select(l => l.Source.Level).ToHashSet();
            for (int s = 0; s < Switches.Length; s++)
            {
                var uses = r.WithMechanics.Where(l => (l.Mask & (1 << s)) != 0).ToList();
                if (uses.Count == 0)
                {
                    continue;
                }

                ReserveSource? first = uses.FirstOrDefault(l => !covered.Contains(l.Level));
                W(F($"| {r.World.World} | `{Switches[s]}` | {uses.Count} | {uses.Count(l => covered.Contains(l.Level))} | {(first is null ? "—" : first.Level.ToString(CultureInfo.InvariantCulture))} |"));
            }
        }

        foreach (ReserveResult r in results.Where(r => r.Levels.Count > 0 || r.Skipped.Count > 0))
        {
            W();
            W(F($"## Mundo {r.World.World}: `{r.PackId}.json`"));
            W();
            W("| Slot | Sustituye | Mecánicas del publicado | Rol | Celda | Ventana | Publicado | Reserva | K | Cuias | Vacíos | Par | Alivio | Ronda |");
            W("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (ReserveLevel l in r.Levels)
            {
                Candidate c = l.Chosen;
                LevelPlan p = l.Plan;
                string relief = l.Relief.Count == 0 ? "—" : string.Join(", ", l.Relief.Select(x => x.Type));
                W(F($"| {p.Level} | `{l.Source.Id}` | {Describe(l.Source.Mask)} | {p.RoleName} | {spec.Plan(p.Level).Cell.Short} | {p.Window} | {l.Source.Label} {l.Source.Bot:0.000} | {c.MeasuredLabel} {c.Bot:0.000} | {c.K} | {c.Board.Caps.Length} | {c.Empties} ({c.FreeEmpties}) | {c.Par} | {relief} | {l.Rounds} |"));
            }

            if (r.Skipped.Count > 0)
            {
                W();
                W("Saltados (sin tablero de reserva válido; entra el siguiente del orden):");
                W();
                foreach (ReserveSkip s in r.Skipped)
                {
                    W(F($"- {s.Source.Level} ({Describe(s.Source.Mask)}): {s.Why}"));
                }
            }
        }

        return sb.ToString();
    }
}
