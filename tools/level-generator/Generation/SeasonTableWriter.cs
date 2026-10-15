// --seasons (SE1 de la tanda 4): tabla de temporada del cliente, copiada del calendario versionado (pregunta 32 de
// docs/GDD.md §6.3, decidida el 2026-10-13: (b), D-020; D-009, punto 6; 03 §12: «el cliente la lee de una tabla por
// ciclo, copiada del calendario versionado»). No toca el formato del pack (08 §4.1): la temporada de cada ciclo de 20
// (A = aguas altas, B = aguas bajas) solo cambia la LUT del fondo, sin cartel (Q11 A de D-007).
//
// Formato [P] af-seasons/1 (Assets/_Project/Data/Levels/seasons_v1.json, generado; nunca a mano):
//   {"format": "af-seasons/1", "source": "<especificaciones>", "cycles": [{"from_level": 21, "season": "B"}, …]}
// - source: la ruta de cada especificación, relativa a la raíz del repositorio y con '/', separadas por ", " en el
//   orden de sus ciclos [P] (hoy solo tools/level-generator/specs/campaign_v1.json; P4 y P7 añaden campaign_v2 para
//   201-700).
// - cycles: un ciclo de 20 por fila, en orden creciente de from_level y sin huecos (from_level ≡ 1 mod 20); cada ciclo va
//   de from_level a from_level + 19. Los niveles anteriores al primer ciclo (1-20: tutorial y rampa) no tienen temporada.
// UTF-8 sin BOM, LF, un ciclo por línea y claves en orden fijo: la misma especificación da los mismos bytes.
//
// La herramienta solo copia y comprueba el formato (cada ciclo de la especificación con A o B, ninguna entrada fuera de un
// inicio de ciclo, varias especificaciones contiguas y sin solaparse). Las reglas del calendario (temporadas alternas, el
// ciclo de debut toma la de su mecánica) las comprueba --validate-spec (SEASONS).
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AmazonFill.Tools.LevelGenerator.Campaign;

namespace AmazonFill.Tools.LevelGenerator.Generation;

/// <summary>La tabla de temporada no se puede construir con estas especificaciones (código 1).</summary>
internal sealed class SeasonTableException(string message) : Exception(message);

/// <summary>Temporadas de una especificación: ciclos de 20 desde <see cref="CycleFrom"/> hasta <see cref="LastLevel"/>.</summary>
internal sealed record SeasonSource(string Source, int CycleFrom, int LastLevel, IReadOnlyDictionary<int, string> Seasons);

/// <summary>Una fila de la tabla: el ciclo que empieza en <see cref="FromLevel"/> y su temporada (A o B).</summary>
internal readonly record struct SeasonCycle(int FromLevel, string Season);

internal static class SeasonTableWriter
{
    public const string Format = "af-seasons/1";
    public const int CycleLength = 20;
    public const string CampaignV1 = "tools/level-generator/specs/campaign_v1.json";
    public const string TableV1 = "Assets/_Project/Data/Levels/seasons_v1.json";
    public const string InvalidLetterFixture = "tools/level-generator/specs/tests/seasons_invalid_letter.json";

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Modo --seasons: escribe (o, con --check, compara byte a byte sin escribir) la tabla de las especificaciones.
    /// Códigos: 0 = escrita o idéntica; 1 = desfase o tabla imposible; 2 = especificación ilegible.
    /// </summary>
    public static int Run(IReadOnlyList<string> specPaths, string outPath, bool check)
    {
        byte[] bytes;
        try
        {
            bytes = Build(specPaths);
        }
        catch (CampaignSpecException e)
        {
            Console.Error.WriteLine(AsciiText.Of("level-generator --seasons: " + e.Message));
            return 2;
        }
        catch (SeasonTableException e)
        {
            Console.Error.WriteLine(AsciiText.Of("level-generator --seasons: " + e.Message));
            return 1;
        }

        string shown = outPath.Replace('\\', '/');
        if (check)
        {
            byte[]? onDisk = File.Exists(outPath) ? File.ReadAllBytes(outPath) : null;
            bool same = onDisk is not null && onDisk.AsSpan().SequenceEqual(bytes);
            Console.WriteLine(same
                ? $"--check: {shown} coincide con el generado"
                : $"--check: {shown} NO coincide con el generado (desfase con el calendario: regenera con --seasons sin --check)");
            return same ? 0 : 1;
        }

        string? dir = Path.GetDirectoryName(Path.GetFullPath(outPath));
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllBytes(outPath, bytes);
        Console.WriteLine($"tabla de temporada: {shown} ({bytes.Length} bytes)");
        return 0;
    }

    /// <summary>Los bytes de la tabla de las especificaciones, en ese orden de argumentos (se ordenan por ciclo).</summary>
    public static byte[] Build(IReadOnlyList<string> specPaths)
    {
        var sources = new List<SeasonSource>(specPaths.Count);
        foreach (string path in specPaths)
        {
            sources.Add(SourceOf(CampaignSpec.Load(path), RepoRelative(path)));
        }

        List<SeasonSource> ordered = Ordered(sources);
        return Bytes(string.Join(", ", ordered.Select(s => s.Source)), CyclesInOrder(ordered));
    }

    public static SeasonSource SourceOf(CampaignSpec spec, string source) =>
        new(source, spec.CycleFrom, spec.LastLevel, spec.Seasons);

    /// <summary>
    /// Los ciclos de las fuentes, en orden: cada fuente da un ciclo por cada inicio de 20 entre su CycleFrom y su LastLevel,
    /// con A o B; las fuentes van seguidas, sin hueco ni solape. Lanza <see cref="SeasonTableException"/> si no.
    /// </summary>
    public static List<SeasonCycle> Cycles(IReadOnlyList<SeasonSource> sources) => CyclesInOrder(Ordered(sources));

    /// <summary>Las fuentes por su primer ciclo (el único ordenamiento: lo usan Build y Cycles).</summary>
    private static List<SeasonSource> Ordered(IReadOnlyList<SeasonSource> sources) =>
        sources.OrderBy(s => s.CycleFrom).ToList();

    private static List<SeasonCycle> CyclesInOrder(List<SeasonSource> ordered)
    {
        if (ordered.Count == 0)
        {
            throw new SeasonTableException("ninguna especificacion");
        }

        var cycles = new List<SeasonCycle>();
        foreach (SeasonSource src in ordered)
        {
            if (src.CycleFrom < 1 || (src.CycleFrom - 1) % CycleLength != 0)
            {
                throw new SeasonTableException(
                    $"{src.Source}: el ciclo empieza en {src.CycleFrom}, que no es el primer nivel de un ciclo de 20 (1, 21, 41...)");
            }

            if (src.LastLevel < src.CycleFrom)
            {
                throw new SeasonTableException($"{src.Source}: last_level {src.LastLevel} antes del primer ciclo ({src.CycleFrom})");
            }

            if (cycles.Count > 0)
            {
                int expected = cycles[^1].FromLevel + CycleLength;
                if (src.CycleFrom != expected)
                {
                    string what = src.CycleFrom < expected ? "se solapa con" : "deja un hueco tras";
                    throw new SeasonTableException(
                        $"{src.Source}: su primer ciclo ({src.CycleFrom}) {what} el ciclo {cycles[^1].FromLevel} de la anterior (esperado: {expected})");
                }
            }

            foreach (int key in src.Seasons.Keys.OrderBy(k => k))
            {
                if (key < src.CycleFrom || key > src.LastLevel || (key - src.CycleFrom) % CycleLength != 0)
                {
                    throw new SeasonTableException(
                        $"{src.Source}: la temporada de \"{key}\" no cae en el inicio de un ciclo entre {src.CycleFrom} y {src.LastLevel}");
                }
            }

            for (int start = src.CycleFrom; start <= src.LastLevel; start += CycleLength)
            {
                if (!src.Seasons.TryGetValue(start, out string? season) || season is not ("A" or "B"))
                {
                    throw new SeasonTableException(
                        $"{src.Source}: el ciclo {start}-{start + CycleLength - 1} no tiene temporada A o B ({season ?? "falta"})");
                }

                cycles.Add(new SeasonCycle(start, season));
            }
        }

        return cycles;
    }

    /// <summary>JSON de la tabla: UTF-8 sin BOM, LF, un ciclo por línea y claves en orden fijo.</summary>
    public static byte[] Bytes(string source, IReadOnlyList<SeasonCycle> cycles)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"format\": ").Append(JsonString(Format)).Append(",\n");
        sb.Append("  \"source\": ").Append(JsonString(source)).Append(",\n");
        sb.Append("  \"cycles\": [\n");
        for (int i = 0; i < cycles.Count; i++)
        {
            sb.Append("    {\"from_level\": ")
                .Append(cycles[i].FromLevel.ToString(CultureInfo.InvariantCulture))
                .Append(", \"season\": ")
                .Append(JsonString(cycles[i].Season))
                .Append(i + 1 < cycles.Count ? "},\n" : "}\n");
        }

        sb.Append("  ]\n");
        sb.Append("}\n");
        return Utf8NoBom.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Ruta relativa a la raíz del repositorio (la carpeta que contiene tools/level-generator/level-generator.csproj),
    /// con '/', para que la tabla no dependa de la carpeta desde la que se ejecuta. Fuera del repositorio: el nombre.
    /// </summary>
    public static string RepoRelative(string path)
    {
        string full = Path.GetFullPath(path);
        for (string? dir = Path.GetDirectoryName(full); dir is not null; dir = Path.GetDirectoryName(dir))
        {
            if (File.Exists(Path.Combine(dir, "tools", "level-generator", "level-generator.csproj")))
            {
                return Path.GetRelativePath(dir, full).Replace('\\', '/');
            }
        }

        return Path.GetFileName(full);
    }

    private static string JsonString(string value) => JsonSerializer.Serialize(value);

    /// <summary>
    /// --self-test seasons: la tabla de campaign_v1 (ciclos 21-181 idénticos a su "seasons", leído aparte con JsonNode, y
    /// la temporada de cada nivel 21-200 igual a CampaignSpec.SeasonOf), el formato (claves, orden, UTF-8 sin BOM, LF),
    /// el determinismo, el desfase con la tabla del disco (--check), la unión de varias especificaciones (P4 y P7) y los
    /// rechazos (hueco, solape, letra, ciclo sin temporada, entrada fuera de un inicio de ciclo, inicio desalineado y el
    /// fixture specs/tests/seasons_invalid_letter.json). Desde la raíz del repositorio. Códigos: 0 / 1 / 2.
    /// </summary>
    public static int SelfTest()
    {
        CampaignSpec spec;
        JsonObject raw;
        try
        {
            spec = CampaignSpec.Load(CampaignV1);
            raw = JsonNode.Parse(File.ReadAllText(CampaignV1, Encoding.UTF8))!.AsObject();
        }
        catch (Exception e) when (e is CampaignSpecException or IOException or JsonException)
        {
            Console.Error.WriteLine(AsciiText.Of("level-generator --self-test seasons: " + e.Message + " (ejecuta desde la raiz del repositorio)"));
            return 2;
        }

        int failures = 0;
        void Check(bool ok, string what)
        {
            Console.WriteLine(AsciiText.Of((ok ? "PASS " : "FAIL ") + what));
            failures += ok ? 0 : 1;
        }

        // 1. Ciclos 21-181 idénticos a campaign_v1 (leído aparte, sin CampaignSpec).
        byte[] bytes = Build([CampaignV1]);
        JsonObject table = JsonNode.Parse(bytes)!.AsObject();
        JsonArray cycles = table["cycles"]!.AsArray();
        JsonObject rawSeasons = raw["seasons"]!.AsObject();
        var expected = rawSeasons
            .Select(kv => (Level: int.Parse(kv.Key, NumberStyles.None, CultureInfo.InvariantCulture), Season: kv.Value!.GetValue<string>()))
            .OrderBy(e => e.Level)
            .ToList();
        var actual = cycles
            .Select(c => (Level: c!["from_level"]!.GetValue<int>(), Season: c["season"]!.GetValue<string>()))
            .ToList();
        Check(actual.SequenceEqual(expected), $"seasons_v1: {actual.Count} ciclos identicos a \"seasons\" de campaign_v1 ({Describe(expected)})");
        Check(actual.Count == 9 && actual[0].Level == 21 && actual[^1].Level == 181, "seasons_v1: ciclos 21-181 (9 de 20 niveles, hasta el 200)");

        // 2. La temporada de cada nivel 21-200 por la tabla (el último from_level <= nivel) es la de CampaignSpec.SeasonOf.
        int mismatches = 0;
        for (int level = spec.CycleFrom; level <= spec.LastLevel; level++)
        {
            string? fromTable = null;
            foreach ((int from, string season) in actual)
            {
                if (from <= level && level < from + CycleLength)
                {
                    fromTable = season;
                }
            }

            mismatches += fromTable == spec.SeasonOf(level) ? 0 : 1;
        }

        Check(mismatches == 0, $"seasons_v1: la temporada de los niveles {spec.CycleFrom}-{spec.LastLevel} coincide con CampaignSpec.SeasonOf ({mismatches} distintos)");

        // 3. Formato: claves y orden, source, letras, UTF-8 sin BOM, LF y salto final.
        Check(string.Join(",", table.Select(kv => kv.Key)) == "format,source,cycles", "formato: claves format, source y cycles, en ese orden");
        Check(table["format"]!.GetValue<string>() == Format, $"formato: format = {Format}");
        Check(table["source"]!.GetValue<string>() == CampaignV1, $"formato: source = {CampaignV1}");
        Check(cycles.All(c => string.Join(",", c!.AsObject().Select(kv => kv.Key)) == "from_level,season"), "formato: cada ciclo con from_level y season, en ese orden");
        Check(actual.All(c => c.Season is "A" or "B") && actual.Zip(actual.Skip(1)).All(p => p.Second.Level == p.First.Level + CycleLength),
            "formato: temporadas A o B, ciclos de 20 seguidos y en orden");
        Check(bytes.Length > 3 && !(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) && Array.IndexOf(bytes, (byte)'\r') < 0 && bytes[^1] == (byte)'\n',
            "formato: UTF-8 sin BOM, LF y salto de linea final");

        // 4. Determinismo: misma especificación, mismos bytes, también con otra ruta al mismo archivo.
        Check(Build([CampaignV1]).AsSpan().SequenceEqual(bytes), "determinismo: dos generaciones dan los mismos bytes");
        Check(Build([Path.GetFullPath(CampaignV1)]).AsSpan().SequenceEqual(bytes), "determinismo: la ruta absoluta da los mismos bytes (source relativo a la raiz)");

        // 5. Desfase con la tabla del disco (lo mismo que --check).
        byte[]? onDisk = File.Exists(TableV1) ? File.ReadAllBytes(TableV1) : null;
        Check(onDisk is not null && onDisk.AsSpan().SequenceEqual(bytes), $"--check: {TableV1} coincide con el generado desde campaign_v1");

        // 6. Varias especificaciones (P4 y P7: 201-700 con campaign_v2): se unen en orden y sin huecos.
        var v1 = SourceOf(spec, CampaignV1);
        var tail = new SeasonSource("v2", 201, 260, new Dictionary<int, string> { [201] = "A", [221] = "A", [241] = "B" });
        List<SeasonCycle> joined = Cycles([tail, v1]);
        Check(joined.Count == 12 && joined[9] == new SeasonCycle(201, "A") && joined[^1] == new SeasonCycle(241, "B")
              && joined.Take(9).Select(c => (c.FromLevel, c.Season)).SequenceEqual(expected),
            "union: campaign_v1 + 201-260 da 12 ciclos en orden, con 21-181 sin cambios");
        Check(Encoding.UTF8.GetString(Bytes("a, b", joined)).Contains("\"source\": \"a, b\"", StringComparison.Ordinal), "union: source con las dos especificaciones");

        // 7. Rechazos.
        Rejects(Check, "hueco entre especificaciones", "deja un hueco tras el ciclo 181", [v1, tail with { CycleFrom = 221, Seasons = new Dictionary<int, string> { [221] = "A", [241] = "B" } }]);
        Rejects(Check, "solape entre especificaciones", "(181) se solapa con el ciclo 181", [v1, tail with { CycleFrom = 181, Seasons = new Dictionary<int, string> { [181] = "B", [201] = "A", [221] = "A", [241] = "B" } }]);
        Rejects(Check, "temporada distinta de A o B", "el ciclo 221-240 no tiene temporada A o B (C)", [tail with { Seasons = new Dictionary<int, string> { [201] = "A", [221] = "C", [241] = "B" } }]);
        Rejects(Check, "ciclo sin temporada", "el ciclo 221-240 no tiene temporada A o B (falta)", [tail with { Seasons = new Dictionary<int, string> { [201] = "A", [241] = "B" } }]);
        Rejects(Check, "entrada fuera de un inicio de ciclo", "la temporada de \"210\" no cae en el inicio de un ciclo", [tail with { Seasons = new Dictionary<int, string> { [201] = "A", [210] = "B", [221] = "A", [241] = "B" } }]);
        Rejects(Check, "entrada tras el ultimo nivel", "la temporada de \"261\" no cae en el inicio de un ciclo", [tail with { Seasons = new Dictionary<int, string> { [201] = "A", [221] = "A", [241] = "B", [261] = "A" } }]);
        Rejects(Check, "primer ciclo desalineado", "el ciclo empieza en 202", [tail with { CycleFrom = 202 }]);
        Rejects(Check, "ninguna especificacion", "ninguna especificacion", []);
        try
        {
            Build([InvalidLetterFixture]);
            Check(false, $"rechazo: {InvalidLetterFixture} (temporada C) no se acepta");
        }
        catch (SeasonTableException e)
        {
            // Por la letra C del ciclo 61 y no por otro motivo (por ejemplo, que extends dejara de fusionar seasons).
            Check(e.Message.Contains("el ciclo 61-80 no tiene temporada A o B (C)", StringComparison.Ordinal),
                $"rechazo: {InvalidLetterFixture} (temporada C del ciclo 61) no se acepta ({e.Message})");
        }
        catch (CampaignSpecException e)
        {
            Check(false, $"rechazo: {InvalidLetterFixture} ilegible: {e.Message}");
        }

        Console.WriteLine(failures == 0 ? "self-test seasons: todo PASS" : $"self-test seasons: {failures} FAIL");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>El rechazo cuenta solo si el mensaje dice el motivo esperado (<paramref name="reason"/>), no otro.</summary>
    private static void Rejects(Action<bool, string> check, string what, string reason, IReadOnlyList<SeasonSource> sources)
    {
        try
        {
            Cycles(sources);
            check(false, "rechazo: " + what);
        }
        catch (SeasonTableException e)
        {
            check(e.Message.Contains(reason, StringComparison.Ordinal), $"rechazo: {what} ({e.Message})");
        }
    }

    private static string Describe(IEnumerable<(int Level, string Season)> cycles) =>
        string.Join(" ", cycles.Select(c => c.Level.ToString(CultureInfo.InvariantCulture) + c.Season));
}
