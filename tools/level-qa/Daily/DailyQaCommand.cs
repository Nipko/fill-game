// level-qa --daily (R4 del encargo docs/dev/traspaso/spark-reto-del-rio.md): QA del pool del Reto del río.
//   dotnet run --project tools/level-qa -c Release -- --daily <daily_pool_v1.json> [--spec <daily_v1.json>] [--partial] [--no-reproduce]
//   dotnet run --project tools/level-qa -- --self-test daily
// Program.cs despacha aquí antes de su --self-test genérico (que rechazaría "daily") y de la lectura de argumentos de la
// campaña. Las comprobaciones, en Daily/DailyChecks.cs (QA-DAILY-*).
//   - --spec: por defecto tools/level-generator/specs/daily_v1.json; los tintes y los pares de riesgo, de su tints_from.
//   - --partial: valida una muestra (p. ej. tools/out/daily_sample): los días que faltan no fallan, pero sale siempre el aviso
//     QA-DAILY-DAYS «muestra parcial … no es el pool completo» y no se mira el tamaño del pool. Sin --partial, el pack tiene
//     que traer las 3 bandas × 730 días.
//   - --no-reproduce: salta QA-DAILY-REPRO (aviso); nunca en el CI.
//   - Reproducción: ejecuta «dotnet run --project tools/level-generator -c Release -- --daily --spec <spec> --bands <las del
//     pack> --days <los del pack> --out <carpeta temporal>» y compara cada tablero (DailyChecks.ReproKeys). level-qa no puede
//     enlazar tools/level-generator/Daily/ sin tocar su .csproj (archivo existente, fuera de lo permitido en el Spark): el
//     enganche para el integrador es enlazarlos, como Generation/*.cs, y entonces llamar a DailyRunner en memoria.
//   - Daily/Shared/DailySpec.cs y DailyIds.cs son copias exactas de tools/level-generator/Daily/ por la misma razón;
//     --self-test daily falla si dejan de ser idénticas byte a byte.
// Imprime ASCII. Códigos: 0 = sin fallos (los avisos no fallan); 1 = algún fallo; 2 = uso, especificación o pack ilegibles.
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Daily;

namespace AmazonFill.Tools.LevelQa.Daily;

internal static class DailyQaCommand
{
    public const string DefaultSpec = "tools/level-generator/specs/daily_v1.json";
    public const string GeneratorProject = "tools/level-generator";
    public const string FixtureDir = "tools/level-qa/fixtures/daily";

    public static int Run(string[] args)
    {
        if (args.Length == 2 && args[0] == "--self-test" && args[1] == "daily")
        {
            return SelfTest();
        }

        if (args.Length < 1 || args[0] != "--daily")
        {
            return Usage("uso: --daily <pack> [--spec <daily_v1.json>] [--partial] [--no-reproduce] o --self-test daily.");
        }

        string? pack = null;
        string? specPath = null;
        bool partial = false;
        bool reproduce = true;
        for (int i = 1; i < args.Length; i++)
        {
            string a = args[i];
            if (a == "--spec" && i + 1 < args.Length && specPath is null)
            {
                specPath = args[++i];
            }
            else if (a == "--partial" && !partial)
            {
                partial = true;
            }
            else if (a == "--no-reproduce" && reproduce)
            {
                reproduce = false;
            }
            else if (!a.StartsWith('-') && pack is null)
            {
                pack = a;
            }
            else
            {
                return Usage($"argumento no valido, repetido o sin valor: {a}");
            }
        }

        if (pack is null)
        {
            return Usage("falta el pack.");
        }

        if (!TryLoad(specPath ?? DefaultSpec, out DailySpec? spec, out CampaignSpec? tints, out string? loadError))
        {
            Console.Error.WriteLine(AsciiText.Of("level-qa --daily: " + loadError));
            return 2;
        }

        DailyQaPack? read = DailyChecks.Read(pack, out string? readError);
        if (read is null)
        {
            Console.Error.WriteLine(AsciiText.Of("level-qa --daily: " + readError));
            return 2;
        }

        var sw = Stopwatch.StartNew();
        List<DailyIssue> issues = RunChecks(read, spec!, tints!, specPath ?? DefaultSpec, partial, reproduce, schema: true);
        int failures = issues.Count(i => !i.Warning);
        Console.WriteLine(AsciiText.Of(string.Create(CultureInfo.InvariantCulture,
            $"{pack.Replace('\\', '/')}: {read.Levels.Count} tableros, {read.Bytes} bytes{(partial ? ", muestra parcial (--partial)" : "")}, {failures} fallo(s)")));
        foreach (DailyIssue i in issues.OrderBy(i => i.Warning).ThenBy(i => i.Check, StringComparer.Ordinal).ThenBy(i => i.Board, StringComparer.Ordinal))
        {
            Console.WriteLine(AsciiText.Of($"{(i.Warning ? "AVISO" : "FAIL ")} [{i.Check}] {i.Board}: {i.Message}"));
        }

        Console.WriteLine("recuento por comprobacion (fallos / avisos):");
        foreach ((string id, _) in DailyChecks.Ids)
        {
            int f = issues.Count(i => i.Check == id && !i.Warning);
            int w = issues.Count(i => i.Check == id && i.Warning);
            if (f + w > 0)
            {
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  {id}: {f} / {w}"));
            }
        }

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{read.Levels.Count} tableros, {DailyChecks.Ids.Length} comprobaciones: {failures} fallo(s), {issues.Count(i => i.Warning)} aviso(s) en {sw.Elapsed.TotalSeconds:0.0} s"));
        return failures == 0 ? 0 : 1;
    }

    private static int Usage(string message)
    {
        Console.Error.WriteLine(AsciiText.Of("level-qa --daily: " + message));
        Console.Error.WriteLine("  --daily <daily_pool_v1.json> [--spec <daily_v1.json>] [--partial] [--no-reproduce]");
        Console.Error.WriteLine("  --self-test daily");
        return 2;
    }

    private static bool TryLoad(string specPath, out DailySpec? spec, out CampaignSpec? tints, out string? error)
    {
        spec = null;
        tints = null;
        error = null;
        try
        {
            spec = DailySpec.Load(specPath);
            tints = CampaignSpec.Load(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(specPath))!, spec.TintsFrom));
            return true;
        }
        catch (Exception e) when (e is DailySpecException or CampaignSpecException)
        {
            error = e.Message;
            return false;
        }
    }

    public static List<DailyIssue> RunChecks(DailyQaPack pack, DailySpec spec, CampaignSpec tints, string specPath, bool partial, bool reproduce, bool schema)
    {
        var issues = new List<DailyIssue>();
        if (schema)
        {
            issues.AddRange(DailyChecks.Schema(pack.Path));
        }

        issues.AddRange(DailyChecks.Header(pack, spec));
        foreach (DailyEval e in DailyChecks.Evaluate(pack))
        {
            issues.AddRange(DailyChecks.Level(e, spec, tints));
        }

        issues.AddRange(DailyChecks.Days(pack, partial));
        issues.AddRange(DailyChecks.Size(pack.Bytes, partial));
        issues.AddRange(DailyChecks.Repetition(pack));
        if (reproduce)
        {
            issues.AddRange(Reproduce(pack, specPath));
        }
        else
        {
            issues.Add(new DailyIssue("QA-DAILY-REPRO", "-", "--no-reproduce: no se comprueba que el generador reproduzca los tableros (03 sec. 17 #10)", Warning: true));
        }

        return issues;
    }

    /// <summary>QA-DAILY-REPRO: el generador, en un proceso hijo, con las bandas y los días del pack.</summary>
    private static IEnumerable<DailyIssue> Reproduce(DailyQaPack pack, string specPath)
    {
        List<DailyQaLevel> slots = pack.Levels.Where(l => l.Band is int b && l.Day is int d && DailyIds.Problem(b, d) is null).ToList();
        if (slots.Count == 0)
        {
            return [new DailyIssue("QA-DAILY-REPRO", "-", "ningun tablero con slot valido que regenerar")];
        }

        string bands = Ranges(slots.Select(l => l.Band!.Value));
        string days = Ranges(slots.Select(l => l.Day!.Value));
        string outDir = Path.Combine(Path.GetTempPath(), "af-daily-qa-" + Guid.NewGuid().ToString("N"));
        try
        {
            (int? code, string output) = RunDotnet("run", "--project", GeneratorProject, "-c", "Release", "--", "--daily", "--spec", specPath, "--bands", bands, "--days", days, "--out", outDir);
            if (code is null)
            {
                return [new DailyIssue("QA-DAILY-REPRO", "-", "no se pudo ejecutar dotnet para regenerar el pool")];
            }

            string regenerated = Directory.Exists(outDir) ? Directory.GetFiles(outDir, "*.json").FirstOrDefault() ?? "" : "";
            if (code != 0 || regenerated.Length == 0)
            {
                string detail = string.Join(" | ", output.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).TakeLast(3));
                return [new DailyIssue("QA-DAILY-REPRO", "-", $"el generador sale con {code}: {detail}")];
            }

            JsonNode regen = JsonNode.Parse(File.ReadAllBytes(regenerated))!;
            var byId = regen["levels"]!.AsArray().Where(x => x is not null).ToDictionary(x => x!["id"]!.GetValue<string>(), x => x!.AsObject(), StringComparer.Ordinal);
            var issues = new List<DailyIssue>();
            foreach (DailyQaLevel lv in slots)
            {
                string id = DailyIds.Id(lv.Band!.Value, lv.Day!.Value);
                if (!byId.TryGetValue(id, out JsonObject? r))
                {
                    issues.Add(new DailyIssue("QA-DAILY-REPRO", lv.Name, $"el generador no da {id}"));
                    continue;
                }

                string[] diff = DailyChecks.ReproKeys.Where(k => !JsonNode.DeepEquals(lv.Json[k], r[k])).ToArray();
                if (diff.Length > 0)
                {
                    issues.Add(new DailyIssue("QA-DAILY-REPRO", lv.Name, $"la semilla no reproduce el tablero: difieren {string.Join(", ", diff)}"));
                }
            }

            return issues;
        }
        finally
        {
            try
            {
                if (Directory.Exists(outDir))
                {
                    Directory.Delete(outDir, recursive: true);
                }
            }
            catch (IOException)
            {
                // Carpeta temporal: si no se puede borrar, se queda.
            }
        }
    }

    /// <summary>«0-9,20,30-31»: los valores distintos en orden, en rangos.</summary>
    public static string Ranges(IEnumerable<int> values)
    {
        List<int> v = values.Distinct().Order().ToList();
        var parts = new List<string>();
        for (int i = 0; i < v.Count;)
        {
            int j = i;
            while (j + 1 < v.Count && v[j + 1] == v[j] + 1)
            {
                j++;
            }

            parts.Add(i == j ? v[i].ToString(CultureInfo.InvariantCulture) : string.Create(CultureInfo.InvariantCulture, $"{v[i]}-{v[j]}"));
            i = j + 1;
        }

        return string.Join(",", parts);
    }

    private static (int? Code, string Output) RunDotnet(params string[] args)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (string a in args)
        {
            psi.ArgumentList.Add(a);
        }

        try
        {
            using Process p = Process.Start(psi)!;
            Task<string> err = p.StandardError.ReadToEndAsync();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return (p.ExitCode, output + err.Result);
        }
        catch (Win32Exception)
        {
            return (null, "");
        }
    }

    /// <summary>Fixtures de tools/level-qa/fixtures/daily/: archivo, opciones y la comprobación que tiene que fallar.</summary>
    private static readonly (string File, bool Partial, bool Reproduce, string Expected)[] Fixtures =
    [
        ("QA-DAILY-SCHEMA__clave_desconocida.json", true, false, "QA-DAILY-SCHEMA"),
        ("QA-DAILY-HEADER__otro_pack_id.json", true, false, "QA-DAILY-HEADER"),
        ("QA-DAILY-STRUCT__cuia_con_tres_dosis.json", true, false, "QA-DAILY-STRUCT"),
        ("QA-DAILY-SLOT__id_de_otro_dia.json", true, false, "QA-DAILY-SLOT"),
        ("QA-DAILY-DAYS__dia_repetido.json", true, false, "QA-DAILY-DAYS"),
        ("QA-DAILY-SHAPE__k_de_otro_dia.json", true, false, "QA-DAILY-SHAPE"),
        ("QA-DAILY-CUIAS-14__quince_cuias.json", true, false, "QA-DAILY-CUIAS-14"),
        ("QA-DAILY-PALETTE__tinte_antes_de_su_debut.json", true, false, "QA-DAILY-PALETTE"),
        ("QA-DAILY-RISK__b2_k9_tres_pares.json", true, false, "QA-DAILY-RISK"),
        ("QA-DAILY-MECH__murky_con_ocultos.json", true, false, "QA-DAILY-MECH"),
        ("QA-DAILY-FIELDS__con_difficulty.json", true, false, "QA-DAILY-FIELDS"),
        ("QA-DAILY-RELIEF__con_variante.json", true, false, "QA-DAILY-RELIEF"),
        ("QA-DAILY-WATER__mundo_de_otro_dia.json", true, false, "QA-DAILY-WATER"),
        ("QA-DAILY-ASTAR__irresoluble.json", true, false, "QA-DAILY-ASTAR"),
        ("QA-DAILY-PAR__casi_resuelto.json", true, false, "QA-DAILY-PAR"),
        ("QA-DAILY-BOT__fuera_de_ventana.json", true, false, "QA-DAILY-BOT"),
        ("QA-DAILY-REPRO__otra_profundidad.json", true, true, "QA-DAILY-REPRO"),
        ("daily_valido.json", false, false, "QA-DAILY-DAYS"),
    ];

    public const string ValidFixture = "daily_valido.json";

    private static int SelfTest()
    {
        int failures = 0;
        void Check(bool ok, string what)
        {
            Console.WriteLine(AsciiText.Of((ok ? "PASS " : "FAIL ") + what));
            failures += ok ? 0 : 1;
        }

        if (!TryLoad(DefaultSpec, out DailySpec? spec, out CampaignSpec? tints, out string? error))
        {
            Console.Error.WriteLine(AsciiText.Of($"level-qa --self-test daily: {error} (ejecuta desde la raiz del repositorio)"));
            return 2;
        }

        // Copias exactas del lector del generador.
        foreach (string name in new[] { "DailySpec.cs", "DailyIds.cs" })
        {
            string original = Path.Combine("tools/level-generator/Daily", name);
            string copy = Path.Combine("tools/level-qa/Daily/Shared", name);
            Check(File.Exists(original) && File.Exists(copy) && File.ReadAllBytes(original).AsSpan().SequenceEqual(File.ReadAllBytes(copy)),
                $"tools/level-qa/Daily/Shared/{name} es identico a tools/level-generator/Daily/{name}");
        }

        Check(Ranges([5, 0, 1, 2, 9, 3, 11, 10]) == "0-3,5,9-11", "rangos de --bands/--days para el generador: 0-3,5,9-11");
        Check(DailyChecks.Size(DailyChecks.MaxBytes, partial: false).Any() && !DailyChecks.Size(DailyChecks.MaxBytes - 1, partial: false).Any()
            && !DailyChecks.Size(DailyChecks.MaxBytes, partial: true).Any(), "QA-DAILY-SIZE: 2 MB falla, 2 MB - 1 pasa, una muestra parcial no lo mira");

        // El fixture válido: sin fallos como muestra parcial (con reproducción y esquema) y con el aviso de muestra parcial.
        string valid = FixtureDir + "/" + ValidFixture;
        DailyQaPack? vp = DailyChecks.Read(valid, out string? vErr);
        if (vp is null)
        {
            Check(false, $"{valid} se lee: {vErr}");
        }
        else
        {
            List<DailyIssue> vi = RunChecks(vp, spec!, tints!, DefaultSpec, partial: true, reproduce: true, schema: true);
            List<DailyIssue> vf = vi.Where(i => !i.Warning).ToList();
            Check(vf.Count == 0, $"{ValidFixture} con --partial: 0 fallos ({string.Join("; ", vf.Select(i => i.Check + " " + i.Board + ": " + i.Message))})");
            Check(vi.Any(i => i.Warning && i.Check == "QA-DAILY-DAYS" && i.Message.Contains("no es el pool completo", StringComparison.Ordinal)),
                $"{ValidFixture} con --partial: aviso QA-DAILY-DAYS de muestra parcial");
        }

        // Cada fixture malo falla con su id.
        foreach ((string file, bool partial, bool reproduce, string expected) in Fixtures)
        {
            string path = FixtureDir + "/" + file;
            DailyQaPack? p = DailyChecks.Read(path, out string? err);
            if (p is null)
            {
                Check(false, $"{file} se lee: {err}");
                continue;
            }

            List<DailyIssue> issues = RunChecks(p, spec!, tints!, DefaultSpec, partial, reproduce, schema: true);
            var ids = issues.Where(i => !i.Warning).Select(i => i.Check).Distinct().Order(StringComparer.Ordinal).ToList();
            DailyIssue? hit = issues.FirstOrDefault(i => !i.Warning && i.Check == expected);
            Check(hit is not null, $"{file}{(partial ? " --partial" : "")}: falla {expected} ({hit?.Message ?? "no"}; fallos: {string.Join(", ", ids)})");
        }

        // Avisos de R-Q10: el día repetido da la misma forma.
        DailyQaPack? dup = DailyChecks.Read(Path.Combine(FixtureDir, "QA-DAILY-DAYS__dia_repetido.json"), out _);
        Check(dup is not null && DailyChecks.Repetition(dup).Any(i => i.Warning && i.Check == "QA-DAILY-REPEAT-SHAPE"),
            "anti-repeticion: el dia repetido da el aviso QA-DAILY-REPEAT-SHAPE (solo aviso, R-Q10)");

        Console.WriteLine(failures == 0 ? "self-test daily: todo PASS" : $"self-test daily: {failures} FAIL");
        return failures == 0 ? 0 : 1;
    }
}
