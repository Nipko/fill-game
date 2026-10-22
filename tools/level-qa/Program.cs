// level-qa: control de calidad de paquetes de niveles de Amazon Fill (GDD 08 §2.4 y §4.1 «Validación en tools/level-qa»;
// 03 §17). Lee los paquetes con System.Text.Json, construye cada tablero con BoardBuilder y lo recalcula con AF.Core y
// AF.Solver (par, bot ×40, trampas tempranas, ruta segura de Aguas turbias, filtros de la tinaja, variantes de alivio);
// comprueba el ciclo de D-009 con la especificación de la campaña (tools/level-generator/specs/campaign_v1.json u otra con
// --spec, mismo lector que el generador). Los rangos de niveles (el que se regenera, el tope de mecánicas por nivel, los hitos de
// K = 13) salen de la especificación y de la cabecera del pack, nunca de un nivel escrito en el código (P6 de la tanda 4); las
// variantes de la 1.0 tienen sus controles en Checks/VariantChecks.cs.
// Es la compuerta completa en un solo comando (Checks/GateChecks.cs): también valida el esquema JSON (QA-SCHEMA, con
// python tools/validate_schema.py --pack), regenera la campaña para comprobar que la semilla reproduce cada pack (QA-REPRO,
// 03 §17 #10) y mide la banda de cada nivel en el pool regenerado (QA-BAND-Q, 03 §7). --no-reproduce salta las dos últimas.
// Todo lo que se imprime es ASCII (.claude/rules/ci.md); el informe Markdown es UTF-8.
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Solver.Collections;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;
using AmazonFill.Tools.LevelQa;
using AmazonFill.Tools.LevelQa.Checks;

const string DefaultSpec = "tools/level-generator/specs/campaign_v1.json";
const string Usage = """
    level-qa: control de calidad de paquetes de niveles de Amazon Fill (GDD 08 sec. 2.4 y 4.1; 03 sec. 17)

    Uso:
      dotnet run --project tools/level-qa -c Release -- <paquete.json> [<paquete.json> ...] [--report <informe.md>] [--spec <campaign.json>] [--levels a-b] [--variant a|b] [--no-reproduce]
      dotnet run --project tools/level-qa -c Release -- --daily <daily_pool_v1.json> [--spec <daily_v1.json>] [--partial] [--no-reproduce]
      dotnet run --project tools/level-qa -c Release -- --reserve <reserva_NN_v1.json> ... --campaign <mundo_NN_v1.json> ... --spec <campaign.json> [--variant a|b] [--report <informe.md>] [--no-reproduce]
      dotnet run --project tools/level-qa -- --self-test [key2|reglas|variante|daily]
      dotnet run --project tools/level-qa -- --help

    Recalcula cada nivel con AF.Core y AF.Solver y comprueba: estructura, U_c, selladas, Aguas estancadas al inicio, par
    (A* w = 1, run) y par >= 2K, paleta y debuts de tinte, K, pares de riesgo, bot x40 con la semilla del nivel y su
    etiqueta, trampas tempranas por etiqueta (Q16 B), tamano, el ciclo de 20 de D-009 (bandas, etiquetas, escalera,
    remanso, puros, ventanas, estrenos, hitos de 14 cuias), alivio, anti-repeticion, mecanicas (tope por nivel de la
    especificacion), las variantes de la 1.0 (Liana de cuenta, Hoja atada, Brote de dos hojas y Flor), Aguas turbias y la
    tinaja (forma, mascara, topes por tipo de nivel, ruta segura, paseos y coste de Morpho, tambien en sus variantes de
    alivio). La especificacion por defecto es tools/level-generator/specs/campaign_v1.json (ejecutar desde la raiz del
    repositorio).

    Es la compuerta completa de 08 sec. 4.1 en un solo comando:
      QA-SCHEMA  cada pack contra schemas/af-level-1.schema.json (python tools/validate_schema.py --pack; exige Python 3
                 con jsonschema; sin ellos, falla);
      QA-REPRO   regenera la campana con la especificacion (--levels, por defecto el rango completo de la especificacion,
                 first_level-last_level: D depende del pool de cada rango) y cada pack tiene que salir byte a byte
                 (03 sec. 17 #10; tarda lo que el generador: varios minutos en Release);
      QA-BAND-Q  banda medida (cuantil de D en el pool regenerado) acorde al rol del ciclo (03 sec. 7).
    --no-reproduce salta QA-REPRO y QA-BAND-Q (fixtures negativos, pruebas rapidas); nunca en el CI.
    --variant a|b (P7b): la variante de una especificacion con respaldos, la misma del --variant del generador (por
    defecto la publicada, published_variant; b = la salida B de D-019, sin la Tinaja, de campaign_v2.json): el
    calendario de las comprobaciones y la regeneracion de QA-REPRO y QA-BAND-Q salen de ella.
    --self-test (sin nombre): key2, daily y reserve, con el codigo mayor.
    --self-test key2: genera el estreno de cada variante de la 1.0 de tools/level-generator/specs/sample_variants.json,
    lo escribe a un pack temporal y comprueba que este lector lo lee (tambien la key2 de la Flor y el relief) con el mismo
    tablero y el mismo hash que el generador; la Flor sin key2 no se construye. Sale con 0, 1 (algun FAIL) o 2.
    --self-test reglas: prueba en positivo y en negativo el tope de mecanicas por nivel (QA-MECH-COUNT: retos desde el 301 y
    hitos desde el 320 con sample_variants.json; 2 siempre con campaign_v1.json), QA-HITO-14 (el vacio del hito de K = 13
    repartido en cuias a medias, como el 580 y el 600 de antes de D-027, y las 14 cuias con K = 12 y la cueva de la Pirana),
    las casas de la Flor de QA-VAR-FLOR (D-024) y la Tinaja de D-027: QA-TINAJA-CUIAS (12 cuias con la pareja en el nivel y
    13 en sus variantes de alivio) y la excepcion de QA-PAR para sus tableros de estreno (BaseChecks.TinajaDebutParExempt).
    Sale con 0, 1 (algun FAIL) o 2.
    --self-test variante (P7b): --variant por la linea de comandos, en un proceso hijo con --no-reproduce, sobre dos niveles
    del pack de muestra de la tinaja: con a (y sin --variant) el 323 con la tinaja pasa QA-MECH-SPEC y QA-MECH-CELL y una
    comodidad pura en el 327 da QA-MECH-CELL; con b, al reves; --variant c y --variant b con campaign_v1 salen con 2.
    Sale con 0, 1 (algun FAIL) o 2.
    --daily (Reto del rio, 03 sec. 16): QA del pool (QA-DAILY-*) con la especificacion tools/level-generator/specs/
    daily_v1.json (o --spec), el mismo lector que el generador: esquema, slot, id y semilla, 729 dias por banda, K, vacios y
    capacidad, paleta y tope de pares por banda, sin mecanicas ni alivio, A*, par >= 2K, ventana del bot, <= 14 cuias,
    < 2 MB y QA-DAILY-REPRO (regenera en memoria los dias del pack con el codigo del generador y compara cada tablero
    entero); la anti-repeticion y las trampas tempranas, solo aviso (R-Q10). --partial: una muestra (los dias que faltan no
    fallan; avisa de que no es el pool completo). --no-reproduce salta QA-DAILY-REPRO; nunca en el CI.
    --reserve (la reserva del 20 %; D-033, anadido "la reserva del 20 %", Q-B11-7 a; 03 sec. 17): QA de los packs de
    reserva (kind reserve, pack_schema 4) con sus packs de campana (--campaign, emparejados por mundo) y la especificacion
    (--spec, la del generador; --variant, la misma): las comprobaciones base sobre cada tablero (par, bot, etiqueta,
    contrato 6, trampas tempranas, paleta, K, pares de riesgo), el alivio, el tamano, QA-SCHEMA y QA-RES-* (cabecera,
    slot, replaces, id y semilla, sin mecanicas ni Barro, ventana, etiqueta y band del slot, tope del 20 %, sin D y
    QA-RES-REPRO, que regenera cada pack en memoria con el codigo del generador y lo compara byte a byte).
    --no-reproduce salta QA-RES-REPRO; nunca en el CI.
    --self-test reserve: el pack de reserva del mundo 3 pasa sin fallos y cada copia estropeada (pack_schema, kind, pack_id,
    replaces, id, seed, la Hoja, el Barro, intro_mechanic, D, la ventana y el tope del 20 %) falla con su QA-RES-*. Sale con
    0, 1 (algun FAIL) o 2.
    --self-test daily: el fixture valido y los malos de tools/level-qa/fixtures/daily/ (cada uno falla con su id), los
    avisos de R-Q10 y el tope de tamano. Sale con 0, 1 (algun FAIL) o 2.
    --self-test cvd (Q-B9-13 bis): QA-CVD-ADJ falla con el fixture del 7 sobre el 10 en el tablero base, es aviso si el
    contacto solo esta en una variante de relief, y GateChecks.CvdUnavoidable solo lo baja a aviso con el pack reproducido
    y Tints.CvdAdjacent > 0. Sale con 0, 1 (algun FAIL) o 2. El --self-test sin nombre suma key2, daily, reserve y cvd.

    Codigos de salida: 0 = todo cumple (los avisos no fallan) o ayuda; 1 = algun nivel no cumple, el esquema falla, un
    pack no se reproduce o falta Python; 2 = uso incorrecto, sin paquetes o archivos ilegibles.
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

// Reto del río (Spark R4): --daily y --self-test daily, antes del --self-test genérico, que rechazaría "daily".
if (args[0] == "--daily" || (args.Length == 2 && args[0] == "--self-test" && args[1] == "daily"))
{
    return AmazonFill.Tools.LevelQa.Daily.DailyQaCommand.Run(args);
}

// --self-test key2 (P5): el PackReader de level-qa lee la key2 de la Flor (y las demás variantes) del pack escrito por el
// generador con el mismo tablero y el mismo hash (VariantsSelfTest.RunPackReadBack, enlazado del generador).
// --self-test reglas (P6): QA-MECH-COUNT, QA-HITO-14 y las casas de QA-VAR-FLOR en positivo (Checks/RulesSelfTest.cs).
// --self-test variante (P7b): la opción --variant con las dos variantes de campaign_v2 (Checks/VariantOptionSelfTest.cs).
// La reserva del 20 % (D-033, Q-B11-7 a): --reserve, con su propio lector de opciones (Reserve/ReserveQaCommand.cs).
if (args[0] == "--reserve")
{
    return AmazonFill.Tools.LevelQa.Reserve.ReserveQaCommand.Run(args, UsageError);
}

if (args[0] == "--self-test")
{
    if (args.Length > 2 || (args.Length == 2 && args[1] != "key2" && args[1] != "reglas" && args[1] != "variante" && args[1] != "reserve" && args[1] != "cvd"))
    {
        return UsageError("--self-test solo admite: key2, reglas, variante, reserve, cvd o daily.");
    }

    if (args.Length == 2 && args[1] == "reserve")
    {
        return AmazonFill.Tools.LevelQa.Reserve.ReserveQaCommand.SelfTest();
    }

    if (args.Length == 2 && args[1] == "variante")
    {
        return VariantOptionSelfTest.Run();
    }

    if (args.Length == 2 && args[1] == "reglas")
    {
        return RulesSelfTest.Run();
    }

    if (args.Length == 2 && args[1] == "cvd")
    {
        return CvdQaSelfTest.Run();
    }

    int key2 = VariantsSelfTest.RunPackReadBack("level-qa PackReader", path =>
    {
        QaLevel level = PackReader.Read(path).Levels[0];
        return (level.Board, [.. level.Relief.Select(r => r.Board)]);
    });

    // E17 (Spark): el --self-test sin nombre suma el del Reto del río (--self-test daily) y el de la reserva (--self-test reserve).
    // Q-B9-13 bis: también el de QA-CVD-ADJ (--self-test cvd).
    return args.Length == 2 ? key2 : Math.Max(Math.Max(Math.Max(key2, AmazonFill.Tools.LevelQa.Daily.DailyQaCommand.Run(["--self-test", "daily"])),
        AmazonFill.Tools.LevelQa.Reserve.ReserveQaCommand.SelfTest()), CvdQaSelfTest.Run());
}

var packages = new List<string>();
string? report = null;
string? specPath = null;
string? levelsArg = null;
string? variantArg = null;
bool reproduce = true;
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
    else if (arg == "--levels" && i + 1 < args.Length && levelsArg is null)
    {
        levelsArg = args[++i];
    }
    else if (arg == "--variant" && i + 1 < args.Length && variantArg is null)
    {
        variantArg = args[++i];
    }
    else if (arg == "--no-reproduce" && reproduce)
    {
        reproduce = false;
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
    spec = CampaignSpec.Load(specPath ?? DefaultSpec, variantArg);
}
catch (CampaignSpecException e)
{
    Console.Error.WriteLine(AsciiText.Of($"level-qa: {e.Message}"));
    return 2;
}

int genFirst = spec.FirstLevel;
int genLast = spec.LastLevel;
if (levelsArg is not null && !TryParseLevels(levelsArg, spec, out genFirst, out genLast))
{
    return UsageError($"--levels debe ser a-b dentro de {spec.FirstLevel}-{spec.LastLevel}: {levelsArg}");
}

if (levelsArg is not null && !reproduce)
{
    return UsageError("--levels solo vale con la reproduccion (sin --no-reproduce).");
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
    issues.AddRange(TinajaChecks.Run(e));
    issues.AddRange(VariantChecks.Run(e));
    issues.AddRange(ReliefChecks.Run(e));
}

issues.AddRange(CycleChecks.Run(ctx));
issues.AddRange(RepetitionChecks.Run(ctx));
issues.AddRange(MechanicChecks.RunCampaign(ctx));

// Compuerta completa (08 sec. 4.1): esquema, reproducibilidad y banda medida.
issues.AddRange(GateChecks.Schema(packs));
if (reproduce)
{
    (List<QaIssue> reproIssues, CampaignResult? regenerated, HashSet<string> reproduced) = GateChecks.Reproduce(spec, packs, genFirst, genLast);
    issues.AddRange(reproIssues);
    if (regenerated is not null)
    {
        issues.AddRange(GateChecks.Bands(ctx, regenerated, reproduced));
        GateChecks.CvdUnavoidable(issues, ctx, regenerated, reproduced);
    }
}
else
{
    issues.Add(new QaIssue("QA-REPRO", "-", 0, "--no-reproduce: no se comprueban la reproducibilidad (03 sec. 17 #10) ni la banda medida (QA-BAND-Q)", Warning: true));
}

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
    .Concat(MechanicChecks.Ids).Concat(MurkyChecks.Ids).Concat(TinajaChecks.Ids).Concat(VariantChecks.Ids).Concat(GateChecks.Ids).ToList();
Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
    $"{evals.Count} niveles, {allIds.Count} comprobaciones: {failures} fallo(s), {issues.Count(i => i.Warning)} aviso(s)"));
if (report is not null)
{
    var sb = new StringBuilder();
    sb.Append("# Informe de `level-qa`\n\n");
    sb.Append("Generado por `dotnet run --project tools/level-qa -c Release -- ")
      .Append(string.Join(" ", packages.Select(p => p.Replace('\\', '/'))))
      .Append(specPath is null ? "" : " --spec " + specPath.Replace('\\', '/'))
      .Append(levelsArg is null ? "" : " --levels " + levelsArg)
      .Append(variantArg is null ? "" : " --variant " + variantArg)
      .Append(reproduce ? "" : " --no-reproduce")
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

static bool TryParseLevels(string text, CampaignSpec spec, out int first, out int last)
{
    first = last = 0;
    string[] parts = text.Split('-');
    return parts.Length == 2
        && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out first)
        && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out last)
        && first >= spec.FirstLevel && last <= spec.LastLevel && first <= last;
}

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
