// --self-test variante (P7b de la tanda 4): la opción --variant a|b de level-qa, la que pasa la compuerta sobre la salida B de
// D-019 (campaign_v2.json sin la Tinaja). Ejecuta el propio level-qa en un proceso hijo, por la línea de comandos (el mismo
// análisis de argumentos del CI), con --no-reproduce, sobre un pack temporal de dos niveles sacados del pack de muestra de la
// tinaja (tools/out/sample_tinaja/mundo_07_v1.json, 321-340, generado por P1):
//   - el 323 tal cual: estreno de la tinaja, celda ★Ti en la a y Ma (su respaldo, d.5.4) en la b;
//   - el 328 de la muestra (una comodidad pura, «C») con el número 327: celda Ti en la a y C (su respaldo) en la b.
// Comprueba, solo con las dos comprobaciones de las mecánicas del calendario (QA-MECH-SPEC y QA-MECH-CELL; las demás fallan
// por ser dos niveles sueltos, y no se miran):
//   - con --variant a (y sin --variant, porque la publicada de campaign_v2 es la a): el 323 pasa y el 327 da QA-MECH-CELL (la
//     tinaja de la celda no está en el nivel);
//   - con --variant b: al revés, el 327 pasa y el 323 da QA-MECH-SPEC (Ti no está en la celda Ma) y QA-MECH-CELL (falta el
//     Matapi); en los tres casos, código 1 (la especificación se leyó), no 2;
//   - los dos errores de uso salen con 2 y su motivo por stderr: una variante inexistente (--variant c con campaign_v2) y una
//     especificación sin variantes (--variant b con campaign_v1, la de por defecto).
// Imprime ASCII (PASS / FAIL) y sale con 0 si todo pasa, 1 si algo falla y 2 si falta el pack de muestra o la especificación.
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class VariantOptionSelfTest
{
    public const string SamplePack = "tools/out/sample_tinaja/mundo_07_v1.json";
    public const string CampaignV2 = "tools/level-generator/specs/campaign_v2.json";

    /// <summary>Las comprobaciones de las mecánicas del calendario, las que cambian con la variante.</summary>
    private static readonly string[] CalendarChecks = ["QA-MECH-CELL", "QA-MECH-SPEC"];

    public static int Run()
    {
        int failures = 0;
        void Check(bool ok, string what)
        {
            Console.WriteLine(AsciiText.Of((ok ? "PASS " : "FAIL ") + what));
            failures += ok ? 0 : 1;
        }

        string dir = Path.Combine(Path.GetTempPath(), "af-qa-variant-" + Guid.NewGuid().ToString("N"));
        string pack = Path.Combine(dir, "mundo_07_v1.json");
        try
        {
            CampaignSpec a = CampaignSpec.Load(CampaignV2, CampaignSpec.VariantA);
            CampaignSpec b = CampaignSpec.Load(CampaignV2, CampaignSpec.VariantB);
            Check(a.Plan(323).Cell.Has("Ti") && a.Plan(327).Cell.Has("Ti") && b.Plan(323).Cell.Literal == "Ma" && b.Plan(327).Cell.Literal == "C"
                  && a.PublishedVariant == CampaignSpec.VariantA,
                "campaign_v2: el 323 y el 327 llevan la Tinaja en la a; en la b, Ma y C; la publicada es la a");
            Directory.CreateDirectory(dir);
            File.WriteAllText(pack, BuildPack());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or CampaignSpecException or InvalidOperationException)
        {
            Console.Error.WriteLine(AsciiText.Of("level-qa --self-test variante: " + e.Message + " (ejecuta desde la raiz del repositorio)"));
            return 2;
        }

        try
        {
            string[] common = [pack, "--no-reproduce", "--spec", CampaignV2];
            (int codeA, string outA, _) = RunSelf([.. common, "--variant", "a"]);
            (int codeDefault, string outDefault, _) = RunSelf(common);
            (int codeB, string outB, _) = RunSelf([.. common, "--variant", "b"]);
            string expectedA = "QA-MECH-CELL 327";
            string expectedB = "QA-MECH-CELL 323, QA-MECH-SPEC 323";
            Check(codeA == 1 && Calendar(outA) == expectedA,
                F($"--variant a: el 323 pasa y el 327 da QA-MECH-CELL (codigo {codeA}; {Calendar(outA)})"));
            Check(codeDefault == 1 && Calendar(outDefault) == expectedA,
                F($"sin --variant, la publicada (a): lo mismo que con --variant a (codigo {codeDefault}; {Calendar(outDefault)})"));
            Check(codeB == 1 && Calendar(outB) == expectedB,
                F($"--variant b: el 327 pasa y el 323 da QA-MECH-CELL y QA-MECH-SPEC (codigo {codeB}; {Calendar(outB)})"));
            Check(outA.Contains("[QA-MECH-CELL] nivel 327: tinaja de la celda (Ti) no esta en el nivel", StringComparison.Ordinal),
                "--variant a: el motivo del 327 es la tinaja de su celda");
            Check(outB.Contains("[QA-MECH-SPEC] nivel 323: Ti no esta en la celda del calendario (Ma)", StringComparison.Ordinal)
                  && outB.Contains("[QA-MECH-CELL] nivel 323: matapi de la celda (Ma) no esta en el nivel", StringComparison.Ordinal),
                "--variant b: los motivos del 323 son la Tinaja fuera de su celda y el Matapi que falta");

            (int codeC, _, string errC) = RunSelf([.. common, "--variant", "c"]);
            Check(codeC == 2 && errC.Contains("la especificacion no tiene la variante 'c' (tiene a, b)", StringComparison.Ordinal),
                F($"--variant c con campaign_v2: codigo 2 y el motivo (codigo {codeC})"));
            (int codeV1, _, string errV1) = RunSelf([pack, "--no-reproduce", "--variant", "b"]);
            Check(codeV1 == 2 && errV1.Contains("campaign_v1.json: la especificacion no tiene la variante 'b' (tiene a)", StringComparison.Ordinal),
                F($"--variant b con campaign_v1 (sin variantes): codigo 2 y el motivo (codigo {codeV1})"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }

        Console.WriteLine(failures == 0 ? "self-test variante: todo PASS" : F($"self-test variante: {failures} FAIL"));
        return failures == 0 ? 0 : 1;
    }

    private static string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);

    /// <summary>El 323 de la muestra tal cual y su 328 (comodidad pura) con el número 327, en la cabecera del pack de muestra.</summary>
    private static string BuildPack()
    {
        JsonObject root = JsonNode.Parse(File.ReadAllText(SamplePack))!.AsObject();
        JsonArray levels = root["levels"]!.AsArray();
        JsonObject Level(int n) => levels.OfType<JsonObject>().Single(l => l["level"]!.GetValue<int>() == n);
        JsonObject debut = Level(323).DeepClone().AsObject();
        JsonObject plain = Level(328).DeepClone().AsObject();
        if (debut["attrs"]?.AsArray().OfType<JsonObject>().Any(m => m["type"]?.GetValue<string>() == "tinaja") != true
            || plain["attrs"]?.AsArray().OfType<JsonObject>().Any() == true)
        {
            throw new InvalidOperationException(SamplePack + ": el 323 tiene que llevar la tinaja y el 328 ninguna mecanica");
        }

        plain["level"] = 327;
        plain["id"] = "m07_v1_n0327";
        plain["seed"] = "amazonfill:v1:m07:v1:n0327";
        root["levels"] = new JsonArray(debut, plain);
        return root.ToJsonString();
    }

    /// <summary>Las incidencias de las mecánicas del calendario (FAIL, no avisos), «id nivel», ordenadas y separadas por comas.</summary>
    private static string Calendar(string stdout) => string.Join(", ", stdout.Split('\n')
        .Select(line => line.Trim())
        .Where(line => line.StartsWith("FAIL", StringComparison.Ordinal))
        .Select(line => CalendarChecks.FirstOrDefault(id => line.Contains("[" + id + "] nivel ", StringComparison.Ordinal)) is string id
            ? id + " " + line[(line.IndexOf("] nivel ", StringComparison.Ordinal) + 8)..].Split(':')[0]
            : null)
        .OfType<string>()
        .Distinct()
        .Order(StringComparer.Ordinal));

    /// <summary>Este mismo level-qa en un proceso hijo (apphost o dotnet + dll), desde el directorio actual.</summary>
    private static (int Code, string Out, string Err) RunSelf(string[] args)
    {
        string exe = Environment.ProcessPath ?? throw new InvalidOperationException("sin ruta del proceso");
        var psi = new ProcessStartInfo(exe) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        if (Path.GetFileNameWithoutExtension(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            psi.ArgumentList.Add(typeof(VariantOptionSelfTest).Assembly.Location);
        }

        foreach (string arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using Process p = Process.Start(psi)!;
        Task<string> err = p.StandardError.ReadToEndAsync();
        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, output.Replace("\r", "", StringComparison.Ordinal), err.Result.Replace("\r", "", StringComparison.Ordinal));
    }
}
