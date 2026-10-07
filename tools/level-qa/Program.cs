// level-qa: control de calidad de paquetes de niveles de Amazon Fill (GDD 08 §2.4).
// Andamiaje: por ahora valida los argumentos y sale con 1 (fallar cerrado). Re-resolverá cada nivel
// con AF.Solver y fallará el CI si algo no cumple.
// Todo lo que se imprime es ASCII (.claude/rules/ci.md): la consola de Windows y Git Bash corrompen
// '§' y los acentos al capturar la salida. Los comentarios sí pueden llevar acentos.
using AF.Core.Board;
using AF.Solver.Collections;

const string Usage = """
    level-qa: control de calidad de paquetes de niveles de Amazon Fill (GDD 08 sec. 2.4)

    Uso:
      dotnet run --project tools/level-qa -- <paquete.json> [<paquete.json> ...] [--report <informe.md>]
      dotnet run --project tools/level-qa -- --help

    Comprueba en cada nivel: resolucion con el solver y par, etiquetas del bot (>= 75 % / >= 90 %),
    Aguas turbias con al menos un vertido seguro y determinismo por semilla.

    Codigos de salida: 0 = todo cumple o ayuda; 1 = algun nivel no cumple (hoy: no implementado);
    2 = uso incorrecto o sin paquetes.
    Sin argumentos sale con 2: un glob vacio en el CI nunca deja pasar la compuerta sin revisar nada.
    """;

// Sin argumentos es un error, no una petición de ayuda: fallar cerrado (.claude/rules/ci.md).
if (args.Length == 0)
{
    return UsageError("no se ha indicado ningun paquete.");
}

bool wantsHelp = args.Contains("--help") || args.Contains("-h");
if (wantsHelp)
{
    Console.WriteLine(Usage);
    Console.WriteLine($"Nucleo enlazado: {Describe(typeof(BoardState))}, {Describe(typeof(MinHeap))}");
    return 0;
}

var packages = new List<string>();
string? report = null;
for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];
    if (arg == "--report" && i + 1 < args.Length && report is null)
    {
        report = args[++i];
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

Console.Error.WriteLine(
    $"level-qa: {packages.Count} paquete(s), informe: {report ?? "(ninguno)"}; "
    + "la comprobacion todavia no esta implementada.");
Console.Error.WriteLine($"Nucleo enlazado: {Describe(typeof(BoardState))}, {Describe(typeof(MinHeap))}");
return 1;

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
