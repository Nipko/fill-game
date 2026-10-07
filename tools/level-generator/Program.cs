// level-generator: generador offline de niveles de Amazon Fill (GDD 08 §2.4; GDD 03).
// --handmade: comprueba el tutorial hecho a mano (02 §13; 03 §4.4) con AF.Core y AF.Solver y emite el pack v0 y su
// informe (docs/dev/prototipo-v0.md b). --spec/--out (generación procedural): andamiaje que valida los argumentos y sale
// con 1 (fallar cerrado); se portará desde tools/levelgen_proto.py sobre AF.Core y AF.Solver.
// Todo lo que se imprime es ASCII (.claude/rules/ci.md): la consola de Windows y Git Bash corrompen
// '§' y los acentos al capturar la salida. Los comentarios sí pueden llevar acentos.
using AF.Core.Board;
using AF.Solver.Collections;
using AmazonFill.Tools.LevelGenerator.Handmade;

const string Usage = """
    level-generator: generador offline de niveles de Amazon Fill (GDD 08 sec. 2.4; GDD 03)

    Uso:
      dotnet run --project tools/level-generator -- --handmade <spec.json> --out <carpeta> --report <informe.md> [--check]
      dotnet run --project tools/level-generator -- --spec <spec.json> --out <carpeta>
      dotnet run --project tools/level-generator -- --help

    --handmade: tutorial hecho a mano (GDD 02 sec. 13; docs/dev/prototipo-v0.md b). Comprueba cada nivel con
      AF.Core (BFS sobre PourRules) y AF.Solver, escribe <carpeta>/<pack_id>.json (subconjunto de af-level/1)
      solo si todo pasa, y siempre el informe. Con --check no escribe el pack: falla si el de disco difiere.
      Codigos: 0 = todo PASS; 1 = alguna comprobacion falla (o --check y el pack difiere); 2 = uso o spec ilegible.
    --spec: generacion procedural (GDD 03). Salida prevista: mundo_{nn}_v{vuelta}.json en formato af-level/1
      (schemas/af-level-1.schema.json) y report.csv. Estado: andamiaje, aun no portada desde
      tools/levelgen_proto.py (sale con 1).

    Codigos de salida: 0 = correcto o ayuda; 1 = la generacion o las comprobaciones fallaron;
    2 = uso incorrecto (sin argumentos, faltan opciones, u opcion desconocida o repetida).
    """;

// Sin argumentos es un error, no una petición de ayuda: fallar cerrado (.claude/rules/ci.md).
if (args.Length == 0)
{
    return UsageError("faltan --spec y --out.");
}

bool wantsHelp = args.Contains("--help") || args.Contains("-h");
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

    return HandmadeRunner.Run(handmade, outDir, report, check);
}

if (report is not null || check)
{
    return UsageError("--report y --check solo valen con --handmade.");
}

if (spec is null || outDir is null)
{
    return UsageError(spec is null ? "falta --spec (o --handmade)." : "falta --out.");
}

Console.Error.WriteLine(
    $"level-generator: spec {spec}, salida {outDir}; la generacion todavia no esta implementada.");
Console.Error.WriteLine($"Nucleo enlazado: {Describe(typeof(BoardState))}, {Describe(typeof(MinHeap))}");
return 1;

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
