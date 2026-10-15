// Lectura de paquetes af-level/1 (08 §4.1) con System.Text.Json, sin dependencias NuGet. El esquema lo valida
// python tools/validate_schema.py --pack en el mismo paso del CI; aquí solo se leen los campos que comprueba level-qa.
// Las variantes de relief con la tinaja llevan su máscara en hidden (pack_schema 3; decisión del dueño del 2026-10-14,
// opción a de la revisión de P2a; 08 §4.1): se lee y se recuerda si la clave está, para QA-RELIEF-MASK.
// key2 de la Flor de vitória-régia (firma de pack_schema 3, D-023 punto 5; P5): se lee en el nivel y en las variantes de
// relief, para que LevelValidation, el par y el hash del tablero sean los del pack.
using System.Text;
using System.Text.Json;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa;

/// <summary>Variante de alivio; <paramref name="HiddenPresent"/>: la variante publica la clave hidden (solo con la tinaja).</summary>
internal sealed record QaRelief(string Type, int? Removed, BoardData Board, int? Par, double BotFail, bool HiddenPresent);

internal sealed record QaLevel(
    string Pack, int Index, string Id, int Level, int World, string Seed, string WaterBase,
    string[] Palette, int UnitsDefault, IReadOnlyDictionary<int, int> UnitsOverrides,
    BoardData Board, bool Murky, string Label, double BotFail, int Par, double Branching, double Traps,
    int Complexity, double D, string Band, bool HasParApprox, int RiskPairs, string? Intro, bool Skippable,
    bool HintSupported, List<QaRelief> Relief, string[] ForbiddenKeys, string Source)
{
    public int K => Palette.Length;
}

internal sealed record QaPack(
    string Path, int PackSchema, string PackId, int World, string WorldId, string WaterBase, string[] Pool,
    int KMin, int KMax, int MaxRiskPairs, List<QaLevel> Levels, long Bytes, long BytesWithoutRelief, long HeaderBytes);

internal sealed class PackFormatException(string message) : Exception(message);

internal static class PackReader
{
    private static readonly string[] Forbidden = ["solution", "safe_path", "qa"];

    public static QaPack Read(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        using JsonDocument doc = JsonDocument.Parse(bytes);
        JsonElement root = doc.RootElement;
        var levels = new List<QaLevel>();
        string packId = Str(root, "pack_id");
        int index = 0;
        foreach (JsonElement lv in root.GetProperty("levels").EnumerateArray())
        {
            levels.Add(Level(path, packId, index++, lv));
        }

        return new QaPack(
            path,
            root.GetProperty("pack_schema").GetInt32(),
            packId,
            root.TryGetProperty("world", out JsonElement w) ? w.GetInt32() : 0,
            root.TryGetProperty("world_id", out JsonElement wid) ? wid.GetString() ?? "" : "",
            root.TryGetProperty("water_base", out JsonElement wb) ? wb.GetString() ?? "" : "",
            root.GetProperty("pool").EnumerateArray().Select(e => e.GetString() ?? "").ToArray(),
            root.GetProperty("k_min").GetInt32(),
            root.GetProperty("k_max").GetInt32(),
            root.GetProperty("max_risk_pairs").GetInt32(),
            levels,
            bytes.LongLength,
            WithoutRelief(root, keepLevels: true),
            WithoutRelief(root, keepLevels: false));
    }

    /// <summary>Bytes del pack minificado sin las variantes de relief (03 §17 #11).</summary>
    private static long WithoutRelief(JsonElement root, bool keepLevels)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            foreach (JsonProperty p in root.EnumerateObject())
            {
                if (p.Name != "levels")
                {
                    p.WriteTo(writer);
                    continue;
                }

                writer.WritePropertyName("levels");
                writer.WriteStartArray();
                foreach (JsonElement lv in p.Value.EnumerateArray().Where(_ => keepLevels))
                {
                    writer.WriteStartObject();
                    foreach (JsonProperty q in lv.EnumerateObject().Where(q => q.Name != "relief"))
                    {
                        q.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        return stream.Length;
    }

    private static QaLevel Level(string pack, string packId, int index, JsonElement lv)
    {
        JsonElement diff = lv.GetProperty("difficulty");
        JsonElement upc = lv.GetProperty("units_per_color");
        int unitsDefault = 4;
        var overrides = new Dictionary<int, int>();
        if (upc.ValueKind == JsonValueKind.Number)
        {
            unitsDefault = upc.GetInt32();
        }
        else
        {
            foreach (JsonProperty p in upc.EnumerateObject())
            {
                overrides[int.Parse(p.Name, System.Globalization.CultureInfo.InvariantCulture)] = p.Value.GetInt32();
            }
        }

        BoardData board = Board(lv.GetProperty("caps"), lv.GetProperty("cuias"), lv.GetProperty("attrs"), lv.GetProperty("hidden"));
        var relief = new List<QaRelief>();
        if (lv.TryGetProperty("relief", out JsonElement rel))
        {
            foreach (JsonElement r in rel.EnumerateArray())
            {
                JsonElement attrs = r.TryGetProperty("attrs", out JsonElement a) ? a : default;
                bool hiddenPresent = r.TryGetProperty("hidden", out JsonElement rh);
                relief.Add(new QaRelief(
                    Str(r, "type"),
                    r.TryGetProperty("removed", out JsonElement rm) ? rm.GetInt32() : null,
                    Board(r.GetProperty("caps"), r.GetProperty("cuias"), attrs, hiddenPresent ? rh : default),
                    r.GetProperty("par").ValueKind == JsonValueKind.Number ? r.GetProperty("par").GetInt32() : null,
                    r.GetProperty("bot_fail").GetDouble(),
                    hiddenPresent));
            }
        }

        JsonElement intro = lv.GetProperty("intro_mechanic");
        return new QaLevel(
            pack,
            index,
            Str(lv, "id"),
            lv.GetProperty("level").GetInt32(),
            lv.GetProperty("world").GetInt32(),
            Str(lv, "seed"),
            Str(lv, "water_base"),
            lv.GetProperty("palette").EnumerateArray().Select(e => e.GetString() ?? "").ToArray(),
            unitsDefault,
            overrides,
            board,
            lv.GetProperty("murky").GetBoolean(),
            Str(diff, "label"),
            diff.GetProperty("bot_fail").GetDouble(),
            diff.GetProperty("par").GetInt32(),
            diff.TryGetProperty("branching", out JsonElement br) ? br.GetDouble() : 0,
            diff.TryGetProperty("traps", out JsonElement tr) ? tr.GetDouble() : 0,
            diff.TryGetProperty("complexity", out JsonElement cx) ? (int)cx.GetDouble() : 0,
            diff.TryGetProperty("D", out JsonElement d) ? d.GetDouble() : 0,
            Str(diff, "band"),
            diff.TryGetProperty("par_approx", out _),
            lv.GetProperty("risk_pairs").GetInt32(),
            intro.ValueKind == JsonValueKind.String ? intro.GetString() : null,
            lv.GetProperty("skippable").GetBoolean(),
            lv.GetProperty("hint_supported").GetBoolean(),
            relief,
            Forbidden.Where(f => lv.TryGetProperty(f, out _)).ToArray(),
            lv.GetProperty("gen").TryGetProperty("source", out JsonElement src) ? src.GetString() ?? "" : "");
    }

    private static BoardData Board(JsonElement caps, JsonElement cuias, JsonElement attrs, JsonElement hidden)
    {
        int[] c = caps.EnumerateArray().Select(e => e.GetInt32()).ToArray();
        int[][] doses = cuias.EnumerateArray().Select(x => x.EnumerateArray().Select(e => e.GetInt32()).ToArray()).ToArray();
        MechanicSpec[] specs = new MechanicSpec[doses.Length];
        if (attrs.ValueKind == JsonValueKind.Array)
        {
            int i = 0;
            foreach (JsonElement a in attrs.EnumerateArray())
            {
                if (i >= specs.Length)
                {
                    throw new PackFormatException("attrs mas largo que cuias");
                }

                specs[i++] = a.ValueKind == JsonValueKind.Null ? MechanicSpec.None : Attr(a);
            }

            if (i != specs.Length)
            {
                throw new PackFormatException("attrs y cuias de distinta longitud");
            }
        }

        int[][] hid = hidden.ValueKind == JsonValueKind.Array
            ? hidden.EnumerateArray().Select(h => h.EnumerateArray().Select(e => e.GetInt32()).ToArray()).ToArray()
            : [];
        return new BoardData(c, doses, specs, hid);
    }

    private static MechanicSpec Attr(JsonElement a)
    {
        if (!MechanicTypes.TryParse(Str(a, "type"), out MechanicType type))
        {
            throw new PackFormatException($"tipo de mecanica desconocido: {Str(a, "type")}");
        }

        return new MechanicSpec(
            type,
            a.TryGetProperty("key", out JsonElement k) ? k.GetInt32() : 0,
            a.TryGetProperty("partner", out JsonElement p) ? p.GetInt32() : -1,
            a.TryGetProperty("n", out JsonElement n) ? n.GetInt32() : 0,
            a.TryGetProperty("k", out JsonElement kk) ? kk.GetInt32() : 0,
            a.TryGetProperty("key2", out JsonElement k2) ? k2.GetInt32() : 0);
    }

    private static string Str(JsonElement e, string key) =>
        e.TryGetProperty(key, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    public static string ReadAllText(string path) => File.ReadAllText(path, Encoding.UTF8);
}
