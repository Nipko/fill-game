// Decisiones del dueño sobre la curación de retos, hitos y estrenos (09 §0.1, tabla de roles, fila de level-pipeline-dev: «El
// dueño aprueba la curación de retos e hitos de cada paquete»; 09 §0.3, ritmo de trabajo: «propuesta de curación de los 30 retos
// e hitos que aprueba el dueño»). Viven en un archivo versionado que no se genera (formato [P] af-curation-decisions/1, por ejemplo
// specs/curation_v2.json para campaign_v2.json) y CurationReport las copia en la última columna del informe: así la decisión
// sobrevive a cada regeneración de tools/out/curation_wNN.md, que no se edita a mano (.claude/rules/data-json.md).
//
//   { "format": "af-curation-decisions/1", "spec": "campaign_v2.json",
//     "decisions": [ { "level": 246, "cell": "Ma+Re (D)", "decision": "aceptar", "date": "2026-10-16", "note": "..." } ] }
//
// "cell" es el literal de la celda del calendario que vio el dueño: si la celda cambia (otra variante o «cambiar la celda»),
// la decisión deja de valer y el informe vuelve a «pendiente» con un aviso. «sustituir» y «cambiar la celda» llevan "note"
// (el motivo). Un archivo cubre toda la especificación: un nivel fuera de ella (su first_level-last_level) es un error, y las
// decisiones de la especificación fuera de los --levels generados se ignoran. Lo prueba --self-test curation (CurationSelfTest,
// con los fixtures specs/tests/curation_*.json).
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal sealed record CurationDecision(int Level, string Cell, string Decision, string Date, string? Note);

internal sealed class CurationDecisionsException(string message) : Exception(message);

internal sealed class CurationDecisions
{
    public const string Format = "af-curation-decisions/1";
    public const string Accept = "aceptar";
    public const string Replace = "sustituir";
    public const string ChangeCell = "cambiar la celda";

    private static readonly string[] Allowed = [Accept, Replace, ChangeCell];
    private static readonly string[] EntryKeys = ["level", "cell", "decision", "date", "note"];

    /// <summary>Ruta tal como se pasó (para la línea de comando del informe).</summary>
    public required string Path { get; init; }

    public required IReadOnlyDictionary<int, CurationDecision> ByLevel { get; init; }

    /// <summary>
    /// Lee y valida el archivo; el nombre de "spec" debe coincidir con el de la especificación que se genera y cada "level" debe
    /// caer dentro de ella (<paramref name="firstLevel"/>-<paramref name="lastLevel"/>: su first_level-last_level).
    /// </summary>
    public static CurationDecisions Load(string path, string specPath, int firstLevel, int lastLevel)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8));
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            throw new CurationDecisionsException($"no se puede leer {path}: {e.Message}");
        }

        if (node is not JsonObject root)
        {
            throw new CurationDecisionsException($"{path}: debe ser un objeto JSON");
        }

        foreach (KeyValuePair<string, JsonNode?> kv in root)
        {
            if (kv.Key is not ("format" or "spec" or "decisions"))
            {
                throw new CurationDecisionsException($"{path}: clave desconocida '{kv.Key}'");
            }
        }

        if (Text(root["format"]) != Format)
        {
            throw new CurationDecisionsException($"{path}: \"format\" debe ser \"{Format}\"");
        }

        string? spec = Text(root["spec"]);
        if (spec is null || System.IO.Path.GetFileName(spec) != System.IO.Path.GetFileName(specPath))
        {
            throw new CurationDecisionsException($"{path}: \"spec\" ({spec ?? "ausente"}) no es la especificacion que se genera ({System.IO.Path.GetFileName(specPath)})");
        }

        if (root["decisions"] is not JsonArray list)
        {
            throw new CurationDecisionsException($"{path}: falta la lista \"decisions\"");
        }

        var byLevel = new Dictionary<int, CurationDecision>();
        for (int i = 0; i < list.Count; i++)
        {
            string where = string.Create(CultureInfo.InvariantCulture, $"{path}: decisions[{i}]");
            if (list[i] is not JsonObject e)
            {
                throw new CurationDecisionsException($"{where} debe ser un objeto");
            }

            foreach (KeyValuePair<string, JsonNode?> kv in e)
            {
                if (Array.IndexOf(EntryKeys, kv.Key) < 0)
                {
                    throw new CurationDecisionsException($"{where}: clave desconocida '{kv.Key}'");
                }
            }

            if (e["level"] is not JsonValue lv || !lv.TryGetValue(out int level) || level < 1)
            {
                throw new CurationDecisionsException($"{where}: \"level\" debe ser un entero positivo");
            }

            if (level < firstLevel || level > lastLevel)
            {
                throw new CurationDecisionsException(string.Create(CultureInfo.InvariantCulture,
                    $"{where}: nivel {level} fuera de la especificacion ({firstLevel}-{lastLevel})"));
            }

            string? cell = Text(e["cell"]);
            string? decision = Text(e["decision"]);
            string? date = Text(e["date"]);
            string? note = Text(e["note"]);
            if (string.IsNullOrWhiteSpace(cell))
            {
                throw new CurationDecisionsException($"{where}: falta \"cell\" (el literal de la celda del calendario)");
            }

            if (decision is null || Array.IndexOf(Allowed, decision) < 0)
            {
                throw new CurationDecisionsException($"{where}: \"decision\" debe ser {string.Join(", ", Allowed.Select(a => "\"" + a + "\""))}");
            }

            if (date is null || !DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                throw new CurationDecisionsException($"{where}: \"date\" debe ser AAAA-MM-DD");
            }

            if (decision != Accept && string.IsNullOrWhiteSpace(note))
            {
                throw new CurationDecisionsException($"{where}: \"{decision}\" lleva \"note\" con el motivo");
            }

            if (!byLevel.TryAdd(level, new CurationDecision(level, cell, decision, date, string.IsNullOrWhiteSpace(note) ? null : note)))
            {
                throw new CurationDecisionsException(string.Create(CultureInfo.InvariantCulture, $"{where}: nivel {level} repetido"));
            }
        }

        return new CurationDecisions { Path = path, ByLevel = byLevel };
    }

    private static string? Text(JsonNode? node) =>
        node is JsonValue v && v.TryGetValue(out string? s) ? s : null;
}
