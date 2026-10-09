// level-qa: control de calidad de paquetes de niveles de Amazon Fill (GDD 08 §2.4 y §4.1 «Validación en tools/level-qa»;
// 03 §17). Lee los paquetes con System.Text.Json, construye cada tablero con BoardBuilder y lo recalcula con AF.Core y
// AF.Solver (par, bot ×40, trampas tempranas, ruta segura de Aguas turbias, variantes de alivio); comprueba el ciclo de
// D-009 con la especificación de la campaña (tools/level-generator/specs/campaign_v1.json, mismo lector que el generador).
// El esquema JSON lo valida python tools/validate_schema.py --pack en el mismo paso del CI.
// Todo lo que se imprime es ASCII (.claude/rules/ci.md); el informe Markdown es UTF-8.
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Solver.Collections;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelQa;
using AmazonFill.Tools.LevelQa.Checks;

const string DefaultSpec = "tools/level-generator/specs/campaign_v1.json";
const string Usage = """
    level-qa: control de calidad de paquetes de niveles de Amazon Fill (GDD 08 sec. 2.4 y 4.1; 03 sec. 17)

    Uso:
      dotnet run --project tools/level-qa -c Release -- <paquete.json> [<paquete.json> ...] [--report <informe.md>] [--spec <campaign.json>]
      dotnet run --project tools/level-qa -- --help

    Recalcula cada nivel con AF.Core y AF.Solver y comprueba: estructura, U_c, selladas, Aguas estancadas al inicio, par
    (A* w = 1, run) y par >= 2K, paleta y debuts de tinte, K, pares de riesgo, bot x40 con la semilla del nivel y su
    etiqueta, trampas tempranas por etiqueta (Q16 B), tamano, el ciclo de 20 de D-009 (bandas, etiquetas, escalera,
    remanso, puros, ventanas, estrenos), alivio, anti-repeticion, mecanicas y Aguas turbias. La especificacion por
    defecto es tools/level-generator/specs/campaign_v1.json (ejecutar desde la raiz del repositorio).

    Codigos de salida: 0 = todo cumple (los avisos no fallan) o ayuda; 1 = algun nivel no cumple;
    2 = uso incorrecto, sin paquetes o archivos ilegibles.
    Sin argumentos sale con 2: un glob vacio en el CI nunca deja pasar la compuerta sin revisar nada.
    """;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// Sin argumentos es un error, no una petición de ayuda: fallar cerrado (.claude/rules/ci.md).
if (args.Length == 0)
{
    return UsageError("no se ha indicado ningun paquete.");
}

if (args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine(Usage);
    Console.WriteLine($"Nucleo enlazado: {Describe(typeof(BoardState))}, {Describe(typeof(MinHeap))}");
    return 0;
}

var packages = new List<string>();
string? report = null;
string? specPath = null;
for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];
    if (arg == "--report" && i + 1 < args.Length && report is null)
    {
        report = args[++i];
    }
    else if (arg == "--spec" && i + 1 < args.Length && specPath is null)
    {
        specPath = args[++i];
    }
    else if (arg.StartsWith('-'))
    {
        return UsageError($"opcion no valida, repetida o sin valor: {arg}");
    }
    else
    {
        packages.Add(arg);
    }
}

if (packages.Count == 0)
{
    return UsageError("no se ha indicado ningun paquete.");
}

CampaignSpec spec;
try
{
    spec = CampaignSpec.Load(specPath ?? DefaultSpec);
}
catch (CampaignSpecException e)
{
    Console.Error.WriteLine(AsciiText.Of($"level-qa: {e.Message}"));
    return 2;
}

var packs = new List<QaPack>();
foreach (string path in packages)
{
    try
    {
        packs.Add(PackReader.Read(path));
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException
        or PackFormatException or KeyNotFoundException or InvalidOperationException or FormatException)
    {
        Console.Error.WriteLine(AsciiText.Of($"level-qa: no se puede leer {path}: {e.Message}"));
        return 2;
    }
}

List<LevelEval> evals = Evaluator.Evaluate(spec, packs);
var ctx = new QaContext { Spec = spec, Packs = packs, Levels = evals };
var issues = new List<QaIssue>();
foreach (QaPack pack in packs)
{
    issues.AddRange(BaseChecks.RunPack(pack));
}

foreach (LevelEval e in evals)
{
    issues.AddRange(BaseChecks.Run(ctx, e));
    issues.AddRange(MechanicChecks.Run(ctx, e));
    issues.AddRange(MurkyChecks.Run(e));
    issues.AddRange(ReliefChecks.Run(e));
}

issues.AddRange(CycleChecks.Run(ctx));
issues.AddRange(RepetitionChecks.Run(ctx));
issues.AddRange(MechanicChecks.RunCampaign(ctx));

foreach (int level in evals.GroupBy(e => e.Level.Level).Where(g => g.Count() > 1).Select(g => g.Key))
{
    issues.Add(new QaIssue("QA-STRUCT", "-", level, "nivel repetido en los paquetes"));
}

List<QaIssue> ordered = issues.OrderBy(i => i.Warning).ThenBy(i => i.Level).ThenBy(i => i.Check, StringComparer.Ordinal).ToList();
foreach (QaPack pack in packs)
{
    int fails = issues.Count(i => !i.Warning && i.Pack == pack.Path);
    Console.WriteLine(AsciiText.Of(string.Create(CultureInfo.InvariantCulture,
        $"{pack.Path.Replace('\\', '/')}: pack_schema {pack.PackSchema}, {pack.Levels.Count} niveles, {fails} fallo(s)")));
}

foreach (QaIssue i in ordered)
{
    Console.WriteLine(AsciiText.Of(string.Create(CultureInfo.InvariantCulture,
        $"{(i.Warning ? "AVISO" : "FAIL ")} [{i.Check}] nivel {i.Level}: {i.Message}")));
}

int failures = issues.Count(i => !i.Warning);
var allIds = BaseChecks.Ids.Concat(CycleChecks.Ids).Concat(ReliefChecks.Ids).Concat(RepetitionChecks.Ids)
    .Concat(MechanicChecks.Ids).Concat(MurkyChecks.Ids).ToList();
Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
    $"{evals.Count} niveles, {allIds.Count} comprobaciones: {failures} fallo(s), {issues.Count(i => i.Warning)} aviso(s)"));
if (report is not null)
{
    var sb = new StringBuilder();
    sb.Append("# Informe de `level-qa`\n\n");
    sb.Append("Generado por `dotnet run --project tools/level-qa -c Release -- ")
      .Append(string.Join(" ", packages.Select(p => p.Replace('\\', '/'))))
      .Append(" --report ").Append(report.Replace('\\', '/')).Append("`: no se edita a mano.\n\n");
    sb.Append(string.Create(CultureInfo.InvariantCulture,
        $"- Niveles: {evals.Count}; fallos: **{failures}**; avisos: {issues.Count(i => i.Warning)}.\n\n"));
    sb.Append("| Comprobación | Regla | Fallos |\n|---|---|---|\n");
    foreach ((string id, string rule) in allIds)
    {
        sb.Append(string.Create(CultureInfo.InvariantCulture, $"| `{id}` | {rule} | {issues.Count(i => i.Check == id && !i.Warning)} |\n"));
    }

    sb.Append("\n## Incidencias\n\n");
    if (ordered.Count == 0)
    {
        sb.Append("Ninguna.\n");
    }

    foreach (QaIssue i in ordered)
    {
        sb.Append(string.Create(CultureInfo.InvariantCulture,
            $"- {(i.Warning ? "aviso" : "**fallo**")} `{i.Check}` nivel {i.Level} (`{i.Pack.Replace('\\', '/')}`): {i.Message}\n"));
    }

    string? dir = Path.GetDirectoryName(Path.GetFullPath(report));
    if (dir is not null)
    {
        Directory.CreateDirectory(dir);
    }

    File.WriteAllBytes(report, new UTF8Encoding(false).GetBytes(sb.ToString()));
    Console.WriteLine($"informe: {report.Replace('\\', '/')}");
}

return failures == 0 ? 0 : 1;

static int UsageError(string message)
{
    Console.Error.WriteLine($"level-qa: {message}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(Usage);
    return 2;
}

static string Describe(Type type)
{
    var name = type.Assembly.GetName();
    return $"{name.Name} {name.Version}";
}
