// --daily (R3 del encargo docs/dev/traspaso/spark-reto-del-rio.md): genera el pool del Reto del río.
//   dotnet run --project tools/level-generator -c Release -- --daily --spec tools/level-generator/specs/daily_v1.json
//       [--bands 1,2,3] [--days 0-729] --out <carpeta> [--report <informe.md>] [--check] [--threads n]
//   - --bands y --days: listas separadas por comas con rangos a-b (p. ej. 0-9,100,200-209); por defecto, todas las bandas y
//     todos los días (0-729); sin repetidos;
//   - escribe <carpeta>/<pack_id>.json (daily_pool_v1.json) y, con --report, el informe; con --check no escribe nada y
//     compara el pack con el del disco byte a byte;
//   - --threads: hilos (por defecto, los del equipo); el pack no depende de ellos.
// Códigos: 0 = generado (o idéntico con --check), aunque haya avisos (días fuera de ventana, R-Q11; pares por encima del tope,
// R-Q1b: los marca el informe y los da como fallo level-qa --daily, R4); 1 = algún día sin tablero, un tablero que no cumple
// lo que promete el pack o --check con un pack distinto o ausente; 2 = uso o especificación ilegible.
// Además, la parte de generación de --self-test daily (GenerationChecks).
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;

namespace AmazonFill.Tools.LevelGenerator.Daily;

internal static partial class DailyCommand
{
    /// <summary>Claves de cada tablero del pack, en su orden (DailyPackWriter.Level).</summary>
    public static readonly string[] LevelKeys =
    [
        "schema", "id", "slot", "world", "seed", "gen", "water_base", "palette", "units_per_color", "caps", "cuias", "attrs",
        "murky", "hidden", "risk_pairs", "intro_mechanic", "skippable", "hint_supported", "relief",
    ];

    private static int DailyUsage(string message)
    {
        Console.Error.WriteLine(AsciiText.Of("level-generator --daily: " + message));
        Console.Error.WriteLine("  --daily --spec <daily_v1.json> [--bands 1,2,3] [--days 0-729] --out <carpeta> [--report <informe.md>] [--check] [--threads n]");
        Console.Error.WriteLine("  --self-test daily");
        return 2;
    }

    private static int RunDaily(string[] args)
    {
        string? specPath = null;
        string? bandsArg = null;
        string? daysArg = null;
        string? outDir = null;
        string? report = null;
        string? threadsArg = null;
        bool check = false;
        for (int i = 1; i < args.Length; i++)
        {
            string a = args[i];
            bool hasValue = i + 1 < args.Length;
            if (a == "--spec" && hasValue && specPath is null)
            {
                specPath = args[++i];
            }
            else if (a == "--bands" && hasValue && bandsArg is null)
            {
                bandsArg = args[++i];
            }
            else if (a == "--days" && hasValue && daysArg is null)
            {
                daysArg = args[++i];
            }
            else if (a == "--out" && hasValue && outDir is null)
            {
                outDir = args[++i];
            }
            else if (a == "--report" && hasValue && report is null)
            {
                report = args[++i];
            }
            else if (a == "--threads" && hasValue && threadsArg is null)
            {
                threadsArg = args[++i];
            }
            else if (a == "--check" && !check)
            {
                check = true;
            }
            else
            {
                return DailyUsage($"argumento no valido, repetido o sin valor: {a}");
            }
        }

        if (specPath is null || outDir is null)
        {
            return DailyUsage(specPath is null ? "falta --spec." : "falta --out.");
        }

        if (!TryParseList(bandsArg ?? "1-3", DailyIds.MinBand, DailyIds.MaxBand, out List<int> bands, out string? bandError))
        {
            return DailyUsage("--bands: " + bandError);
        }

        if (!TryParseList(daysArg ?? "0-729", 0, DailyIds.Days - 1, out List<int> days, out string? dayError))
        {
            return DailyUsage("--days: " + dayError);
        }

        int threads = Environment.ProcessorCount;
        if (threadsArg is not null && (!int.TryParse(threadsArg, NumberStyles.None, CultureInfo.InvariantCulture, out threads) || threads < 1))
        {
            return DailyUsage($"--threads lleva un entero >= 1: {threadsArg}");
        }

        DailySpec spec;
        DailyTints tints;
        try
        {
            spec = DailySpec.Load(specPath);
            tints = LoadTints(spec, specPath);
        }
        catch (Exception e) when (e is DailySpecException or CampaignSpecException)
        {
            Console.Error.WriteLine(AsciiText.Of("level-generator --daily: " + e.Message));
            return 2;
        }

        var sw = Stopwatch.StartNew();
        DailyRun run = DailyRunner.Run(spec, tints, bands, days, threads);
        sw.Stop();
        var errors = new List<string>(run.Errors);
        foreach (DailyBoard b in run.Boards)
        {
            string? problem = DailyPackWriter.CheckBoard(b);
            if (problem is not null)
            {
                errors.Add($"{b.Id}: {problem}");
            }
        }

        foreach (DailyBand band in spec.Bands.Where(b => bands.Contains(b.Band)))
        {
            List<DailyBoard> bs = run.Boards.Where(b => b.Band == band.Band).ToList();
            long ms = run.BandMilliseconds.TryGetValue(band.Band, out long m) ? m : 0;
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"B{band.Band}: {bs.Count} tableros, {bs.Count(b => !b.InWindow)} fuera de ventana, {bs.Count(b => b.UsedFallback)} con respaldo, {bs.Count(b => b.OverRiskCap)} con pares > tope, {bs.Sum(b => b.Tried)} candidatos, {ms / 1000.0:F1} s (suma de tiempos de pared por tablero)"));
            foreach (DailyBoard b in bs.Where(b => !b.InWindow))
            {
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"WARN {b.Id}: bot_fail {b.Bot:F3} fuera de la ventana {band.BotFailMin:F2}-{band.BotFailMax:F2} (R-Q11; el mas cercano de {b.Tried} candidatos)"));
            }

            foreach (IGrouping<int, DailyBoard> g in bs.Where(b => b.OverRiskCap).GroupBy(b => b.K).OrderBy(g => g.Key))
            {
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"WARN B{band.Band} K = {g.Key}: {g.Count()} tableros con {tints.MinPairs(band.Band, g.Key)} pares de riesgo, por encima del tope {band.MaxRiskPairs} (R-Q1b, nota sec. 1.1: la paleta no admite ese K con el tope)"));
            }
        }

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"tiempo: {sw.Elapsed.TotalSeconds:F1} s con {run.Threads} hilos"));
        foreach (string e in errors)
        {
            Console.Error.WriteLine(AsciiText.Of("ERROR " + e));
        }

        if (errors.Count > 0 || run.Boards.Count == 0)
        {
            Console.Error.WriteLine("level-generator --daily: no se escribe el pack (errores arriba).");
            return 1;
        }

        string packText = DailyPackWriter.Write(spec, tints, run.Boards);
        byte[] bytes = PackWriter.Bytes(packText);
        string packPath = Path.Combine(outDir, DailyPackWriter.FileName(spec)).Replace('\\', '/');
        if (check)
        {
            bool same = File.Exists(packPath) && File.ReadAllBytes(packPath).AsSpan().SequenceEqual(bytes);
            Console.WriteLine(same ? $"--check: {packPath} identico" : $"--check: {packPath} difiere o no existe");
            return same ? 0 : 1;
        }

        Directory.CreateDirectory(outDir);
        File.WriteAllBytes(packPath, bytes);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"escrito {packPath} ({bytes.Length} bytes, {run.Boards.Count} tableros)"));
        if (report is not null)
        {
            string command = "dotnet run --project tools/level-generator -- " + string.Join(' ', args.Where((_, i) => !IsThreads(args, i)));
            string? dir = Path.GetDirectoryName(report);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(report, PackWriter.Bytes(DailyReport.Write(spec, run, command, packPath, bytes)));
            Console.WriteLine($"escrito {report}");
        }

        return 0;
    }

    private static bool IsThreads(string[] args, int i) => args[i] == "--threads" || (i > 0 && args[i - 1] == "--threads");

    private static DailyTints LoadTints(DailySpec spec, string specPath)
    {
        string dir = Path.GetDirectoryName(Path.GetFullPath(specPath))!;
        return new DailyTints(CampaignSpec.Load(Path.Combine(dir, spec.TintsFrom)), spec);
    }

    /// <summary>Lista «a,b-c,…» de enteros en [min, max], sin repetidos, en el orden ascendente.</summary>
    public static bool TryParseList(string text, int min, int max, out List<int> values, out string? error)
    {
        values = [];
        error = null;
        var seen = new SortedSet<int>();
        foreach (string part in text.Split(','))
        {
            string[] ends = part.Split('-');
            if (ends.Length is < 1 or > 2
                || !int.TryParse(ends[0], NumberStyles.None, CultureInfo.InvariantCulture, out int lo)
                || !int.TryParse(ends[^1], NumberStyles.None, CultureInfo.InvariantCulture, out int hi)
                || lo > hi)
            {
                error = $"'{part}' no es un entero ni un rango a-b con a <= b";
                return false;
            }

            if (lo < min || hi > max)
            {
                error = string.Create(CultureInfo.InvariantCulture, $"'{part}' fuera de {min}-{max}");
                return false;
            }

            for (int v = lo; v <= hi; v++)
            {
                if (!seen.Add(v))
                {
                    error = string.Create(CultureInfo.InvariantCulture, $"{v} repetido");
                    return false;
                }
            }
        }

        values = [.. seen];
        return true;
    }

    /// <summary>Generación (R3) en --self-test daily: 3 días por banda.</summary>
    private static void GenerationChecks(Action<bool, string> check, DailySpec spec)
    {
        // Listas de --bands / --days.
        check(TryParseList("0-2,5,10-11", 0, 729, out List<int> l, out _) && string.Join(",", l) == "0,1,2,5,10,11", "--days 0-2,5,10-11 = 0,1,2,5,10,11");
        check(!TryParseList("0-730", 0, 729, out _, out _) && !TryParseList("3,1-3", 1, 3, out _, out _) && !TryParseList("2-1", 0, 729, out _, out _)
            && !TryParseList("a", 0, 729, out _, out _) && !TryParseList("", 0, 729, out _, out _) && !TryParseList("-1", 0, 729, out _, out _),
            "--days/--bands rechazan 0-730, repetidos, 2-1, texto, vacio y -1");

        DailyTints tints;
        try
        {
            tints = LoadTints(spec, SpecPath);
        }
        catch (CampaignSpecException e)
        {
            check(false, "tintes de tints_from: " + e.Message);
            return;
        }

        // Mínimo de pares de riesgo con K tintes (nota §1.1): B1 0 y 0; B2 1 y 3; B3 3 y 4.
        string mins = string.Join(" ", spec.Bands.SelectMany(b => Enumerable.Range(b.KMin, b.KMax - b.KMin + 1).Select(k => $"B{b.Band}K{k}={tints.MinPairs(b.Band, k)}")));
        check(mins == "B1K6=0 B1K7=0 B2K8=1 B2K9=3 B3K10=3 B3K11=4", $"minimo de pares de riesgo por K igual a la nota sec. 1.1 ({mins})");
        check(tints.Pool(spec.Band(1)).Count == 7 && tints.Pool(spec.Band(2)).Count == 9 && tints.Pool(spec.Band(3)).Count == 12,
            "pool de tintes por banda: 7, 9 y 12 (nota sec. 1.1)");

        int[] bands = [1, 2, 3];
        int[] days = [0, 1, 2];
        var sw = Stopwatch.StartNew();
        DailyRun parallel = DailyRunner.Run(spec, tints, bands, days, Environment.ProcessorCount);
        DailyRun serial = DailyRunner.Run(spec, tints, bands, days, 1);
        sw.Stop();
        check(parallel.Errors.Count == 0 && parallel.Boards.Count == 9, string.Create(CultureInfo.InvariantCulture,
            $"3 dias por banda: 9 tableros sin errores ({parallel.Boards.Count}; {string.Join("; ", parallel.Errors)})"));
        if (parallel.Boards.Count != 9 || serial.Boards.Count != 9)
        {
            return;
        }

        string a = DailyPackWriter.Write(spec, tints, parallel.Boards);
        string b = DailyPackWriter.Write(spec, tints, serial.Boards);
        check(PackWriter.Bytes(a).AsSpan().SequenceEqual(PackWriter.Bytes(b)), string.Create(CultureInfo.InvariantCulture,
            $"determinista: el pack en paralelo y con un hilo es identico byte a byte ({a.Length} caracteres; {sw.Elapsed.TotalSeconds:F1} s las dos)"));

        DailyRun single = DailyRunner.Run(spec, tints, [2], [1], 1);
        string alone = single.Boards.Count == 1 ? PackWriter.Min(DailyPackWriter.Level(single.Boards[0])) : "";
        check(alone == PackWriter.Min(DailyPackWriter.Level(parallel.Boards.First(x => x.Band == 2 && x.Day == 1))),
            "la semilla reproduce el tablero d2_n0001 generado solo (03 sec. 17 #10)");

        foreach (DailyBoard x in parallel.Boards)
        {
            string? problem = DailyPackWriter.CheckBoard(x);
            check(problem is null && x.Par >= 2 * x.K && x.K == spec.Band(x.Band).KOfDay(x.Day),
                string.Create(CultureInfo.InvariantCulture, $"{x.Id}: K {x.K} del dia, par {x.Par} >= 2K, {x.K} + {x.Empties} cuias de 4, sin mecanicas ({problem ?? "ok"}; bot {x.Bot:F3})"));
        }

        check(parallel.Boards.Where(x => x.Band == 1).All(x => !x.OverRiskCap && x.Palette.RiskPairs == 0)
            && parallel.Boards.First(x => x.Band == 2 && x.Day == 0).Palette.RiskPairs <= 1
            && parallel.Boards.First(x => x.Band == 2 && x.Day == 1) is { OverRiskCap: true, Palette.RiskPairs: 3 }
            && parallel.Boards.Where(x => x.Band == 3).All(x => x.OverRiskCap && x.Palette.RiskPairs == tints.MinPairs(3, x.K)),
            "pares de riesgo: B1 0; B2 K 8 <= 1; B2 K 9 y B3 marcados con el minimo posible (R-Q1b)");

        // El JSON: cabecera daily y lo que el pack no lleva.
        JsonNode pack = JsonNode.Parse(a)!;
        check(pack["kind"]?.GetValue<string>() == "daily" && pack["pack_schema"]?.GetValue<int>() == DailyPackWriter.PackSchema
            && pack["pack_id"]?.GetValue<string>() == spec.PackId && pack["levels"]!.AsArray().Count == 9
            && pack["world"] is null && pack["lap"] is null && pack["water_base"] is null,
            "cabecera: kind daily, pack_schema 2, pack_id daily_pool_v1, sin world, lap ni water_base");
        check(!a.Contains("0x", StringComparison.Ordinal) && a.EndsWith("]}\n", StringComparison.Ordinal) && !a.Contains('\r'),
            "JSON minificado con LF y sin hexadecimal");
        bool keys = true;
        bool slots = true;
        bool empty = true;
        foreach (JsonNode? level in pack["levels"]!.AsArray())
        {
            JsonObject o = level!.AsObject();
            keys &= o.Select(kv => kv.Key).SequenceEqual(LevelKeys);
            int band = o["slot"]!["band"]!.GetValue<int>();
            int day = o["slot"]!["day"]!.GetValue<int>();
            DailyWorld w = spec.WorldOfDay(day);
            slots &= o["id"]!.GetValue<string>() == DailyIds.Id(band, day) && o["seed"]!.GetValue<string>() == DailyIds.Seed(band, day)
                && o["world"]!.GetValue<int>() == w.World && o["water_base"]!.GetValue<string>() == w.WaterBase;
            empty &= o["attrs"]!.AsArray().All(x => x is null) && !o["murky"]!.GetValue<bool>() && o["hidden"]!.AsArray().Count == 0
                && o["relief"]!.AsArray().Count == 0 && o["intro_mechanic"] is null && !o["skippable"]!.GetValue<bool>()
                && o["caps"]!.AsArray().All(x => x!.GetValue<int>() == 4)
                && o["palette"]!.AsArray().Count == o["cuias"]!.AsArray().Count(c => c!.AsArray().Count > 0);
        }

        check(keys, "tableros: claves en orden y sin level, difficulty, solution, safe_path ni qa");
        check(slots, "tableros: slot coherente con id y seed; world = 1 + (dia mod 9) y su water_base (R-Q2 [P])");
        check(empty, "tableros: sin mecanicas ni ocultas, relief [], intro_mechanic null, skippable false, caps 4 y K = colores");
    }
}
