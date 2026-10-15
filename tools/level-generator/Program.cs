// level-generator: generador offline de niveles de Amazon Fill (GDD 08 §2.4; GDD 03).
// --handmade: comprueba el tutorial hecho a mano (02 §13; 03 §4.4-§5) con AF.Core y AF.Solver y emite el pack v0 y su
// informe (docs/dev/prototipo-v0.md b), con el par en el modo de vertido --pour-mode, por defecto PourModes.Default = run
// (decisión del dueño D-005 del 2026-10-08, que sustituye a D-001; docs/dev/decisiones.md): run = la racha visible que
// cabe, con vertido parcial (02 §1.2, R9); single = una dosis por vertido, solo pruebas. El pack no declara el modo
// (08 §4.1) y asume run. --spec/--out (generación procedural): andamiaje que valida los argumentos y sale con 1 (fallar
// cerrado); se portará desde tools/levelgen_proto.py sobre AF.Core y AF.Solver.
// --seasons (SE1): tabla de temporada del cliente copiada del calendario versionado (pregunta 32 b, D-020;
// Generation/SeasonTableWriter.cs).
// --registry (P4): registro de combinaciones publicadas (propuesta de D-009 d.5.6; Campaign/SpecValidator.cs,
// CombinationsRegistry). --variant a|b (P4): variante de una especificación con respaldos (d.5.4; salida B de D-019).
// Sin --levels, --spec genera todos los niveles de la especificación (first_level-last_level).
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
      dotnet run --project tools/level-generator -c Release -- --spec <campaign.json> [--levels a-b] [--variant a|b] --out <carpeta> [--report <informe.md>] [--check]
      dotnet run --project tools/level-generator -- --validate-spec <campaign.json>
      dotnet run --project tools/level-generator -- --registry <campaign.json> --out <registro.json> [--variant a|b] [--check]
      dotnet run --project tools/level-generator -- --parity [--report tools/out/parity_report.md]
      dotnet run --project tools/level-generator -- --seasons <campaign.json> [<campaign.json> ...] --out <tabla.json> [--check]
      dotnet run --project tools/level-generator -c Release -- --self-test [tinaja|seasons|variants|registry]
      dotnet run --project tools/level-generator -- --help

    --validate-spec: comprueba la especificacion de la campana (specs/campaign_v1.json, 11-200, y campaign_v2.json,
      201-700) contra la rotacion de la propuesta de D-007 (d.2, d.3, las comprobaciones de d.4 y d.5.1-d.5.5) y el
      escalon de D-009 (c.3, c.4); imprime un resumen ASCII por ciclo. Con "previous" (campaign_v2 -> campaign_v1) cuenta
      la historia; con dos variantes (respaldos en "fallback", d.5.4) comprueba las dos, cada una con su registro de
      combinaciones en memoria (d.5.6). Codigos: 0 = cumple; 1 = alguna regla falla; 2 = especificacion ilegible.
    --registry: registro de combinaciones publicadas (formato [P] af-combinations/1; d.5.6): una entrada {level, codes,
      kind} por conjunto de codigos nunca visto, desde la primera especificacion de la cadena de "previous", con la
      variante publicada (published_variant) o la de --variant. Con --check no escribe: falla si el del disco difiere.
      Codigos: 0 = escrito o identico; 1 = difiere o la especificacion no pasa --validate-spec; 2 = uso o especificacion
      ilegible.
    --seasons: tabla de temporada del cliente (pregunta 32 b de GDD.md sec. 6.3, D-020; D-009 punto 6): copia la
      temporada A o B de cada ciclo de 20 de las especificaciones (hoy specs/campaign_v1.json: ciclos 21-181) en
      <tabla.json> con el formato [P] af-seasons/1 {format, source, cycles[{from_level, season}]}; varias
      especificaciones se unen en orden, sin huecos ni solapes. Con --check no escribe: falla si la del disco difiere
      (desfase con el calendario). Codigos: 0 = escrita o identica; 1 = desfase, o algun ciclo sin temporada A o B;
      2 = uso o especificacion ilegible.
    --self-test: pruebas de la herramienta sin proyecto de tests; sin nombre, todas. "seasons": la tabla de temporada
      (ciclos 21-181 identicos a campaign_v1, formato, determinismo, desfase con la del disco, union y rechazos).
      "tinaja": el paso Ti de la Tinaja con semillas fijas (tope de dosis por tipo de nivel, T4, boca, hidden, su cuia
      contigua y en la misma fila), el slot 323 de specs/sample_tinaja.json con sus filtros (ruta segura, paseos, S0 sin
      trampas) y su JSON (pack_schema 3), el alivio sin frutas de la tinaja, el tope de 12 cuias con tinaja (02 sec. 5),
      los tableros de la ficha b.2 y, como INFO, la calibracion de Morpho (nodos/ms del PC) en los estados mas pesados
      del pack de muestra (tools/out/sample_tinaja). "variants": las variantes de la 1.0 (P5; Ln, Ha, B2 y Fl): codigos,
      el orden de la superposicion sin las variantes, cada paso sobre tableros base fijos (colocacion valida segun
      LevelValidation, determinismo), los estrenos 243, 263, 283 y 303 de specs/sample_variants.json (un ejemplar, 2 vacios y
      bot < 0,20), su JSON (attrs, key2 de la Flor, intro_mechanic y pack_schema 3) leido de vuelta con el mismo hash, y la
      key2 en el alivio y al cambiar los colores. "registry" (P4): specs/campaign_v2.json con sus dos variantes (la b
      cambia las 19 celdas con la Tinaja), --validate-spec en verde en las dos y en campaign_v1, el registro de
      combinaciones (102 entradas en la a y 96 en la b, el versionado identico al regenerado), el hueco maximo de 34
      entre primeras veces, max_risk_pairs "libre", K = 13 en los hitos de 561-700, --seasons de v1 + v2 y los fixtures
      negativos de P4. Desde la raiz del repositorio.
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
      un pack por mundo mundo_{mm}_v1.json (af-level/1, pack_schema 2, o 3 si algun nivel lleva la tinaja, D-019, o una
      variante de la 1.0 del 3: Hoja atada, Flor, Brote de dos hojas o el estreno de la Liana de cuenta; D-023 punto 5).
      --levels por defecto todos los de la especificacion (11-200 en campaign_v1, 201-700 en campaign_v2); D depende
      del pool de cada rango de 03 sec. 11, asi que --check debe usar los mismos --levels. --variant: la variante de una
      especificacion con respaldos (por defecto la publicada; b = la salida B de D-019, sin la Tinaja). Con --check no escribe nada y compara
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
    if (args.Length == 1)
    {
        int tinaja = TinajaSelfTest.Run();
        int seasons = SeasonTableWriter.SelfTest();
        int variants = VariantsSelfTest.Run();
        int registry = RegistrySelfTest.Run();
        return Math.Max(Math.Max(tinaja, seasons), Math.Max(variants, registry));
    }

    if (args.Length == 2 && args[1] == "registry")
    {
        return RegistrySelfTest.Run();
    }

    if (args.Length == 2 && args[1] == "variants")
    {
        return VariantsSelfTest.Run();
    }

    if (args.Length == 2 && args[1] == "tinaja")
    {
        return TinajaSelfTest.Run();
    }

    if (args.Length == 2 && args[1] == "seasons")
    {
        return SeasonTableWriter.SelfTest();
    }

    return UsageError("--self-test solo admite: tinaja, seasons, variants o registry.");
}

if (args.Length >= 1 && args[0] == "--seasons")
{
    var seasonSpecs = new List<string>();
    string? seasonsOut = null;
    bool seasonsCheck = false;
    for (int i = 1; i < args.Length; i++)
    {
        if (args[i] == "--out" && i + 1 < args.Length && seasonsOut is null)
        {
            seasonsOut = args[++i];
        }
        else if (args[i] == "--check" && !seasonsCheck)
        {
            seasonsCheck = true;
        }
        else if (!args[i].StartsWith("--", StringComparison.Ordinal) && seasonsOut is null && !seasonsCheck)
        {
            seasonSpecs.Add(args[i]);
        }
        else
        {
            return UsageError($"--seasons: argumento no valido, repetido o sin valor: {args[i]}");
        }
    }

    if (seasonSpecs.Count == 0 || seasonsOut is null)
    {
        return UsageError(seasonSpecs.Count == 0 ? "--seasons lleva al menos una especificacion." : "--seasons: falta --out.");
    }

    return SeasonTableWriter.Run(seasonSpecs, seasonsOut, seasonsCheck);
}

if (args.Length >= 1 && args[0] == "--registry")
{
    string? registrySpec = null;
    string? registryOut = null;
    string? registryVariant = null;
    bool registryCheck = false;
    for (int i = 1; i < args.Length; i++)
    {
        if (args[i] == "--out" && i + 1 < args.Length && registryOut is null)
        {
            registryOut = args[++i];
        }
        else if (args[i] == "--variant" && i + 1 < args.Length && registryVariant is null)
        {
            registryVariant = args[++i];
        }
        else if (args[i] == "--check" && !registryCheck)
        {
            registryCheck = true;
        }
        else if (!args[i].StartsWith("--", StringComparison.Ordinal) && registrySpec is null)
        {
            registrySpec = args[i];
        }
        else
        {
            return UsageError($"--registry: argumento no valido, repetido o sin valor: {args[i]}");
        }
    }

    if (registrySpec is null || registryOut is null)
    {
        return UsageError(registrySpec is null ? "--registry lleva una especificacion." : "--registry: falta --out.");
    }

    return CombinationsRegistry.Run(registrySpec, registryOut, registryVariant, registryCheck);
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
string? variantArg = null;
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
    else if (arg == "--variant" && i + 1 < args.Length && variantArg is null)
    {
        variantArg = args[++i];
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

    if (variantArg is not null || levelsArg is not null)
    {
        return UsageError("--variant y --levels solo valen con --spec.");
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

if (variantArg is not null && spec is null)
{
    return UsageError("--variant solo vale con --spec o --registry.");
}

if (pourModeKey is not null)
{
    return UsageError("--pour-mode solo vale con --handmade (la campana se genera en run, D-005).");
}

if (spec is null || outDir is null)
{
    return UsageError(spec is null ? "falta --spec (o --handmade)." : "falta --out.");
}

return CampaignRunner.Run(spec, levelsArg, outDir, report, check, variantArg);

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
