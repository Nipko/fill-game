// Reto del río (daily_challenge; 03 §16): entrada de level-generator para --daily y --self-test daily (R2-R3 del encargo
// docs/dev/traspaso/spark-reto-del-rio.md). Program.cs despacha aquí antes de su --self-test genérico.
//   - --self-test daily (R2): ids y semillas de DailyIds (d2_n0417 <-> amazonfill:v1:daily:b2:d417, días 0 y 728, bandas
//     1-3 y los rechazos de la banda 0 y 4 y de los días -1 y 729, el padding exacto), sus patrones frente a los del esquema
//     schemas/af-level-1.schema.json (rama kind = daily), los 2.187 ids y semillas distintos y de ida y vuelta, que
//     specs/daily_v1.json carga con los valores de 03 §16 y de la nota docs/dev/reto-del-rio.md §13, y que una especificación
//     mal formada (mutaciones en memoria de la versionada) se rechaza con su mensaje. Desde R3, además, la generación
//     (DailyCommand.Generate.cs): 3 días por banda, determinismo byte a byte y lo que el pack promete.
//   - --daily (R3): genera el pool (DailyRunner, DailyPackWriter y DailyReport; DailyCommand.Generate.cs).
// Imprime ASCII (PASS / FAIL). Códigos: 0 = todo PASS; 1 = algún FAIL; 2 = uso, o la especificación o el esquema no se
// encuentran. Desde la raíz del repositorio.
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AmazonFill.Tools.LevelGenerator.Campaign;

namespace AmazonFill.Tools.LevelGenerator.Daily;

internal static partial class DailyCommand
{
    public const string SpecPath = "tools/level-generator/specs/daily_v1.json";
    public const string SchemaPath = "schemas/af-level-1.schema.json";

    public static int Run(string[] args)
    {
        if (args.Length == 2 && args[0] == "--self-test" && args[1] == "daily")
        {
            return SelfTest();
        }

        if (args.Length >= 1 && args[0] == "--daily")
        {
            return RunDaily(args);
        }

        return DailyUsage("uso del Reto del rio: --daily ... o --self-test daily.");
    }

    /// <summary>Mutaciones de specs/daily_v1.json que Parse tiene que rechazar y el texto que debe llevar el mensaje.</summary>
    private static readonly (string What, Action<JsonObject> Mutate, string Reason)[] Malformed =
    [
        ("clave desconocida en la raiz", r => r["comment"] = "x", "clave desconocida 'comment'"),
        ("spec_schema 2", r => r["spec_schema"] = 2, "\"spec_schema\" debe ser 1"),
        ("days 730", r => r["days"] = 730, "\"days\" debe ser 729"),
        ("pour_mode single", r => r["pour_mode"] = "single", "\"pour_mode\" debe ser \"run\""),
        ("tints_from inexistente", r => r["tints_from"] = "no_existe.json", "\"tints_from\" (no_existe.json) no existe"),
        ("k_by_day desconocido", r => r["k_by_day"] = "random", "\"k_by_day\" (random) desconocido"),
        ("sin bandas", r => r.Remove("bands"), "falta la lista \"bands\""),
        ("falta la banda 3", r => Bands(r).RemoveAt(2), "bands: falta la banda 3"),
        ("banda 2 repetida", r => Band(r, 2)["band"] = 2, "bands[2]: banda 2 repetida"),
        ("banda 0", r => Band(r, 0)["band"] = 0, "bands[0]: \"band\" debe ser un entero 1-3"),
        ("banda 4", r => Band(r, 2)["band"] = 4, "bands[2]: \"band\" debe ser un entero 1-3"),
        ("clave desconocida en una banda", r => Band(r, 1)["windows"] = 1, "bands[1]: clave desconocida 'windows'"),
        ("K minimo > maximo", r => Band(r, 0)["k"] = new JsonArray(7, 6), "(banda 1): \"k\" [7, 6] debe cumplir 2 <= minimo <= maximo"),
        ("K con un solo valor", r => Band(r, 0)["k"] = new JsonArray(6), "(banda 1): \"k\" debe ser [minimo, maximo]"),
        ("K decimal", r => Band(r, 0)["k"] = new JsonArray(6.5, 7), "(banda 1): \"k\" debe ser [minimo, maximo]"),
        ("0 vacios", r => Band(r, 1)["empties"] = 0, "(banda 2): \"empties\" debe ser un entero >= 1"),
        ("K + vacios > 14", r => { Band(r, 2)["k"] = new JsonArray(10, 14); }, "(banda 3): K maximo + vacios = 14 + 1 = 15 > 14 cuias"),
        ("capacidad 5", r => Band(r, 0)["capacity"] = 5, "(banda 1): \"capacity\" debe ser 4"),
        ("ventana min > max", r => Band(r, 0)["bot_fail"] = Window(0.6, 0.4), "(banda 1): \"bot_fail\" debe cumplir 0 <= min <= max <= 1"),
        ("ventana max > 1", r => Band(r, 2)["bot_fail"] = Window(0.75, 1.2), "(banda 3): \"bot_fail\" debe cumplir 0 <= min <= max <= 1"),
        ("ventana min < 0", r => Band(r, 0)["bot_fail"] = Window(-0.1, 0.6), "(banda 1): \"bot_fail\" debe cumplir 0 <= min <= max <= 1"),
        ("ventana en texto", r => Band(r, 0)["bot_fail"] = new JsonObject { ["min"] = "0.4", ["max"] = 0.6 }, "(banda 1): \"bot_fail\" debe cumplir"),
        ("0 candidatos", r => Band(r, 0)["max_candidates"] = 0, "(banda 1): \"max_candidates\" debe ser un entero 1-30"),
        ("31 candidatos", r => Band(r, 0)["max_candidates"] = 31, "(banda 1): \"max_candidates\" debe ser un entero 1-30"),
        ("fuente desconocida", r => Rule(r, 2, 0)["gen"] = "inverse", "\"gen\" (inverse) desconocida; solo walk, deep o fill"),
        ("fill sin e", r => Rule(r, 0, 0).Remove("e"), "fill lleva \"e\" igual a los vacios de la banda (2;"),
        ("fill con otros vacios", r => Source(r, 0)["fallback"] = new JsonObject { ["gen"] = "fill", ["e"] = 1 }, "source.fallback: fill lleva \"e\" igual a los vacios de la banda (2;"),
        ("deep con e", r => Rule(r, 2, 0)["e"] = 1, "\"e\" solo vale con fill (no con deep)"),
        ("reglas solapadas", r => Source(r, 2)["rules"]!.AsArray().Add(new JsonObject { ["k"] = new JsonArray(11, 11), ["gen"] = "deep" }),
            "source.rules[1]: \"k\" [11, 11] se solapa con otra regla ([10, 11])"),
        ("K sin regla", r => Rule(r, 2, 0)["k"] = new JsonArray(10, 10), "(banda 3): source: ninguna regla cubre K = 11"),
        ("respaldo vacio", r => Source(r, 0)["fallback"] = new JsonArray(), "(banda 1): source: \"fallback\" vacio; null si no hay respaldo"),
        ("respaldo de la lista desconocido", r => Source(r, 0)["fallback"]![1]!["gen"] = "inverse", "source.fallback[1]: \"gen\" (inverse) desconocida"),
        ("nodes con walk", r => Source(r, 0)["fallback"]![1]!["nodes"] = 60000, "source.fallback[1]: \"nodes\" solo vale con deep"),
        ("nodes por debajo de 1.000", r => Source(r, 0)["fallback"]![2]!["nodes"] = 500, "source.fallback[2]: \"nodes\" solo vale con deep y debe ser un entero 1000-300000"),
        ("regla fuera de la banda", r => Rule(r, 0, 0)["k"] = new JsonArray(5, 7), "\"k\" [5, 7] fuera de la banda (6-7)"),
        ("regla con K minimo > maximo", r => Rule(r, 2, 0)["k"] = new JsonArray(11, 10), "source.rules[0]: \"k\" [11, 10] con minimo > maximo"),
        ("sin fallback", r => Source(r, 2).Remove("fallback"), "(banda 3): source: falta \"fallback\""),
        ("paleta sin status", r => Palette(r, 1).Remove("status"), "(banda 2): palette: falta \"status\""),
        ("max_risk_pairs negativo", r => Palette(r, 0)["max_risk_pairs"] = -1, "(banda 1): palette: \"max_risk_pairs\" debe ser un entero 0-10"),
        ("max_risk_pairs con otro texto", r => Palette(r, 2)["max_risk_pairs"] = "sin tope", "(banda 3): palette: \"max_risk_pairs\" debe ser un entero 0-10 o \"libre\""),
        ("debut_until 0", r => Palette(r, 2)["debut_until"] = 0, "(banda 3): palette: \"debut_until\" debe ser un entero >= 1"),
        ("agua sin status", r => Water(r).Remove("status"), "water: falta \"status\""),
        ("falta un mundo", r => Water(r)["worlds"]!.AsArray().RemoveAt(4), "water: falta el mundo 5"),
        ("world_id de otro mundo", r => Water(r)["worlds"]!.AsArray()[1]!["world"] = 1, "water.worlds[1]: \"world_id\" (w02_tapajos) debe empezar por w01_"),
        ("mundo repetido", r => Water(r)["worlds"]!.AsArray()[1] = new JsonObject { ["world"] = 1, ["world_id"] = "w01_estuario", ["water_base"] = "white" }, "water.worlds[1]: mundo 1 repetido"),
        ("agua desconocida", r => Water(r)["worlds"]!.AsArray()[0]!["water_base"] = "muddy", "water.worlds[0]: \"water_base\" (muddy) debe ser black, white o clear"),
        ("modulo 0", r => Water(r)["world_of_day"]!["modulo"] = 0, "water.world_of_day: \"offset\" y \"modulo\" deben ser enteros >= 1"),
        ("modulo 8 con 9 mundos", r => Water(r)["world_of_day"]!["modulo"] = 8, "\"modulo\" 8 no divide a los 729 dias del pool"),
        ("modulo 10 con 10 mundos", r => { Water(r)["world_of_day"]!["modulo"] = 10; Water(r)["worlds"]!.AsArray().Add(new JsonObject { ["world"] = 10, ["world_id"] = "w10_alto_negro", ["water_base"] = "black" }); },
            "water.world_of_day: \"modulo\" 10 no divide a los 729 dias del pool (R-Q2b"),
        ("modulo 3 con 9 mundos", r => Water(r)["world_of_day"]!["modulo"] = 3, "water: 9 mundos, pero world_of_day solo usa 3"),
    ];

    private static int SelfTest()
    {
        int failures = 0;
        void Check(bool ok, string what)
        {
            Console.WriteLine(AsciiText.Of((ok ? "PASS " : "FAIL ") + what));
            failures += ok ? 0 : 1;
        }

        if (!File.Exists(SpecPath) || !File.Exists(SchemaPath))
        {
            Console.Error.WriteLine($"level-generator --self-test daily: no encuentro {SpecPath} o {SchemaPath} (ejecuta desde la raiz del repositorio)");
            return 2;
        }

        // 1. Ids y semillas (03 §16): el ejemplo, los extremos y el padding.
        Check(DailyIds.Id(2, 417) == "d2_n0417" && DailyIds.Seed(2, 417) == "amazonfill:v1:daily:b2:d417", "d2_n0417 <-> amazonfill:v1:daily:b2:d417 (03 sec. 16)");
        Check(DailyIds.SeedOfId("d2_n0417") == "amazonfill:v1:daily:b2:d417" && DailyIds.IdOfSeed("amazonfill:v1:daily:b2:d417") == "d2_n0417", "SeedOfId / IdOfSeed del ejemplo");
        Check(DailyIds.Id(1, 0) == "d1_n0000" && DailyIds.Seed(1, 0) == "amazonfill:v1:daily:b1:d000", "dia 0: d1_n0000 y amazonfill:v1:daily:b1:d000");
        Check(DailyIds.Id(3, 728) == "d3_n0728" && DailyIds.Seed(3, 728) == "amazonfill:v1:daily:b3:d728", "dia 728: d3_n0728 y amazonfill:v1:daily:b3:d728 (729 dias, R-Q2b)");
        Check(DailyIds.Id(1, 5) == "d1_n0005" && DailyIds.Seed(1, 5) == "amazonfill:v1:daily:b1:d005", "padding: cuatro cifras en el id y tres en la semilla (d1_n0005, d005)");
        foreach ((int band, int day, string what) in new[] { (0, 0, "banda 0"), (4, 0, "banda 4"), (2, -1, "dia -1"), (2, 729, "dia 729") })
        {
            string? idError = Throws(() => DailyIds.Id(band, day));
            string? seedError = Throws(() => DailyIds.Seed(band, day));
            string expected = what.StartsWith("banda", StringComparison.Ordinal) ? "fuera de 1-3" : "fuera de 0-728";
            Check(idError is not null && idError.Contains(expected, StringComparison.Ordinal) && seedError is not null,
                $"{what} se rechaza con mensaje claro ({idError ?? "sin error"})");
        }

        foreach (string bad in new[] { "d0_n0001", "d4_n0001", "d2_n0729", "d2_n0417 ", "d2_n417", "d2_n00417", "D2_n0417", "d2_n0417\n", "d2_n٤١٧٠" })
        {
            Check(!DailyIds.TryParseId(bad, out _, out _), $"id rechazado: {Printable(bad)}");
        }

        foreach (string bad in new[] { "amazonfill:v1:daily:b0:d001", "amazonfill:v1:daily:b4:d001", "amazonfill:v1:daily:b2:d729", "amazonfill:v1:daily:b2:d0417", "amazonfill:v1:daily:b2:d41", "amazonfill:v2:daily:b2:d417", "amazonfill:v1:daily:b2:d417\n" })
        {
            Check(!DailyIds.TryParseSeed(bad, out _, out _), $"semilla rechazada: {Printable(bad)}");
        }

        Check(DailyIds.TryParseId("d3_n0728", out int pb, out int pd) && pb == 3 && pd == 728, "TryParseId(d3_n0728) = banda 3, dia 728");

        // 2. Los patrones frente al esquema (rama kind = daily; el esquema es la única copia del formato, contrato 21).
        (string? schemaId, string? schemaSeed) = SchemaPatterns();
        Check(schemaId == DailyIds.IdPattern, $"patron del id igual al del esquema ({schemaId ?? "no encontrado"})");
        Check(schemaSeed == DailyIds.SeedPattern, $"patron de la semilla igual al del esquema ({schemaSeed ?? "no encontrado"})");
        var idRegex = new Regex(schemaId ?? "^$", RegexOptions.CultureInvariant);
        var seedRegex = new Regex(schemaSeed ?? "^$", RegexOptions.CultureInvariant);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var seeds = new HashSet<string>(StringComparer.Ordinal);
        var values = new HashSet<ulong>();
        bool allMatch = true;
        bool roundTrip = true;
        for (int b = DailyIds.MinBand; b <= DailyIds.MaxBand; b++)
        {
            for (int d = 0; d < DailyIds.Days; d++)
            {
                string id = DailyIds.Id(b, d);
                string seed = DailyIds.Seed(b, d);
                allMatch &= idRegex.IsMatch(id) && seedRegex.IsMatch(seed);
                roundTrip &= DailyIds.TryParseId(id, out int ib, out int idd) && ib == b && idd == d
                    && DailyIds.TryParseSeed(seed, out int sb, out int sd) && sb == b && sd == d;
                ids.Add(id);
                seeds.Add(seed);
                values.Add(DailyIds.SeedValue(b, d));
            }
        }

        int total = DailyIds.Days * DailyIds.MaxBand;
        Check(allMatch, string.Create(CultureInfo.InvariantCulture, $"los {total} ids y semillas cumplen las regex del esquema"));
        Check(roundTrip, string.Create(CultureInfo.InvariantCulture, $"los {total} ids y semillas vuelven a su banda y dia"));
        Check(ids.Count == total && seeds.Count == total && values.Count == total,
            string.Create(CultureInfo.InvariantCulture, $"{total} ids, semillas y valores de Prng.SeedFrom distintos ({ids.Count}, {seeds.Count}, {values.Count})"));

        // 3. La especificación versionada carga con los valores de 03 §16 y de la nota §13.
        DailySpec? spec = null;
        try
        {
            spec = DailySpec.Load(SpecPath);
            Check(true, $"{SpecPath} carga");
        }
        catch (DailySpecException e)
        {
            Check(false, $"{SpecPath} carga: {e.Message}");
        }

        if (spec is not null)
        {
            DailyBand b1 = spec.Band(1);
            DailyBand b2 = spec.Band(2);
            DailyBand b3 = spec.Band(3);
            Check(spec.PackId == "daily_pool_v1", "pack_id daily_pool_v1 (03 sec. 16)");
            Check(b1 is { KMin: 6, KMax: 7, Empties: 2, BotFailMin: 0.40, BotFailMax: 0.60 }
                && b2 is { KMin: 8, KMax: 8, Empties: 1, BotFailMin: 0.60, BotFailMax: 0.80 }
                && b3 is { KMin: 10, KMax: 11, Empties: 1, BotFailMin: 0.75, BotFailMax: 1.00 },
                "bandas de 03 sec. 16 con R-Q1b (a): K 6-7/8/10-11, vacios 2/1/1, bot 40-60/60-80/>=75 %");
            Check(spec.Bands.All(b => b.Capacity == 4 && b.MaxCandidates == 30), "capacidad 4 y 30 candidatos en las tres bandas");
            Check(b1.SourceFor(6) is { Gen: "fill", E: 2 } && b1.SourceFor(7) is { Gen: "fill", E: 2 }
                && b1.Fallbacks.Count == 3 && b1.Fallbacks[0] is { Gen: "deep", Nodes: null } && b1.Fallbacks[1].Gen == "walk"
                && b1.Fallbacks[2] is { Gen: "deep", Nodes: 60000 },
                "B1: fill E=2; respaldos deep, walk y deep con 60.000 nodos (R5 [P], R-Q11)");
            Check(b2.SourceFor(8).Gen == "deep" && b2.Fallbacks.Count == 1 && b2.Fallback is { Gen: "fill", E: 1 },
                "B2: deep; respaldo fill E=1 (R5 [P], R-Q11)");
            Check(b3.SourceFor(10).Gen == "deep" && b3.SourceFor(11).Gen == "deep" && b3.Fallbacks.Count == 0, "B3: deep (nota sec. 13)");
            Check(b1 is { DebutUntil: 40, MaxRiskPairs: 0, RiskFree: false } && b2 is { DebutUntil: 80, MaxRiskPairs: 1, RiskFree: false }
                && b3 is { DebutUntil: 200, RiskFree: true, MaxRiskPairs: DailySpec.MaxRiskPairsCap },
                "paleta de R-Q1 y R-Q1b (a): tintes hasta 40/80/200, tope 0/1/libre (libre = 10, todos los pares de 03 sec. 4.5)");
            Check(b2.KOfDay(416) == 8 && b2.KOfDay(417) == 8 && b3.KOfDay(0) == 10 && b3.KOfDay(727) == 11 && b3.KOfDay(728) == 10, "K por dia: par el menor, impar el mayor [P]; B2 siempre 8");
            Check(spec.WorldOfDay(0).World == 1 && spec.WorldOfDay(8).World == 9 && spec.WorldOfDay(9).World == 1 && spec.WorldOfDay(728).World == 9
                && DailyIds.Days % spec.WorldModulo == 0,
                "mundo del dia = 1 + (dia mod 9) y 729 = 81 x 9: el dia 728 es el mundo 9 y la vuelta empieza en el 1, como la fecha (R-Q2b)");
            string waters = string.Join(",", spec.Worlds.Select(w => w.WaterBase));
            Check(waters == "white,clear,black,white,white,white,white,white,white", $"agua base de los mundos 1-9 del glosario sec. 8 ({waters})");
        }

        // 4. Especificaciones mal formadas: cada mutación se rechaza y el mensaje dice por qué.
        string baseText = File.ReadAllText(SpecPath, System.Text.Encoding.UTF8);
        string baseDir = Path.GetDirectoryName(Path.GetFullPath(SpecPath))!;
        foreach ((string what, Action<JsonObject> mutate, string reason) in Malformed)
        {
            JsonObject root = JsonNode.Parse(baseText)!.AsObject();
            mutate(root);
            Rejects(Check, root.ToJsonString(), baseDir, what, reason);
        }

        Rejects(Check, "{ \"spec\": ", baseDir, "JSON ilegible", "JSON ilegible");
        Rejects(Check, "[]", baseDir, "raiz que no es un objeto", "debe ser un objeto JSON");

        // 5. Generación (R3): 3 días por banda.
        if (spec is not null)
        {
            GenerationChecks(Check, spec);
        }

        Console.WriteLine(failures == 0 ? "self-test daily: todo PASS" : $"self-test daily: {failures} FAIL");
        return failures == 0 ? 0 : 1;
    }

    private static void Rejects(Action<bool, string> check, string json, string baseDir, string what, string reason)
    {
        try
        {
            DailySpec.Parse(json, "daily_v1 (mutada)", baseDir);
            check(false, $"rechaza {what}: se acepto");
        }
        catch (DailySpecException e)
        {
            check(e.Message.Contains(reason, StringComparison.Ordinal), $"rechaza {what}: {e.Message}");
        }
    }

    private static (string? Id, string? Seed) SchemaPatterns()
    {
        try
        {
            JsonNode? schema = JsonNode.Parse(File.ReadAllText(SchemaPath, System.Text.Encoding.UTF8));
            foreach (JsonNode? branch in schema?["allOf"]?.AsArray() ?? new JsonArray())
            {
                if (branch?["if"]?["properties"]?["kind"]?["const"]?.GetValue<string>() != "daily")
                {
                    continue;
                }

                JsonNode? props = branch["then"]?["properties"]?["levels"]?["items"]?["properties"];
                return (props?["id"]?["pattern"]?.GetValue<string>(), props?["seed"]?["pattern"]?.GetValue<string>());
            }
        }
        catch (Exception e) when (e is IOException or JsonException or InvalidOperationException)
        {
            Console.Error.WriteLine(AsciiText.Of($"level-generator --self-test daily: no puedo leer los patrones de {SchemaPath}: {e.Message}"));
        }

        return (null, null);
    }

    private static string? Throws(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (ArgumentOutOfRangeException e)
        {
            return e.Message;
        }
    }

    private static string Printable(string s) => s.Replace("\n", "\\n", StringComparison.Ordinal).Replace(" ", "<sp>", StringComparison.Ordinal)
        .Replace("٤١٧٠", "<cifras arabigas>", StringComparison.Ordinal);

    private static JsonArray Bands(JsonObject r) => r["bands"]!.AsArray();

    private static JsonObject Band(JsonObject r, int index) => Bands(r)[index]!.AsObject();

    private static JsonObject Source(JsonObject r, int index) => Band(r, index)["source"]!.AsObject();

    private static JsonObject Rule(JsonObject r, int band, int rule) => Source(r, band)["rules"]!.AsArray()[rule]!.AsObject();

    private static JsonObject Palette(JsonObject r, int index) => Band(r, index)["palette"]!.AsObject();

    private static JsonObject Water(JsonObject r) => r["water"]!.AsObject();

    private static JsonObject Window(double min, double max) => new() { ["min"] = min, ["max"] = max };
}
