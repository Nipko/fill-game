// Comprobaciones QA-DAILY-* del pool del Reto del río (R4 del encargo docs/dev/traspaso/spark-reto-del-rio.md; 03 §16 y §17;
// nota de diseño docs/dev/reto-del-rio.md §13 y §1.6). Las reglas por banda (K, vacíos, capacidad, ventana del bot, paleta y
// tope de pares, agua por mundo) salen de la especificación (tools/level-generator/specs/daily_v1.json, con el mismo lector
// que el generador, enlazado: Shared/Daily/), nunca de la cabecera del pack ni del código.
//   - Esquema (QA-DAILY-SCHEMA): python tools/validate_schema.py --pack, con Checks/GateChecks.Schema de la compuerta de
//     campaña (.NET no trae validador de JSON Schema y level-qa no usa NuGet). Sin Python o sin jsonschema, falla.
//   - El tablero se mide sobre caps y cuias (colores + vacíos, 02 R1): attrs, murky y hidden solo los mira QA-DAILY-MECH, para
//     que una mecánica colada no cambie además el resto de las medidas.
//   - Bot x40 (MetricsEngine.BotFail, enlazado del generador) con la semilla del tablero y su ventana con la tolerancia de 1e-9
//     del generador: FALLO (R-Q11 (a): la ventana es compuerta). Pares de riesgo contra el tope de la banda: FALLO, salvo con
//     max_risk_pairs "libre" (R-Q1b (a): B3), que no pone tope.
//   - Reproducción (QA-DAILY-REPRO, 03 §17 #10): DailyQaCommand regenera en memoria las bandas y los días del pack con el
//     DailyRunner y el DailyPackWriter del generador (enlazados) y compara cada tablero entero (igualdad profunda del JSON).
//   - Anti-repetición (#9, Campaign/BoardShape.cs como la campaña) y trampas tempranas (#3, el tope por etiqueta de
//     SlotGenerator.EarlyTrapCap) solo como aviso (R-Q10).
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using AF.Core.Board;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Daily;
using AmazonFill.Tools.LevelGenerator.Generation;
using AmazonFill.Tools.LevelGenerator.Metrics;
using AmazonFill.Tools.LevelQa.Checks;

namespace AmazonFill.Tools.LevelQa.Daily;

internal sealed record DailyIssue(string Check, string Board, string Message, bool Warning = false);

/// <summary>Un tablero del pack leído con tolerancia: lo que falta o no tiene su tipo queda en <see cref="ReadError"/>.</summary>
internal sealed class DailyQaLevel
{
    public required int Index { get; init; }
    public required JsonObject Json { get; init; }
    public string Name => Id ?? string.Create(CultureInfo.InvariantCulture, $"levels[{Index}]");
    public string? Id { get; init; }
    public string? Seed { get; init; }
    public int? Band { get; init; }
    public int? Day { get; init; }
    public int? World { get; init; }
    public string? WaterBase { get; init; }
    public string[] Palette { get; init; } = [];
    public int? Units { get; init; }
    public int[] Caps { get; init; } = [];
    public int[][] Cuias { get; init; } = [];
    public int? RiskPairs { get; init; }
    public string? ReadError { get; init; }
}

internal sealed class DailyQaPack
{
    public required string Path { get; init; }
    public required long Bytes { get; init; }
    public required JsonObject Json { get; init; }
    public required List<DailyQaLevel> Levels { get; init; }
}

internal sealed class DailyEval
{
    public required DailyQaLevel Level { get; init; }
    public string? BuildError { get; init; }
    public string? Validation { get; init; }
    public ParResult? Par { get; init; }
    public double Bot { get; init; } = -1;
    public int EarlyTraps { get; init; } = -1;
}

internal static class DailyChecks
{
    /// <summary>Tamaño máximo del pool sin comprimir (compuerta de R5 del encargo; 03 §16 estima ≈ 1,4 MB [E]).</summary>
    public const long MaxBytes = 2L * 1024 * 1024;

    public const double WindowTolerance = 1e-9;

    public static readonly string[] Forbidden = ["level", "difficulty", "solution", "safe_path", "qa"];

    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-DAILY-SCHEMA", "JSON valido contra schemas/af-level-1.schema.json con python tools/validate_schema.py --pack (03 sec. 17 #11)"),
        ("QA-DAILY-HEADER", "cabecera kind daily y pack_id de la especificacion; cada paleta en pool y K entre k_min y k_max"),
        ("QA-DAILY-STRUCT", "tablero legible y valido: BoardBuilder (U_c = units_per_color) y LevelValidation de AF.Core (sin selladas ni Aguas estancadas al inicio)"),
        ("QA-DAILY-SLOT", "slot {band 1-3, day 0-728} coherente con id d{b}_n{nnnn} y seed amazonfill:v1:daily:b{b}:d{ddd} (03 sec. 16)"),
        ("QA-DAILY-DAYS", "729 dias por banda (R-Q2b) en las bandas 1-3, sin huecos ni duplicados (con --partial: sin huecos exigidos, nunca pool completo)"),
        ("QA-DAILY-SHAPE", "K de la banda y del dia (k_by_day), vacios de la banda y cuias de capacidad 4 llenas o vacias, units_per_color 4 (especificacion; nota sec. 13)"),
        ("QA-DAILY-CUIAS-14", "como mucho 14 cuias (02 R1; 03 sec. 17 #5)"),
        ("QA-DAILY-PALETTE", "paleta = tintes presentes, de tints_from y con debut <= debut_until de la banda (R-Q1)"),
        ("QA-DAILY-RISK", "risk_pairs coherente y <= max_risk_pairs de la banda en la especificacion, no de la cabecera; \"libre\" no pone tope (R-Q1 y R-Q1b (a): B3 libre)"),
        ("QA-DAILY-MECH", "sin mecanicas ni ocultos: attrs todo null, murky false, hidden [] e intro_mechanic null (03 sec. 16; 03 sec. 17 #6 y #7)"),
        ("QA-DAILY-FIELDS", "sin level, difficulty, solution, safe_path ni qa (03 sec. 16; 08 sec. 4.1)"),
        ("QA-DAILY-RELIEF", "relief vacio (03 sec. 16)"),
        ("QA-DAILY-WATER", "world = offset + (dia mod modulo) y su water_base de la especificacion (R-Q2 [P])"),
        ("QA-DAILY-ASTAR", "resoluble con A* (w = 1, run; presupuesto del pipeline) (03 sec. 17 #1)"),
        ("QA-DAILY-PAR", "par >= 2K (03 sec. 17 #2)"),
        ("QA-DAILY-BOT", "bot_fail x40 con la semilla del tablero dentro de la ventana de la banda: compuerta (03 sec. 16; 03 sec. 17 #3; R-Q11 (a))"),
        ("QA-DAILY-REPRO", "el generador reproduce el tablero desde su banda y dia (03 sec. 17 #10)"),
        ("QA-DAILY-SIZE", "pack < 2 MB sin comprimir, tambien con --partial (R5 del encargo; 03 sec. 16)"),
        ("QA-DAILY-REPEAT-SHAPE", "aviso: forma distinta de la de los 200 dias anteriores de su banda, con la vuelta del ciclo (728 -> 0) (03 sec. 17 #9; R-Q10)"),
        ("QA-DAILY-REPEAT-HAMMING", "aviso: Hamming >= 25 % con los 20 dias anteriores de su banda, con la vuelta del ciclo (03 sec. 17 #9; R-Q10)"),
        ("QA-DAILY-EARLY-TRAPS", "aviso: trampas tempranas dentro del tope de la etiqueta que da el bot (Q16 B; 03 sec. 17 #3; R-Q10)"),
    ];

    /// <summary>Lee el pack; null y <paramref name="error"/> si ni siquiera es un objeto JSON con "levels".</summary>
    public static DailyQaPack? Read(string path, out string? error)
    {
        error = null;
        byte[] bytes;
        JsonNode? node;
        try
        {
            bytes = File.ReadAllBytes(path);
            node = JsonNode.Parse(bytes);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            error = $"no se puede leer {path}: {e.Message}";
            return null;
        }

        if (node is not JsonObject root || root["levels"] is not JsonArray levels)
        {
            error = $"{path}: no es un objeto JSON con la lista \"levels\"";
            return null;
        }

        var list = new List<DailyQaLevel>();
        for (int i = 0; i < levels.Count; i++)
        {
            list.Add(ReadLevel(levels[i], i));
        }

        return new DailyQaPack { Path = path, Bytes = bytes.Length, Json = root, Levels = list };
    }

    private static DailyQaLevel ReadLevel(JsonNode? node, int index)
    {
        if (node is not JsonObject o)
        {
            return new DailyQaLevel { Index = index, Json = [], ReadError = "no es un objeto" };
        }

        string? error = null;
        int[] caps = IntArray(o["caps"]) ?? Error(ref error, "caps", Array.Empty<int>());
        int[][] cuias = Matrix(o["cuias"]) ?? Error(ref error, "cuias", Array.Empty<int[]>());
        string[] palette = o["palette"] is JsonArray pa && pa.All(x => Text(x) is not null) ? pa.Select(x => Text(x)!).ToArray() : Error(ref error, "palette", Array.Empty<string>());
        int? units = Int(o["units_per_color"]);
        if (units is null)
        {
            error ??= "units_per_color no es un entero (el Reto usa 4 en todas las cuias)";
        }

        return new DailyQaLevel
        {
            Index = index,
            Json = o,
            Id = Text(o["id"]),
            Seed = Text(o["seed"]),
            Band = Int(o["slot"]?["band"]),
            Day = Int(o["slot"]?["day"]),
            World = Int(o["world"]),
            WaterBase = Text(o["water_base"]),
            Palette = palette,
            Units = units,
            Caps = caps,
            Cuias = cuias,
            RiskPairs = Int(o["risk_pairs"]),
            ReadError = error,
        };
    }

    /// <summary>Recalcula cada tablero legible (en paralelo, un motor de métricas por hilo).</summary>
    public static List<DailyEval> Evaluate(DailyQaPack pack)
    {
        var results = new ConcurrentDictionary<int, DailyEval>();
        using var engines = new ThreadLocal<MetricsEngine>(() => new MetricsEngine());
        Parallel.ForEach(pack.Levels, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, lv =>
        {
            results[lv.Index] = EvaluateOne(lv, engines.Value!);
        });

        return pack.Levels.Select(l => results[l.Index]).ToList();
    }

    private static DailyEval EvaluateOne(DailyQaLevel lv, MetricsEngine metrics)
    {
        if (lv.ReadError is not null || lv.Units is null)
        {
            return new DailyEval { Level = lv, BuildError = lv.ReadError };
        }

        BoardBuildResult built;
        try
        {
            built = BoardBuilder.Build(lv.Caps, lv.Cuias, null, null, UnitsPerColor.Uniform(lv.Units.Value));
        }
        catch (ArgumentException e)
        {
            return new DailyEval { Level = lv, BuildError = e.Message };
        }

        if (!built.IsValid)
        {
            return new DailyEval { Level = lv, BuildError = built.Error };
        }

        BoardState state = built.State!;
        string? validation = LevelValidation.Validate(state);
        if (validation is not null)
        {
            return new DailyEval { Level = lv, Validation = validation };
        }

        string seed = lv.Seed ?? "";
        return new DailyEval
        {
            Level = lv,
            Par = metrics.Par(state),
            Bot = metrics.BotFail(state, seed),
            EarlyTraps = metrics.EarlyTraps(state),
        };
    }

    public static IEnumerable<DailyIssue> Header(DailyQaPack pack, DailySpec spec)
    {
        JsonObject h = pack.Json;
        if (Text(h["kind"]) != "daily")
        {
            yield return new DailyIssue("QA-DAILY-HEADER", "-", $"kind = {Text(h["kind"]) ?? "ausente"} (debe ser daily)");
        }

        if (Text(h["pack_id"]) != spec.PackId)
        {
            yield return new DailyIssue("QA-DAILY-HEADER", "-", $"pack_id = {Text(h["pack_id"]) ?? "ausente"} (la especificacion dice {spec.PackId})");
        }

        var pool = h["pool"] is JsonArray p ? p.Select(Text).Where(t => t is not null).ToHashSet(StringComparer.Ordinal) : [];
        int? kMin = Int(h["k_min"]);
        int? kMax = Int(h["k_max"]);
        foreach (DailyQaLevel lv in pack.Levels.Where(l => l.ReadError is null))
        {
            string? outside = lv.Palette.FirstOrDefault(t => !pool.Contains(t));
            if (outside is not null)
            {
                yield return new DailyIssue("QA-DAILY-HEADER", lv.Name, $"el tinte {outside} no esta en el pool de la cabecera");
            }

            if (kMin is null || kMax is null || lv.Palette.Length < kMin || lv.Palette.Length > kMax)
            {
                yield return new DailyIssue("QA-DAILY-HEADER", lv.Name, string.Create(CultureInfo.InvariantCulture, $"K = {lv.Palette.Length} fuera de k_min-k_max de la cabecera ({kMin}-{kMax})"));
            }
        }
    }

    public static IEnumerable<DailyIssue> Level(DailyEval e, DailySpec spec, CampaignSpec tints)
    {
        DailyQaLevel lv = e.Level;
        string n = lv.Name;
        JsonObject o = lv.Json;

        foreach (string key in Forbidden.Where(o.ContainsKey))
        {
            yield return new DailyIssue("QA-DAILY-FIELDS", n, $"lleva \"{key}\"");
        }

        if (o["relief"] is not JsonArray relief || relief.Count != 0)
        {
            yield return new DailyIssue("QA-DAILY-RELIEF", n, "relief debe ser [] (03 sec. 16)");
        }

        bool attrsNull = o["attrs"] is JsonArray attrs && attrs.All(a => a is null) && attrs.Count == lv.Caps.Length;
        bool murky = o["murky"] is JsonValue mv && mv.GetValueKind() != JsonValueKind.False;
        bool hidden = o["hidden"] is not JsonArray hd || hd.Count != 0;
        bool intro = o["intro_mechanic"] is not null;
        if (!attrsNull || murky || hidden || intro)
        {
            yield return new DailyIssue("QA-DAILY-MECH", n, $"attrs todo null: {Yes(attrsNull)}; murky: {Yes(murky)}; hidden con datos: {Yes(hidden)}; intro_mechanic: {Yes(intro)}");
        }

        if (lv.ReadError is not null)
        {
            yield return new DailyIssue("QA-DAILY-STRUCT", n, lv.ReadError);
            yield break;
        }

        // Slot, id y seed.
        bool slotOk = lv.Band is int band && lv.Day is int day && DailyIds.Problem(band, day) is null;
        if (!slotOk)
        {
            yield return new DailyIssue("QA-DAILY-SLOT", n, string.Create(CultureInfo.InvariantCulture, $"slot ({lv.Band}, {lv.Day}) fuera de banda 1-3 y dia 0-728"));
            yield break;
        }

        int b = lv.Band!.Value;
        int d = lv.Day!.Value;
        if (lv.Id != DailyIds.Id(b, d) || lv.Seed != DailyIds.Seed(b, d))
        {
            yield return new DailyIssue("QA-DAILY-SLOT", n, $"slot ({b}, {d}) pide id {DailyIds.Id(b, d)} y seed {DailyIds.Seed(b, d)}; lleva {lv.Id} y {lv.Seed}");
        }

        DailyBand db = spec.Band(b);
        DailyWorld w = spec.WorldOfDay(d);
        if (lv.World != w.World || lv.WaterBase != w.WaterBase)
        {
            yield return new DailyIssue("QA-DAILY-WATER", n, $"world {lv.World} y water_base {lv.WaterBase}; el dia {d} pide el mundo {w.World} con {w.WaterBase} (R-Q2 [P])");
        }

        // Forma de la banda.
        int k = lv.Palette.Length;
        int empties = lv.Cuias.Count(c => c.Length == 0);
        int expectedK = db.KOfDay(d);
        var shape = new List<string>();
        if (k != expectedK)
        {
            shape.Add(string.Create(CultureInfo.InvariantCulture, $"K = {k} y el dia {d} de la banda {b} pide {expectedK} ({db.KMin}-{db.KMax}, {spec.KByDay})"));
        }

        if (empties != db.Empties)
        {
            shape.Add(string.Create(CultureInfo.InvariantCulture, $"{empties} vacios (la banda pide {db.Empties})"));
        }

        if (lv.Caps.Any(c => c != db.Capacity) || lv.Caps.Length != lv.Cuias.Length || lv.Units != db.Capacity)
        {
            shape.Add(string.Create(CultureInfo.InvariantCulture, $"caps y units_per_color deben ser {db.Capacity} en todas las cuias"));
        }

        if (lv.Cuias.Any(c => c.Length != 0 && c.Length != db.Capacity))
        {
            shape.Add("hay cuias a medias (el Reto solo lleva cuias llenas y vacios)");
        }

        if (lv.Caps.Length != k + db.Empties)
        {
            shape.Add(string.Create(CultureInfo.InvariantCulture, $"{lv.Caps.Length} cuias y K + vacios = {k + db.Empties}"));
        }

        if (shape.Count > 0)
        {
            yield return new DailyIssue("QA-DAILY-SHAPE", n, string.Join("; ", shape));
        }

        if (lv.Caps.Length > 14)
        {
            yield return new DailyIssue("QA-DAILY-CUIAS-14", n, string.Create(CultureInfo.InvariantCulture, $"{lv.Caps.Length} cuias (como mucho 14, 02 R1)"));
        }

        // Paleta y pares de riesgo.
        var present = lv.Cuias.SelectMany(c => c).Distinct().Order().ToList();
        var engines = new List<int>();
        var palette = new List<string>();
        foreach (string id in lv.Palette)
        {
            TintInfo? t = tints.Tints.FirstOrDefault(x => x.Id == id);
            if (t is null)
            {
                palette.Add($"{id} no es un tinte de {spec.TintsFrom}");
                continue;
            }

            engines.Add(t.Engine);
            if (t.Debut > db.DebutUntil)
            {
                palette.Add(string.Create(CultureInfo.InvariantCulture, $"{id} debuta en el {t.Debut}, despues del {db.DebutUntil} de la banda {b}"));
            }
        }

        if (!engines.Order().SequenceEqual(present))
        {
            palette.Add($"la paleta ({string.Join(",", engines.Order())}) no son los colores de las cuias ({string.Join(",", present)})");
        }

        if (palette.Count > 0)
        {
            yield return new DailyIssue("QA-DAILY-PALETTE", n, string.Join("; ", palette));
        }

        int risk = TintAssigner.RiskCountEngines(tints, present);
        if (lv.RiskPairs != risk)
        {
            yield return new DailyIssue("QA-DAILY-RISK", n, string.Create(CultureInfo.InvariantCulture, $"risk_pairs = {lv.RiskPairs} y los colores tienen {risk} pares de riesgo"));
        }
        else if (!db.RiskFree && risk > db.MaxRiskPairs)
        {
            yield return new DailyIssue("QA-DAILY-RISK", n, string.Create(CultureInfo.InvariantCulture,
                $"{risk} pares de riesgo con K = {k}; el tope de la banda {b} es {db.MaxRiskPairs} (R-Q1 y R-Q1b; nota sec. 1.1)"));
        }

        // Medidas.
        if (e.BuildError is not null || e.Validation is not null)
        {
            yield return new DailyIssue("QA-DAILY-STRUCT", n, e.BuildError is not null ? "BoardBuilder: " + e.BuildError : "LevelValidation: " + e.Validation);
            yield break;
        }

        if (e.Par is not { } par || par.Status != AF.Solver.Search.SolveStatus.Solved)
        {
            yield return new DailyIssue("QA-DAILY-ASTAR", n, e.Par is { Status: AF.Solver.Search.SolveStatus.Unsolvable } ? "irresoluble" : "A* agota el presupuesto del pipeline");
        }
        else if (par.Par < 2 * k)
        {
            yield return new DailyIssue("QA-DAILY-PAR", n, string.Create(CultureInfo.InvariantCulture, $"par {par.Par} < 2K = {2 * k}"));
        }

        if (e.Bot < db.BotFailMin - WindowTolerance || e.Bot > db.BotFailMax + WindowTolerance)
        {
            yield return new DailyIssue("QA-DAILY-BOT", n, string.Create(CultureInfo.InvariantCulture,
                $"bot_fail {e.Bot:0.000} fuera de la ventana {db.BotFailMin:0.00}-{db.BotFailMax:0.00} de la banda {b} (R-Q11 (a): la ventana es compuerta)"));
        }

        // Etiqueta por bot_fail con los umbrales de la especificación de tintes y la tolerancia de CampaignSpec.MeasuredLabel
        // (sin los niveles del primer Difícil y Muy difícil, que son de la campaña).
        string label = e.Bot >= tints.VeryHardThreshold - WindowTolerance ? "very_hard" : e.Bot >= tints.HardThreshold - WindowTolerance ? "hard" : "normal";
        int cap = SlotGenerator.EarlyTrapCap(label);
        if (e.EarlyTraps > cap)
        {
            yield return new DailyIssue("QA-DAILY-EARLY-TRAPS", n, string.Create(CultureInfo.InvariantCulture,
                $"{e.EarlyTraps} trampas tempranas con bot {e.Bot:0.000} ({label}, tope {cap}); solo aviso (R-Q10)"), Warning: true);
        }
    }

    /// <summary>729 días por banda (R-Q2b) sin huecos ni duplicados (con <paramref name="partial"/>, los huecos solo avisan).</summary>
    public static IEnumerable<DailyIssue> Days(DailyQaPack pack, bool partial)
    {
        var seen = new HashSet<(int, int)>();
        foreach (DailyQaLevel lv in pack.Levels.Where(l => l.Band is int b && l.Day is int d && DailyIds.Problem(b, d) is null))
        {
            if (!seen.Add((lv.Band!.Value, lv.Day!.Value)))
            {
                yield return new DailyIssue("QA-DAILY-DAYS", lv.Name, string.Create(CultureInfo.InvariantCulture, $"banda {lv.Band}, dia {lv.Day} repetido"));
            }
        }

        int total = (DailyIds.MaxBand - DailyIds.MinBand + 1) * DailyIds.Days;
        int missing = total - seen.Count;
        if (missing == 0)
        {
            yield break;
        }

        if (partial)
        {
            yield return new DailyIssue("QA-DAILY-DAYS", "-", string.Create(CultureInfo.InvariantCulture,
                $"--partial: muestra parcial con {seen.Count} de {total} tableros; no es el pool completo y no se publica"), Warning: true);
            yield break;
        }

        for (int b = DailyIds.MinBand; b <= DailyIds.MaxBand; b++)
        {
            var have = seen.Where(s => s.Item1 == b).Select(s => s.Item2).ToHashSet();
            if (have.Count == DailyIds.Days)
            {
                continue;
            }

            int first = Enumerable.Range(0, DailyIds.Days).First(d => !have.Contains(d));
            yield return new DailyIssue("QA-DAILY-DAYS", "-", string.Create(CultureInfo.InvariantCulture,
                $"banda {b}: {have.Count} de {DailyIds.Days} dias (falta el {first} y {DailyIds.Days - have.Count - 1} mas); para una muestra, --partial"));
        }
    }

    /// <summary>Siempre, también con --partial (una muestra queda muy por debajo de 2 MB).</summary>
    public static IEnumerable<DailyIssue> Size(long bytes)
    {
        if (bytes >= MaxBytes)
        {
            yield return new DailyIssue("QA-DAILY-SIZE", "-", string.Create(CultureInfo.InvariantCulture, $"{bytes} bytes (como mucho {MaxBytes - 1})"));
        }
    }

    /// <summary>
    /// Anti-repetición por banda con las ventanas de la campaña en días (BoardShape): cada tablero frente a los de su banda de
    /// los 200 (forma) y 20 (Hamming) días anteriores, dando la vuelta al ciclo (el pool se juega con día mod 729: al 728
    /// le sigue el 0) y, antes, frente a otro del mismo día (duplicado). Solo avisos (R-Q10).
    /// </summary>
    public static IEnumerable<DailyIssue> Repetition(DailyQaPack pack)
    {
        foreach (IGrouping<int, DailyQaLevel> g in pack.Levels.Where(l => l.ReadError is null && l.Band is int b && l.Day is int d && DailyIds.Problem(b, d) is null)
            .GroupBy(l => l.Band!.Value).OrderBy(g => g.Key))
        {
            List<DailyQaLevel> seq = g.OrderBy(l => l.Day).ThenBy(l => l.Index).ToList();
            var byDay = seq.GroupBy(l => l.Day!.Value).ToDictionary(x => x.Key, x => x.ToList());
            var shapes = seq.ToDictionary(l => l.Index, l => BoardShape.Shape(l.Caps, l.Cuias, new int[l.Caps.Length]));
            foreach (DailyQaLevel cur in seq)
            {
                DailyQaLevel? shapeHit = Previous(cur, byDay, BoardShape.ShapeWindow).FirstOrDefault(o => shapes[o.Index] == shapes[cur.Index]);
                if (shapeHit is not null)
                {
                    yield return new DailyIssue("QA-DAILY-REPEAT-SHAPE", cur.Name, $"misma forma que {shapeHit.Name}; solo aviso (R-Q10)", Warning: true);
                }

                foreach (DailyQaLevel o in Previous(cur, byDay, BoardShape.HammingWindow))
                {
                    double h = BoardShape.Hamming(o.Cuias, cur.Cuias);
                    if (h < BoardShape.MinHamming)
                    {
                        yield return new DailyIssue("QA-DAILY-REPEAT-HAMMING", cur.Name, string.Create(CultureInfo.InvariantCulture,
                            $"Hamming {h:0.00} con {o.Name} (< 0,25); solo aviso (R-Q10)"), Warning: true);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>Los tableros anteriores de la banda, del más cercano al más lejano: el mismo día (índice menor) y después
    /// los días día − 1 … día − <paramref name="window"/>, módulo 729.</summary>
    private static IEnumerable<DailyQaLevel> Previous(DailyQaLevel cur, Dictionary<int, List<DailyQaLevel>> byDay, int window)
    {
        int day = cur.Day!.Value;
        foreach (DailyQaLevel o in byDay[day].Where(o => o.Index < cur.Index))
        {
            yield return o;
        }

        for (int dist = 1; dist <= Math.Min(window, DailyIds.Days - 1); dist++)
        {
            if (byDay.TryGetValue(((day - dist) % DailyIds.Days + DailyIds.Days) % DailyIds.Days, out List<DailyQaLevel>? list))
            {
                foreach (DailyQaLevel o in list)
                {
                    yield return o;
                }
            }
        }
    }

    /// <summary>QA-DAILY-SCHEMA con la compuerta de campaña (GateChecks.Schema: python tools/validate_schema.py --pack).</summary>
    public static IEnumerable<DailyIssue> Schema(string path) =>
        GateChecks.Schema([new QaPack(path, 0, "", 0, "", "", [], 0, 0, 0, [], 0, 0, 0)])
            .Select(i => new DailyIssue("QA-DAILY-SCHEMA", "-", i.Message));

    public static string? Text(JsonNode? node) =>
        node is JsonValue v && v.GetValueKind() == JsonValueKind.String ? v.GetValue<string>() : null;

    public static int? Int(JsonNode? node) =>
        node is JsonValue v && v.GetValueKind() == JsonValueKind.Number && v.TryGetValue(out int i) ? i : null;

    private static int[]? IntArray(JsonNode? node)
    {
        if (node is not JsonArray a)
        {
            return null;
        }

        var r = new int[a.Count];
        for (int i = 0; i < a.Count; i++)
        {
            if (Int(a[i]) is not int x)
            {
                return null;
            }

            r[i] = x;
        }

        return r;
    }

    private static int[][]? Matrix(JsonNode? node)
    {
        if (node is not JsonArray a)
        {
            return null;
        }

        var r = new int[a.Count][];
        for (int i = 0; i < a.Count; i++)
        {
            if (IntArray(a[i]) is not int[] row)
            {
                return null;
            }

            r[i] = row;
        }

        return r;
    }

    private static T Error<T>(ref string? error, string key, T empty)
    {
        error ??= $"\"{key}\" ausente o con otro tipo";
        return empty;
    }

    private static string Yes(bool b) => b ? "si" : "no";
}
