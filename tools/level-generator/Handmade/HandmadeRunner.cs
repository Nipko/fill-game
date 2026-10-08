// Modo --handmade: comprueba el tutorial hecho a mano y emite el pack v0 y su informe (docs/dev/prototipo-v0.md b).
//   entrada:  spec.json (tools/level-generator/specs/tutorial_v0.json; tintes por ID del glosario §9) y el modo de
//             vertido (--pour-mode; por defecto PourModes.Default = run, la racha visible que cabe, decisión del dueño
//             D-005 que sustituye a D-001; single, una dosis por vertido, solo pruebas)
//   salida:   <out>/<pack_id>.json   (subconjunto documentado de af-level/1; solo si todas las comprobaciones pasan)
//             <report>               (informe Markdown; se escribe siempre)
// Mismo JSON byte a byte en cada ejecución (UTF-8 sin BOM, LF, 2 espacios, orden de claves fijo; .claude/rules/data-json.md).
// La consola es ASCII (.claude/rules/ci.md); el informe es UTF-8.
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AF.Core.Rules;

namespace AmazonFill.Tools.LevelGenerator.Handmade;

internal static class HandmadeRunner
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// 0 = todo PASS; 1 = alguna comprobación falla (o --check y el pack difiere); 2 = especificación ilegible.
    /// El par del pack se calcula en <paramref name="mode"/>; el pack no declara el modo (el subconjunto v0 de
    /// af-level/1 no tiene ese campo y añadirlo es cambio de contrato, pregunta P7 de docs/dev/decisiones.md): asume el
    /// modo por defecto del cliente, run (D-005). El informe declara el modo y el par del otro modo como referencia.
    /// </summary>
    public static int Run(string specPath, string outDir, string reportPath, bool checkOnly, PourMode mode)
    {
        List<LevelSpec> specs;
        try
        {
            specs = LoadSpec(specPath);
        }
        catch (SpecException e)
        {
            Console.Error.WriteLine(Ascii($"level-generator: {e.Message}"));
            return 2;
        }

        string modeKey = PourModes.ToKey(mode);
        string referenceKey = PourModes.ToKey(mode == PourMode.Single ? PourMode.Run : PourMode.Single);
        Console.WriteLine($"modo de vertido: {modeKey}{(mode == PourModes.Default ? " (por defecto)" : " (NO es el modo por defecto del cliente; solo pruebas)")}");
        var levels = new List<HandmadeLevel>();
        foreach (LevelSpec spec in specs)
        {
            HandmadeLevel level;
            try
            {
                level = new HandmadeLevel(spec, mode);
            }
            catch (SpecException e)
            {
                Console.Error.WriteLine(Ascii($"level-generator: {e.Message}"));
                return 2;
            }

            level.Run();
            levels.Add(level);
            Console.WriteLine(Ascii(string.Create(CultureInfo.InvariantCulture,
                $"[{(level.Ok ? "PASS" : "FAIL")}] nivel {level.Number,2}: {level.Board.Count} cuias = {level.Colors.Length} colores + {level.Empties} vacios, par {HandmadeLevel.Fmt(level.Par)} ({referenceKey}: {HandmadeLevel.Fmt(level.ReferencePar)}), {level.Graph?.StateCount ?? 0} estados, estancadas en {(level.StuckPath is null ? "-" : level.StuckPath.Count.ToString(CultureInfo.InvariantCulture))}")));
            foreach (Check c in level.Checks.Where(c => !c.Ok))
            {
                Console.WriteLine(Ascii($"         FAIL {c.Name}: {c.Detail}"));
            }
        }

        List<Check> packChecks = PackChecks(levels);
        foreach (Check c in packChecks.Where(c => !c.Ok))
        {
            Console.WriteLine(Ascii($"[FAIL] pack: {c.Name}: {c.Detail}"));
        }

        bool allOk = levels.All(l => l.Ok) && packChecks.All(c => c.Ok);
        string packText = BuildPack(levels);
        byte[] packBytes = Utf8NoBom.GetBytes(packText);
        string sha = Convert.ToHexStringLower(SHA256.HashData(packBytes));
        string packPath = Path.Combine(outDir, HandmadeCanon.PackId + ".json");

        bool differs = false;
        if (checkOnly)
        {
            byte[]? onDisk = File.Exists(packPath) ? File.ReadAllBytes(packPath) : null;
            differs = onDisk is null || !onDisk.AsSpan().SequenceEqual(packBytes);
            Console.WriteLine($"--check: el pack en disco {(differs ? "NO coincide" : "coincide")} con el generado");
        }
        else if (allOk)
        {
            Directory.CreateDirectory(outDir);
            File.WriteAllBytes(packPath, packBytes);
        }

        string note = !allOk ? " (pack no escrito: hay comprobaciones en FAIL)"
            : differs ? " (--check: el pack en disco NO coincide con este)"
            : "";
        string? reportDir = Path.GetDirectoryName(Path.GetFullPath(reportPath));
        if (reportDir is not null)
        {
            Directory.CreateDirectory(reportDir);
        }

        File.WriteAllBytes(reportPath, Utf8NoBom.GetBytes(HandmadeReport.Build(levels, packChecks, sha, note, allOk, mode)));

        int total = levels.Sum(l => l.Checks.Count) + packChecks.Count;
        int pass = levels.Sum(l => l.Checks.Count(c => c.Ok)) + packChecks.Count(c => c.Ok);
        Console.WriteLine($"{pass}/{total} comprobaciones PASS; pack sha256 {sha}{(allOk && !checkOnly ? " (escrito)" : "")}");
        Console.WriteLine(Ascii($"informe: {reportPath.Replace('\\', '/')}"));
        return allOk && !differs ? 0 : 1;
    }

    private static List<LevelSpec> LoadSpec(string path)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            throw new SpecException($"no se puede leer {path}: {e.Message}");
        }

        using (doc)
        {
            JsonElement root = doc.RootElement;
            CheckHeader(root, "pack_id", HandmadeCanon.PackId);
            CheckHeader(root, "kind", HandmadeCanon.Kind);
            CheckHeader(root, "world", HandmadeCanon.World);
            CheckHeader(root, "world_id", HandmadeCanon.WorldId);
            CheckHeader(root, "lap", HandmadeCanon.Lap);
            CheckHeader(root, "water_base", HandmadeCanon.WaterBase);
            if (!root.TryGetProperty("levels", out JsonElement levels) || levels.ValueKind != JsonValueKind.Array)
            {
                throw new SpecException("falta la lista 'levels' en la especificación");
            }

            var result = new List<LevelSpec>();
            foreach (JsonElement lv in levels.EnumerateArray())
            {
                int number = lv.TryGetProperty("level", out JsonElement n) && n.TryGetInt32(out int v) ? v : -1;
                string[] palette = Strings(lv, "palette", number);
                if (!lv.TryGetProperty("cuias", out JsonElement cuias) || cuias.ValueKind != JsonValueKind.Array)
                {
                    throw new SpecException($"nivel {number}: falta 'cuias'");
                }

                var doses = new List<string[]>();
                foreach (JsonElement cuia in cuias.EnumerateArray())
                {
                    if (cuia.ValueKind != JsonValueKind.Array)
                    {
                        throw new SpecException($"nivel {number}: cada cuia debe ser una lista de tintes");
                    }

                    doses.Add(cuia.EnumerateArray().Select(t => t.GetString() ?? "").ToArray());
                }

                foreach (string id in palette.Concat(doses.SelectMany(d => d)))
                {
                    if (HandmadeCanon.FindTint(id) is null)
                    {
                        throw new SpecException($"nivel {number}: tinte desconocido '{id}' (glosario §9)");
                    }
                }

                string? note = lv.TryGetProperty("note", out JsonElement noteEl) ? noteEl.GetString() : null;
                result.Add(new LevelSpec(number, palette, doses.ToArray(), note));
            }

            if (!result.Select(l => l.Level).SequenceEqual(Enumerable.Range(1, 10)))
            {
                throw new SpecException("la especificación debe traer los niveles 1-10 en orden");
            }

            return result;
        }
    }

    private static void CheckHeader(JsonElement root, string key, string expected)
    {
        if (!root.TryGetProperty(key, out JsonElement el) || el.ValueKind != JsonValueKind.String || el.GetString() != expected)
        {
            throw new SpecException($"cabecera: '{key}' debe ser \"{expected}\" (prototipo-v0 b)");
        }
    }

    private static void CheckHeader(JsonElement root, string key, int expected)
    {
        if (!root.TryGetProperty(key, out JsonElement el) || !el.TryGetInt32(out int v) || v != expected)
        {
            throw new SpecException(string.Create(CultureInfo.InvariantCulture, $"cabecera: '{key}' debe ser {expected} (prototipo-v0 b)"));
        }
    }

    private static string[] Strings(JsonElement lv, string key, int level)
    {
        if (!lv.TryGetProperty(key, out JsonElement arr) || arr.ValueKind != JsonValueKind.Array)
        {
            throw new SpecException($"nivel {level}: falta '{key}'");
        }

        return arr.EnumerateArray().Select(t => t.GetString() ?? "").ToArray();
    }

    /// <summary>Comprobaciones de 08 §4.1 que tienen sentido en el subconjunto v0 (sin pack_schema, gen, seed ni bot).</summary>
    private static List<Check> PackChecks(List<HandmadeLevel> levels)
    {
        var checks = new List<Check>();
        foreach (HandmadeLevel l in levels)
        {
            var errors = new List<string>();
            string id = HandmadeCanon.LevelId(l.Number);
            if (!System.Text.RegularExpressions.Regex.IsMatch(id, "^m[0-9]{2}_v[0-9]+_n[0-9]{4}$"))
            {
                errors.Add($"id {id} no cumple m{{mm}}_v{{v}}_n{{nnnn}} (glosario §11)");
            }

            if (l.Board.Count > 14)
            {
                errors.Add("más de 14 cuias generadas (02 R1)");
            }

            var paletteEngines = l.Spec.Palette.Select(t => HandmadeCanon.FindTint(t)!.Engine).Order().ToArray();
            if (!paletteEngines.SequenceEqual(l.Colors) || l.Colors.Any(c => l.Board.UnitsOf(c) != HandmadeCanon.UnitsPerColor))
            {
                errors.Add("tintes de cuias distintos de palette o sin units_per_color dosis (08 §4.1)");
            }

            if (l.Par is null || l.Par < 1)
            {
                errors.Add("difficulty.par debe ser ≥ 1");
            }

            checks.Add(new Check($"nivel {l.Number}: campos del subconjunto de af-level/1 y comprobaciones de 08 §4.1",
                errors.Count == 0, errors.Count == 0 ? "ok (sin solution, safe_path ni qa en el pack)" : string.Join("; ", errors)));
        }

        return checks;
    }

    private static string BuildPack(List<HandmadeLevel> levels)
    {
        var levelNodes = new List<object?>();
        foreach (HandmadeLevel l in levels)
        {
            levelNodes.Add(new JsonObj
            {
                { "id", HandmadeCanon.LevelId(l.Number) },
                { "level", l.Number },
                { "world", HandmadeCanon.World },
                { "lap", HandmadeCanon.Lap },
                { "water_base", HandmadeCanon.WaterBase },
                { "palette", l.Spec.Palette.Cast<object?>().ToList() },
                { "units_per_color", HandmadeCanon.UnitsPerColor },
                { "caps", Enumerable.Repeat<object?>(HandmadeCanon.Capacity, l.Board.Count).ToList() },
                { "cuias", l.Doses.Select(d => (object?)d.Cast<object?>().ToList()).ToList() },
                { "attrs", Enumerable.Repeat<object?>(null, l.Board.Count).ToList() },
                { "murky", false },
                { "hidden", new List<object?>() },
                { "difficulty", new JsonObj { { "par", l.Par ?? 0 } } },
                { "relief", new List<object?>() },
            });
        }

        var pack = new JsonObj
        {
            { "subset_of", HandmadeCanon.SubsetOf },
            { "pack_id", HandmadeCanon.PackId },
            { "kind", HandmadeCanon.Kind },
            { "world", HandmadeCanon.World },
            { "world_id", HandmadeCanon.WorldId },
            { "lap", HandmadeCanon.Lap },
            { "water_base", HandmadeCanon.WaterBase },
            { "levels", levelNodes },
        };
        return StableJson.Write(pack, 0) + "\n";
    }

    /// <summary>La consola de Windows corrompe los acentos al capturar la salida: se imprime ASCII.</summary>
    public static string Ascii(string text)
    {
        string t = text.Replace("→", "->").Replace("×", "x").Replace("≤", "<=").Replace("≥", ">=").Replace("Δ", "d").Replace("§", "sec. ");
        string decomposed = t.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
        {
            if (c < 128)
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}

/// <summary>Objeto JSON con orden de claves estable.</summary>
internal sealed class JsonObj : List<KeyValuePair<string, object?>>
{
    public void Add(string key, object? value) => Add(new KeyValuePair<string, object?>(key, value));
}

/// <summary>
/// JSON estable del repositorio (el mismo formato que los packs ya versionados): 2 espacios; las listas de escalares
/// o de listas de escalares van en una sola línea con ", ".
/// </summary>
internal static class StableJson
{
    public static string Write(object? value, int indent)
    {
        string pad = new(' ', 2 * indent);
        switch (value)
        {
            case JsonObj obj:
                if (obj.Count == 0)
                {
                    return "{}";
                }

                return "{\n" + string.Join(",\n", obj.Select(kv => $"{pad}  {Str(kv.Key)}: {Write(kv.Value, indent + 1)}")) + "\n" + pad + "}";
            case List<object?> list:
                bool flat = list.All(x => x is not JsonObj and not List<object?>);
                bool nested = list.All(x => x is List<object?> inner && inner.All(y => y is not JsonObj and not List<object?>));
                if (flat || nested)
                {
                    return "[" + string.Join(", ", list.Select(x => Write(x, 0))) + "]";
                }

                return "[\n" + string.Join(",\n", list.Select(x => $"{pad}  {Write(x, indent + 1)}")) + "\n" + pad + "]";
            case null:
                return "null";
            case bool b:
                return b ? "true" : "false";
            case int i:
                return i.ToString(CultureInfo.InvariantCulture);
            case string s:
                return Str(s);
            default:
                throw new ArgumentException("Tipo JSON no admitido: " + value.GetType().Name);
        }
    }

    private static string Str(string s)
    {
        var sb = new StringBuilder(s.Length + 2);
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                    {
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(c);
                    }

                    break;
            }
        }

        return sb.Append('"').ToString();
    }
}
