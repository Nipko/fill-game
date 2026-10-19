// Especificación del pool del Reto del río (specs/daily_v1.json; R2 del encargo docs/dev/traspaso/spark-reto-del-rio.md):
// lector con validación. Datos de 03 §16 (bandas, K, vacíos y ventana del bot), 03 §4.4 (fuente y rechazo por ventana, con
// un máximo de 30 candidatos), 02 R1 (T <= 14 cuias; capacidad 4), glosario §8 (agua base por mundo) y las decisiones del
// dueño R-Q1 y R-Q1b (paleta: B3 con max_pares_riesgo libre) y R-Q2 (agua), con la propuesta de la nota de diseño
// docs/dev/reto-del-rio.md §13. Lo que es propuesta (la fórmula de R-Q2, las fuentes) va con su campo "status" [P].
//
// Comprueba, con un mensaje que dice dónde y por qué:
//   - claves desconocidas (la raíz, water, cada banda, bot_fail, source, sus reglas y palette): un error tipográfico no pasa;
//   - "spec_schema" 1, "days" 729 (DailyIds.Days; R-Q2b), "pour_mode" run (D-005; el pack asume run), "k_by_day" conocido y
//     "tints_from" presente (y existente junto a la especificación si se lee de disco);
//   - water: world_of_day con offset >= 1 y modulo >= 1 que divide a los días del pool (R-Q2b: así el mundo sigue a la
//     fecha en todas las vueltas), y un mundo por cada valor de offset..offset+modulo-1, sin repetir,
//     con world_id wNN_* de su número y water_base black, white o clear (esquema, $defs.water_base);
//   - bands: exactamente las bandas 1-3, sin repetir; K mínimo >= 2 y <= máximo; vacíos >= 1; K máximo + vacíos <= 14
//     (02 R1); capacidad 4 (nota §13: sin Cuia chica ni alta); ventana 0 <= min <= max <= 1; candidatos 1-30 (03 §4.4);
//   - source: reglas por K que cubren cada K de la banda una sola vez con una fuente conocida (walk, deep o fill; el
//     enum gen.source del esquema); fill lleva "e" igual a los vacíos de la banda y walk y deep no lo llevan (nota §13: sin
//     relajaciones que cambien los vacíos); deep admite "nodes" (1.000-300.000; 03 §4.2); "fallback" null, una fuente o una lista no vacía de fuentes (respaldos en orden,
//     cada uno con sus max_candidates; R-Q11: la ventana es compuerta), con la misma regla;
//   - palette: "status" (la marca de la decisión no se pierde en silencio), debut_until >= 1 y max_risk_pairs 0-10 (esquema)
//     o "libre" (R-Q1b; como la campaña desde el 201, CampaignSpec.FreeRiskPairs): sin tope; se guarda como el número de
//     pares de riesgo de 03 §4.5 (10, MaxRiskPairsCap), que ningún tablero puede superar, y RiskFree = true.
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AmazonFill.Tools.LevelGenerator.Daily;

internal sealed class DailySpecException(string message) : Exception(message);

/// <summary>
/// Fuente de los tableros de un tramo de K (03 §4.4): walk, deep (con <paramref name="Nodes"/> nodos del barajado en
/// profundidad, por defecto los de InverseDeep; más nodos, tableros más difíciles: 03 §4.2) o fill (con E vacíos).
/// </summary>
internal sealed record DailySource(int KMin, int KMax, string Gen, int? E, int? Nodes = null);

internal sealed record DailyWorld(int World, string WorldId, string WaterBase);

internal sealed record DailyBand(
    int Band,
    int KMin,
    int KMax,
    int Empties,
    int Capacity,
    double BotFailMin,
    double BotFailMax,
    int MaxCandidates,
    IReadOnlyList<DailySource> Rules,
    IReadOnlyList<DailySource> Fallbacks,
    int DebutUntil,
    int MaxRiskPairs,
    bool RiskFree)
{
    /// <summary>El primer respaldo, o null si la banda no tiene.</summary>
    public DailySource? Fallback => Fallbacks.Count > 0 ? Fallbacks[0] : null;

    /// <summary>K del tablero de ese día (k_by_day = even_min_odd_max, nota §13 [P]): día par el menor, impar el mayor.</summary>
    public int KOfDay(int day) => day % 2 == 0 ? KMin : KMax;

    /// <summary>Fuente de la regla que cubre ese K.</summary>
    public DailySource SourceFor(int k) =>
        Rules.FirstOrDefault(r => r.KMin <= k && k <= r.KMax)
        ?? throw new DailySpecException(string.Create(CultureInfo.InvariantCulture, $"la banda {Band} no tiene fuente para K = {k}"));
}

internal sealed partial class DailySpec
{
    public const int SpecSchema = 1;
    public const string KByDayEvenMinOddMax = "even_min_odd_max";
    public const int MaxCuias = 14;
    public const int Capacity = 4;
    public const int MaxCandidatesCap = 30;
    public const int MaxRiskPairsCap = 10;

    /// <summary>max_risk_pairs sin tope (R-Q1b; el mismo literal que la campaña, CampaignSpec.FreeRiskPairs).</summary>
    public const string FreeRiskPairs = "libre";

    public static readonly string[] Gens = ["walk", "deep", "fill"];
    public static readonly string[] WaterBases = ["black", "white", "clear"];

    private static readonly string[] RootKeys =
        ["spec", "spec_schema", "description", "status", "pack_id", "days", "pour_mode", "tints_from", "tints_from_note", "k_by_day", "k_by_day_note", "water", "bands"];
    private static readonly string[] WaterKeys = ["status", "world_of_day", "world_of_day_note", "worlds"];
    private static readonly string[] WorldOfDayKeys = ["offset", "modulo"];
    private static readonly string[] WorldKeys = ["world", "world_id", "water_base"];
    private static readonly string[] BandKeys = ["band", "note", "k", "empties", "capacity", "bot_fail", "max_candidates", "source", "palette"];
    private static readonly string[] BotFailKeys = ["min", "max"];
    private static readonly string[] SourceKeys = ["status", "rules", "fallback", "fallback_note"];
    private static readonly string[] RuleKeys = ["k", "gen", "e", "nodes"];
    private static readonly string[] FallbackKeys = ["gen", "e", "nodes"];

    /// <summary>Nodos de deep admitidos: 1.000-300.000 (el presupuesto de A* del pipeline).</summary>
    public const int MinDeepNodes = 1000;
    public const int MaxDeepNodes = 300000;
    private static readonly string[] PaletteKeys = ["status", "debut_until", "max_risk_pairs"];

    public required string Path { get; init; }
    public required string Name { get; init; }
    public required string PackId { get; init; }
    public required string TintsFrom { get; init; }
    public required string KByDay { get; init; }
    public required int WorldOffset { get; init; }
    public required int WorldModulo { get; init; }

    /// <summary>Mundos ordenados por número.</summary>
    public required IReadOnlyList<DailyWorld> Worlds { get; init; }

    /// <summary>Bandas 1-3 en orden (índice = banda − 1).</summary>
    public required IReadOnlyList<DailyBand> Bands { get; init; }

    public DailyBand Band(int band)
    {
        DailyIds.Validate(band, 0);
        return Bands[band - 1];
    }

    /// <summary>Mundo del día del pool: offset + (día mod modulo), 1 + (día mod 9) en daily_v1 (R-Q2, nota §13 [P]).</summary>
    public DailyWorld WorldOfDay(int day)
    {
        DailyIds.Validate(DailyIds.MinBand, day);
        int world = WorldOffset + (day % WorldModulo);
        return Worlds.First(w => w.World == world);
    }

    /// <summary>Lee y valida la especificación de disco; "tints_from" debe existir en la misma carpeta.</summary>
    public static DailySpec Load(string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path, System.Text.Encoding.UTF8);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new DailySpecException($"no se puede leer {path}: {e.Message}");
        }

        return Parse(text, path, System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)));
    }

    /// <summary>
    /// Valida el texto JSON <paramref name="json"/>; <paramref name="where"/> encabeza los mensajes. Con
    /// <paramref name="baseDir"/> comprueba además que "tints_from" exista en esa carpeta.
    /// </summary>
    public static DailySpec Parse(string json, string where, string? baseDir)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException e)
        {
            throw new DailySpecException($"{where}: JSON ilegible: {e.Message}");
        }

        if (node is not JsonObject root)
        {
            throw new DailySpecException($"{where}: debe ser un objeto JSON");
        }

        Keys(root, RootKeys, where);
        string name = RequiredText(root, "spec", where);
        if (Int(root["spec_schema"]) != SpecSchema)
        {
            throw new DailySpecException($"{where}: \"spec_schema\" debe ser {SpecSchema}");
        }

        string packId = RequiredText(root, "pack_id", where);
        if (!PackIdRegex().IsMatch(packId))
        {
            throw new DailySpecException($"{where}: \"pack_id\" ({packId}) debe ser minusculas, cifras y _ (p. ej. daily_pool_v1)");
        }

        if (Int(root["days"]) != DailyIds.Days)
        {
            throw new DailySpecException(string.Create(CultureInfo.InvariantCulture, $"{where}: \"days\" debe ser {DailyIds.Days} (03 sec. 16: dias 0-{DailyIds.Days - 1})"));
        }

        if (Text(root["pour_mode"]) != "run")
        {
            throw new DailySpecException($"{where}: \"pour_mode\" debe ser \"run\" (D-005; el pack no declara el modo y asume run)");
        }

        string tintsFrom = RequiredText(root, "tints_from", where);
        if (baseDir is not null && !File.Exists(System.IO.Path.Combine(baseDir, tintsFrom)))
        {
            throw new DailySpecException($"{where}: \"tints_from\" ({tintsFrom}) no existe junto a la especificacion");
        }

        string kByDay = RequiredText(root, "k_by_day", where);
        if (kByDay != KByDayEvenMinOddMax)
        {
            throw new DailySpecException($"{where}: \"k_by_day\" ({kByDay}) desconocido; solo \"{KByDayEvenMinOddMax}\"");
        }

        (int offset, int modulo, List<DailyWorld> worlds) = ReadWater(root["water"], where + ": water");
        List<DailyBand> bands = ReadBands(root["bands"], where + ": bands");
        return new DailySpec
        {
            Path = where,
            Name = name,
            PackId = packId,
            TintsFrom = tintsFrom,
            KByDay = kByDay,
            WorldOffset = offset,
            WorldModulo = modulo,
            Worlds = worlds,
            Bands = bands,
        };
    }

    private static (int Offset, int Modulo, List<DailyWorld> Worlds) ReadWater(JsonNode? node, string where)
    {
        JsonObject water = Object(node, where);
        Keys(water, WaterKeys, where);
        RequiredText(water, "status", where);
        JsonObject wod = Object(water["world_of_day"], where + ".world_of_day");
        Keys(wod, WorldOfDayKeys, where + ".world_of_day");
        int offset = Int(wod["offset"]) ?? 0;
        int modulo = Int(wod["modulo"]) ?? 0;
        if (offset < 1 || modulo < 1)
        {
            throw new DailySpecException($"{where}.world_of_day: \"offset\" y \"modulo\" deben ser enteros >= 1 (mundo = offset + dia mod modulo)");
        }

        if (water["worlds"] is not JsonArray list)
        {
            throw new DailySpecException($"{where}: falta la lista \"worlds\"");
        }

        var worlds = new List<DailyWorld>();
        for (int i = 0; i < list.Count; i++)
        {
            string at = string.Create(CultureInfo.InvariantCulture, $"{where}.worlds[{i}]");
            JsonObject w = Object(list[i], at);
            Keys(w, WorldKeys, at);
            int world = Int(w["world"]) ?? 0;
            if (world < 1 || world > 13)
            {
                throw new DailySpecException($"{at}: \"world\" debe ser un entero 1-13 (glosario sec. 8)");
            }

            string worldId = RequiredText(w, "world_id", at);
            string prefix = "w" + world.ToString("D2", CultureInfo.InvariantCulture) + "_";
            if (!worldId.StartsWith(prefix, StringComparison.Ordinal) || !PackIdRegex().IsMatch(worldId))
            {
                throw new DailySpecException($"{at}: \"world_id\" ({worldId}) debe empezar por {prefix} (glosario sec. 11)");
            }

            string waterBase = RequiredText(w, "water_base", at);
            if (Array.IndexOf(WaterBases, waterBase) < 0)
            {
                throw new DailySpecException($"{at}: \"water_base\" ({waterBase}) debe ser black, white o clear");
            }

            if (worlds.Any(x => x.World == world))
            {
                throw new DailySpecException(string.Create(CultureInfo.InvariantCulture, $"{at}: mundo {world} repetido"));
            }

            worlds.Add(new DailyWorld(world, worldId, waterBase));
        }

        for (int world = offset; world < offset + modulo; world++)
        {
            if (!worlds.Any(x => x.World == world))
            {
                throw new DailySpecException(string.Create(CultureInfo.InvariantCulture,
                    $"{where}: falta el mundo {world} (world_of_day da los mundos {offset}-{offset + modulo - 1})"));
            }
        }

        if (DailyIds.Days % modulo != 0)
        {
            throw new DailySpecException(string.Create(CultureInfo.InvariantCulture,
                $"{where}.world_of_day: \"modulo\" {modulo} no divide a los {DailyIds.Days} dias del pool (R-Q2b: el mundo tiene que seguir a la fecha en todas las vueltas)"));
        }

        if (worlds.Count != modulo)
        {
            throw new DailySpecException(string.Create(CultureInfo.InvariantCulture,
                $"{where}: {worlds.Count} mundos, pero world_of_day solo usa {modulo} ({offset}-{offset + modulo - 1})"));
        }

        worlds.Sort((a, b) => a.World.CompareTo(b.World));
        return (offset, modulo, worlds);
    }

    private static List<DailyBand> ReadBands(JsonNode? node, string where)
    {
        if (node is not JsonArray list)
        {
            throw new DailySpecException($"{where}: falta la lista \"bands\"");
        }

        var bands = new DailyBand?[DailyIds.MaxBand];
        for (int i = 0; i < list.Count; i++)
        {
            string at = string.Create(CultureInfo.InvariantCulture, $"{where}[{i}]");
            DailyBand band = ReadBand(list[i], at);
            if (bands[band.Band - 1] is not null)
            {
                throw new DailySpecException(string.Create(CultureInfo.InvariantCulture, $"{at}: banda {band.Band} repetida"));
            }

            bands[band.Band - 1] = band;
        }

        for (int b = DailyIds.MinBand; b <= DailyIds.MaxBand; b++)
        {
            if (bands[b - 1] is null)
            {
                throw new DailySpecException(string.Create(CultureInfo.InvariantCulture, $"{where}: falta la banda {b} (deben estar las bandas 1-3)"));
            }
        }

        return bands.Select(b => b!).ToList();
    }

    private static DailyBand ReadBand(JsonNode? node, string at)
    {
        JsonObject o = Object(node, at);
        Keys(o, BandKeys, at);
        int band = Int(o["band"]) ?? 0;
        if (band < DailyIds.MinBand || band > DailyIds.MaxBand)
        {
            throw new DailySpecException($"{at}: \"band\" debe ser un entero 1-3 (03 sec. 16)");
        }

        at = string.Create(CultureInfo.InvariantCulture, $"{at} (banda {band})");
        (int kMin, int kMax) = Pair(o["k"], at + ": \"k\"");
        if (kMin < 2 || kMin > kMax)
        {
            throw new DailySpecException(string.Create(CultureInfo.InvariantCulture, $"{at}: \"k\" [{kMin}, {kMax}] debe cumplir 2 <= minimo <= maximo"));
        }

        int empties = Int(o["empties"]) ?? 0;
        if (empties < 1)
        {
            throw new DailySpecException($"{at}: \"empties\" debe ser un entero >= 1");
        }

        if (kMax + empties > MaxCuias)
        {
            throw new DailySpecException(string.Create(CultureInfo.InvariantCulture,
                $"{at}: K maximo + vacios = {kMax} + {empties} = {kMax + empties} > {MaxCuias} cuias (02 R1)"));
        }

        if (Int(o["capacity"]) != Capacity)
        {
            throw new DailySpecException($"{at}: \"capacity\" debe ser {Capacity} (sin Cuia chica ni alta, nota sec. 13)");
        }

        JsonObject botFail = Object(o["bot_fail"], at + ": bot_fail");
        Keys(botFail, BotFailKeys, at + ": bot_fail");
        double? lo = Number(botFail["min"]);
        double? hi = Number(botFail["max"]);
        if (lo is not { } min || hi is not { } max || min < 0 || min > max || max > 1)
        {
            throw new DailySpecException($"{at}: \"bot_fail\" debe cumplir 0 <= min <= max <= 1 (fraccion de fallo del bot x40)");
        }

        int candidates = Int(o["max_candidates"]) ?? 0;
        if (candidates < 1 || candidates > MaxCandidatesCap)
        {
            throw new DailySpecException(string.Create(CultureInfo.InvariantCulture, $"{at}: \"max_candidates\" debe ser un entero 1-{MaxCandidatesCap} (03 sec. 4.4)"));
        }

        (List<DailySource> rules, List<DailySource> fallbacks) = ReadSource(o["source"], at + ": source", kMin, kMax, empties);

        JsonObject palette = Object(o["palette"], at + ": palette");
        Keys(palette, PaletteKeys, at + ": palette");
        RequiredText(palette, "status", at + ": palette");
        int debutUntil = Int(palette["debut_until"]) ?? 0;
        if (debutUntil < 1)
        {
            throw new DailySpecException($"{at}: palette: \"debut_until\" debe ser un entero >= 1 (ultimo nivel de debut de los tintes de la banda)");
        }

        bool riskFree = Text(palette["max_risk_pairs"]) == FreeRiskPairs;
        int? risk = riskFree ? MaxRiskPairsCap : Int(palette["max_risk_pairs"]);
        if (risk is not { } maxRisk || maxRisk < 0 || maxRisk > MaxRiskPairsCap)
        {
            throw new DailySpecException(string.Create(CultureInfo.InvariantCulture,
                $"{at}: palette: \"max_risk_pairs\" debe ser un entero 0-{MaxRiskPairsCap} o \"{FreeRiskPairs}\""));
        }

        return new DailyBand(band, kMin, kMax, empties, Capacity, min, max, candidates, rules, fallbacks, debutUntil, maxRisk, riskFree);
    }

    private static (List<DailySource> Rules, List<DailySource> Fallbacks) ReadSource(JsonNode? node, string at, int kMin, int kMax, int empties)
    {
        JsonObject o = Object(node, at);
        Keys(o, SourceKeys, at);
        RequiredText(o, "status", at);
        if (o["rules"] is not JsonArray list || list.Count == 0)
        {
            throw new DailySpecException($"{at}: \"rules\" debe ser una lista no vacia");
        }

        var rules = new List<DailySource>();
        for (int i = 0; i < list.Count; i++)
        {
            string ra = string.Create(CultureInfo.InvariantCulture, $"{at}.rules[{i}]");
            JsonObject r = Object(list[i], ra);
            Keys(r, RuleKeys, ra);
            (int lo, int hi) = Pair(r["k"], ra + ": \"k\"");
            if (lo > hi)
            {
                throw new DailySpecException(string.Create(CultureInfo.InvariantCulture, $"{ra}: \"k\" [{lo}, {hi}] con minimo > maximo"));
            }

            if (lo < kMin || hi > kMax)
            {
                throw new DailySpecException(string.Create(CultureInfo.InvariantCulture, $"{ra}: \"k\" [{lo}, {hi}] fuera de la banda ({kMin}-{kMax})"));
            }

            DailySource rule = Gen(r, ra, lo, hi, empties);
            DailySource? overlap = rules.FirstOrDefault(x => x.KMin <= hi && lo <= x.KMax);
            if (overlap is not null)
            {
                throw new DailySpecException(string.Create(CultureInfo.InvariantCulture,
                    $"{ra}: \"k\" [{lo}, {hi}] se solapa con otra regla ([{overlap.KMin}, {overlap.KMax}])"));
            }

            rules.Add(rule);
        }

        for (int k = kMin; k <= kMax; k++)
        {
            if (!rules.Any(x => x.KMin <= k && k <= x.KMax))
            {
                throw new DailySpecException(string.Create(CultureInfo.InvariantCulture, $"{at}: ninguna regla cubre K = {k}"));
            }
        }

        rules.Sort((a, b) => a.KMin.CompareTo(b.KMin));
        var fallbacks = new List<DailySource>();
        JsonNode? fb = o["fallback"];
        if (fb is JsonArray list2)
        {
            if (list2.Count == 0)
            {
                throw new DailySpecException($"{at}: \"fallback\" vacio; null si no hay respaldo");
            }

            for (int i = 0; i < list2.Count; i++)
            {
                string fa = string.Create(CultureInfo.InvariantCulture, $"{at}.fallback[{i}]");
                JsonObject f = Object(list2[i], fa);
                Keys(f, FallbackKeys, fa);
                fallbacks.Add(Gen(f, fa, kMin, kMax, empties));
            }
        }
        else if (fb is not null)
        {
            JsonObject f = Object(fb, at + ".fallback");
            Keys(f, FallbackKeys, at + ".fallback");
            fallbacks.Add(Gen(f, at + ".fallback", kMin, kMax, empties));
        }
        else if (!o.ContainsKey("fallback"))
        {
            throw new DailySpecException($"{at}: falta \"fallback\" (null si no hay)");
        }

        return (rules, fallbacks);
    }

    private static DailySource Gen(JsonObject o, string at, int kMin, int kMax, int empties)
    {
        string? gen = Text(o["gen"]);
        if (gen is null || Array.IndexOf(Gens, gen) < 0)
        {
            throw new DailySpecException($"{at}: \"gen\" ({gen ?? "ausente"}) desconocida; solo walk, deep o fill (03 sec. 4)");
        }

        int? e = null;
        if (gen == "fill")
        {
            e = Int(o["e"]);
            if (e != empties)
            {
                throw new DailySpecException(string.Create(CultureInfo.InvariantCulture,
                    $"{at}: fill lleva \"e\" igual a los vacios de la banda ({empties}; nota sec. 13: sin relajaciones que cambien los vacios)"));
            }
        }
        else if (o.ContainsKey("e"))
        {
            throw new DailySpecException($"{at}: \"e\" solo vale con fill (no con {gen})");
        }

        int? nodes = null;
        if (o.ContainsKey("nodes"))
        {
            nodes = Int(o["nodes"]);
            if (gen != "deep" || nodes is null || nodes < MinDeepNodes || nodes > MaxDeepNodes)
            {
                throw new DailySpecException(string.Create(CultureInfo.InvariantCulture,
                    $"{at}: \"nodes\" solo vale con deep y debe ser un entero {MinDeepNodes}-{MaxDeepNodes}"));
            }
        }

        return new DailySource(kMin, kMax, gen, e, nodes);
    }

    private static void Keys(JsonObject o, string[] allowed, string where)
    {
        foreach (KeyValuePair<string, JsonNode?> kv in o)
        {
            if (Array.IndexOf(allowed, kv.Key) < 0)
            {
                throw new DailySpecException($"{where}: clave desconocida '{kv.Key}'");
            }
        }
    }

    private static JsonObject Object(JsonNode? node, string where) =>
        node as JsonObject ?? throw new DailySpecException($"{where}: debe ser un objeto");

    private static (int Lo, int Hi) Pair(JsonNode? node, string where)
    {
        if (node is JsonArray { Count: 2 } a && Int(a[0]) is { } lo && Int(a[1]) is { } hi)
        {
            return (lo, hi);
        }

        throw new DailySpecException($"{where} debe ser [minimo, maximo] con dos enteros");
    }

    private static string RequiredText(JsonObject o, string key, string where)
    {
        string? s = Text(o[key]);
        return string.IsNullOrWhiteSpace(s) ? throw new DailySpecException($"{where}: falta \"{key}\" (texto no vacio)") : s;
    }

    private static string? Text(JsonNode? node) =>
        node is JsonValue v && v.GetValueKind() == JsonValueKind.String ? v.GetValue<string>() : null;

    private static int? Int(JsonNode? node) =>
        node is JsonValue v && v.GetValueKind() == JsonValueKind.Number && v.TryGetValue(out int i) ? i : null;

    private static double? Number(JsonNode? node) =>
        node is JsonValue v && v.GetValueKind() == JsonValueKind.Number && v.TryGetValue(out double d) ? d : null;

    [GeneratedRegex(@"^[a-z][a-z0-9_]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex PackIdRegex();
}
