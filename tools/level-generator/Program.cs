// level-generator: generador offline de niveles de Aquazonia (GDD 08 §2.4; GDD 03).
// Andamiaje: por ahora valida los argumentos y sale con 1 (fallar cerrado). La generación se portará
// desde tools/levelgen_proto.py sobre AF.Core y AF.Solver, las mismas reglas que ejecuta el cliente.
// Todo lo que se imprime es ASCII (.claude/rules/ci.md): la consola de Windows y Git Bash corrompen
// '§' y los acentos al capturar la salida. Los comentarios sí pueden llevar acentos.
using AF.Core.Board;
using AF.Solver.Collections;

const string Usage = """
    level-generator: generador offline de niveles de Aquazonia (GDD 08 sec. 2.4; GDD 03)

    Uso:
      dotnet run --project tools/level-generator -- --spec <spec.json> --out <carpeta>
      dotnet run --project tools/level-generator -- --help

    Entrada:  spec.json con los parametros del mundo (GDD 03).
    Salida:   mundo_{nn}_v{vuelta}.json en formato af-level/1 (schemas/af-level-1.schema.json) y report.csv.

    Codigos de salida: 0 = correcto o ayuda; 1 = la generacion fallo (hoy: no implementada);
    2 = uso incorrecto (sin argumentos, falta --spec o --out, u opcion desconocida).
    Estado: andamiaje. La generacion aun no esta portada desde tools/levelgen_proto.py.
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
string? outDir = null;
for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];
    if (arg == "--spec" && i + 1 < args.Length && spec is null)
    {
        spec = args[++i];
    }
    else if (arg == "--out" && i + 1 < args.Length && outDir is null)
    {
        outDir = args[++i];
    }
    else
    {
        return UsageError($"argumento no valido, repetido o sin valor: {arg}");
    }
}

if (spec is null || outDir is null)
{
    return UsageError(spec is null ? "falta --spec." : "falta --out.");
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
