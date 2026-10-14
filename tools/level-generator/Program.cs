// level-generator: generador offline de niveles de Amazon Fill (GDD 08 §2.4; GDD 03).
// --handmade: comprueba el tutorial hecho a mano (02 §13; 03 §4.4-§5) con AF.Core y AF.Solver y emite el pack v0 y su
// informe (docs/dev/prototipo-v0.md b), con el par en el modo de vertido --pour-mode, por defecto PourModes.Default = run
// (decisión del dueño D-005 del 2026-10-08, que sustituye a D-001; docs/dev/decisiones.md): run = la racha visible que
// cabe, con vertido parcial (02 §1.2, R9); single = una dosis por vertido, solo pruebas. El pack no declara el modo
// (08 §4.1) y asume run. --spec/--out (generación procedural): andamiaje que valida los argumentos y sale con 1 (fallar
// cerrado); se portará desde tools/levelgen_proto.py sobre AF.Core y AF.Solver.
// Todo lo que se imprime es ASCII (.claude/rules/ci.md): la consola de Windows y Git Bash corrompen
// '§' y los acentos al capturar la salida. Los comentarios sí pueden llevar acentos.
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Collections;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;
using AmazonFill.Tools.LevelGenerator.Handmade;
using AmazonFill.Tools.LevelGenerator.Metrics;

const string Usage = """
    level-generator: generador offline de niveles de Amazon Fill (GDD 08 sec. 2.4; GDD 03)

    Uso:
      dotnet run --project tools/level-generator -- --handmade <spec.json> --out <carpeta> --report <informe.md> [--pour-mode run|single] [--check]
      dotnet run --project tools/level-generator -c Release -- --spec <campaign.json> --levels a-b --out <carpeta> [--report <informe.md>] [--check]
      dotnet run --project tools/level-generator -- --validate-spec <campaign.json>
      dotnet run --project tools/level-generator -- --parity [--report tools/out/parity_report.md]
      dotnet run --project tools/level-generator -c Release -- --self-test [tinaja]
      dotnet run --project tools/level-generator -- --help

    --validate-spec: comprueba la especificacion de la campana (specs/campaign_v1.json) contra la rotacion de la
      propuesta de D-007 (d.2, d.3 y las comprobaciones de d.4) y el escalon de D-009 (c.3, c.4); imprime un resumen
      ASCII por ciclo. Codigos: 0 = cumple; 1 = alguna regla falla; 2 = especificacion ilegible.
    --self-test: pruebas de la herramienta sin proyecto de tests (hoy solo "tinaja", la de por defecto): el paso Ti de la
      Tinaja con semillas fijas (tope de dosis por tipo de nivel, T4, boca, hidden, su cuia contigua y en la misma fila),
      el slot 323 de specs/sample_tinaja.json con sus filtros (ruta segura, paseos, S0 sin trampas) y su JSON
      (pack_schema 3), el alivio sin frutas de la tinaja, el tope de 12 cuias con tinaja (02 sec. 5), los tableros de la
      ficha b.2 y, como INFO, la calibracion de Morpho (nodos/ms del PC) en los estados mas pesados del pack de muestra
      (tools/out/sample_tinaja). Desde la raiz del repositorio.
      Codigos: 0 = todo PASS; 1 = algun FAIL; 2 = uso o especificacion ilegible.
    --parity: recalcula con AF.Core/AF.Solver par, complejidad, ramificacion, trampas y la variante -1 color de los 40
      niveles del prototipo (tools/out/levels_proto.json y tools/out_inverse/levels_proto.json; GDD 03 sec. 17-18, 09 R26)
      y escribe el informe. Codigos: 0 = umbrales cumplidos; 1 = no; 2 = uso o archivos ausentes.

    --handmade: tutorial hecho a mano (GDD 02 sec. 13; docs/dev/prototipo-v0.md b). Comprueba cada nivel con
      AF.Core (BFS sobre PourRules) y AF.Solver, escribe <carpeta>/<pack_id>.json (subconjunto de af-level/1)
      solo si todo pasa, y siempre el informe. Con --check no escribe el pack: falla si el de disco difiere.
      Codigos: 0 = todo PASS; 1 = alguna comprobacion falla (o --check y el pack difiere); 2 = uso o spec ilegible.
    --pour-mode: modo de vertido con el que se calcula el par (decision D-005, que sustituye a D-001; clave
      rules_pour_mode): run = la racha visible que cabe, con vertido parcial (por defecto; GDD 02 sec. 1.2 y R9) o
      single = una dosis por vertido (solo pruebas). El pack no declara el modo y asume run, el del cliente por
      defecto, asi que un pack generado con single solo vale para jugar en single. El informe lo declara.
    --spec: generacion procedural de la campana (GDD 03 sec. 2; D-009 y D-007): candidatos por slot (walk, deep o fill),
      superposicion de mecanicas, tintes, A*, bot x40, trampas tempranas, indice D, colocacion en el ciclo de 20, alivio y
      un pack por mundo mundo_{mm}_v1.json (af-level/1, pack_schema 2, o 3 si algun nivel lleva la tinaja, D-019).
      --levels por defecto 11-200; D depende del pool
      de cada rango de 03 sec. 11, asi que --check debe usar los mismos --levels. Con --check no escribe nada y compara
      byte a byte con los packs del disco. Los tiempos salen por consola; el informe no lleva tiempos.
    Codigos de salida: 0 = correcto o ayuda; 1 = la generacion o las comprobaciones fallaron;
    2 = uso incorrecto (sin argumentos, faltan opciones, u opcion desconocida o repetida).
    """;

// Todo el texto con InvariantCulture (.claude/rules/data-json.md): también en los hilos de Parallel.
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

// Sin argumentos es un error, no una petición de ayuda: fallar cerrado (.claude/rules/ci.md).
if (args.Length == 0)
{
    return UsageError("faltan --spec y --out.");
}

bool wantsHelp = args.Contains("--help") || args.Contains("-h");
if (args.Length >= 1 && args[0] == "--validate-spec")
{
    return args.Length == 2 ? SpecValidator.Run(args[1]) : UsageError("--validate-spec lleva un solo archivo.");
}

if (args.Length >= 1 && args[0] == "--self-test")
{
    if (args.Length == 1 || (args.Length == 2 && args[1] == "tinaja"))
    {
        return TinajaSelfTest.Run();
    }

    return UsageError("--self-test solo admite: tinaja.");
}

if (args.Length >= 1 && args[0] == "--parity")
{
    if (args.Length == 1)
    {
        return Parity.Run("tools/out/parity_report.md");
    }

    return args.Length == 3 && args[1] == "--report" ? Parity.Run(args[2]) : UsageError("--parity solo admite --report <informe.md>.");
}

if (wantsHelp)
{
    Console.WriteLine(Usage);
    Console.WriteLine($"Nucleo enlazado: {Describe(typeof(BoardState))}, {Describe(typeof(MinHeap))}");
    return 0;
}

string? spec = null;
string? handmade = null;
string? outDir = null;
string? report = null;
string? pourModeKey = null;
string? levelsArg = null;
bool check = false;
for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];
    if (arg == "--spec" && i + 1 < args.Length && spec is null)
    {
        spec = args[++i];
    }
    else if (arg == "--handmade" && i + 1 < args.Length && handmade is null)
    {
        handmade = args[++i];
    }
    else if (arg == "--out" && i + 1 < args.Length && outDir is null)
    {
        outDir = args[++i];
    }
    else if (arg == "--report" && i + 1 < args.Length && report is null)
    {
        report = args[++i];
    }
    else if (arg == "--pour-mode" && i + 1 < args.Length && pourModeKey is null)
    {
        pourModeKey = args[++i];
    }
    else if (arg == "--levels" && i + 1 < args.Length && levelsArg is null)
    {
        levelsArg = args[++i];
    }
    else if (arg == "--check" && !check)
    {
        check = true;
    }
    else
    {
        return UsageError($"argumento no valido, repetido o sin valor: {arg}");
    }
}

if (handmade is not null)
{
    if (spec is not null)
    {
        return UsageError("--handmade y --spec no se combinan.");
    }

    if (outDir is null || report is null)
    {
        return UsageError(outDir is null ? "falta --out." : "falta --report.");
    }

    PourMode pourMode = PourModes.Default;
    if (pourModeKey is not null && !PourModes.TryParse(pourModeKey, out pourMode))
    {
        return UsageError($"--pour-mode debe ser {PourModes.RunKey} o {PourModes.SingleKey}: {pourModeKey}");
    }

    return HandmadeRunner.Run(handmade, outDir, report, check, pourMode);
}

if (levelsArg is not null && spec is null)
{
    return UsageError("--levels solo vale con --spec.");
}

if (pourModeKey is not null)
{
    return UsageError("--pour-mode solo vale con --handmade (la campana se genera en run, D-005).");
}

if (spec is null || outDir is null)
{
    return UsageError(spec is null ? "falta --spec (o --handmade)." : "falta --out.");
}

return CampaignRunner.Run(spec, levelsArg ?? "11-200", outDir, report, check);

static int UsageError(string message)
{
    Console.Error.WriteLine($"level-generator: {message}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(Usage);
    return 2;
}

static string Describe(Type type)
{
    var name = type.Assembly.GetName();
    return $"{name.Name} {name.Version}";
}
