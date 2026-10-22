// Compuerta completa de 08 §4.1 («Validación en tools/level-qa», falla el CI) y 08 §2.4 (determinismo por semilla) en un
// solo comando (hallazgo de la revisión: con tres comandos, un CI que olvide uno publicaría un pack inválido o que no se
// reproduce):
//   - QA-SCHEMA: cada pack contra schemas/af-level-1.schema.json con python tools/validate_schema.py --pack (la única copia
//     del esquema; .NET no trae un validador de JSON Schema y level-qa no usa NuGet). Sin Python o sin jsonschema, falla.
//   - QA-REPRO (03 §17 #10): se regenera la campaña con la especificación (tools/level-generator, Generation/*.cs
//     enlazados, en memoria) y cada pack tiene que salir byte a byte. D depende del pool de cada rango de 03 §11, así que
//     se regenera el rango completo de la especificación (first_level-last_level: 11-200 en campaign_v1, 201-700 en la de
//     P4) aunque se compruebe un solo pack; el rango sale de la especificación, nunca del código (P6).
//   - QA-BAND-Q (03 §7: «la banda coloca»): la banda medida (cuantil de D en el pool del rango, que solo existe
//     regenerando) acorde al rol del ciclo de D-009: comodidad, estreno, remanso y rampa ≤ 0,70; recuperación ≤ 0,10 o D
//     por debajo de la comodidad más fácil del ciclo; hito > 0,95 o par − 2K ≥ la media del pool del rango. Son las
//     mismas reglas que exige la colocación (Generation/Placement.cs). En un pack que se reproduce se usa el cuantil exacto
//     del candidato regenerado; en uno que no, el de su D publicada frente al pool regenerado (aproximado, se indica).
// --no-reproduce salta QA-REPRO y QA-BAND-Q (fixtures negativos y pruebas rápidas); lo deja escrito como aviso.
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class GateChecks
{
    public const string SchemaScript = "tools/validate_schema.py";

    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-SCHEMA", "JSON valido contra schemas/af-level-1.schema.json con python tools/validate_schema.py --pack; sin Python o sin jsonschema, falla (08 sec. 4.1; 03 sec. 17 #11)"),
        ("QA-REPRO", "la especificacion y las semillas reproducen el pack byte a byte: se regenera la campana completa en memoria (03 sec. 17 #10; 08 sec. 2.4 y 4.1)"),
        ("QA-BAND-Q", "banda medida (cuantil de D en el pool del rango) acorde al rol: comodidad, estreno, remanso y rampa <= 0,70; recuperacion <= 0,10 o D bajo la comodidad mas facil del ciclo; hito > 0,95 o par - 2K >= media del pool (03 sec. 7; c.3 reglas 3 y 5)"),
    ];

    /// <summary>QA-SCHEMA: un proceso de Python por pack (python y, si no existe, python3).</summary>
    public static IEnumerable<QaIssue> Schema(IReadOnlyList<QaPack> packs)
    {
        if (!File.Exists(SchemaScript))
        {
            yield return new QaIssue("QA-SCHEMA", "-", 0, $"no existe {SchemaScript} (ejecuta level-qa desde la raiz del repositorio)");
            yield break;
        }

        foreach (QaPack pack in packs)
        {
            (int? code, string output) = RunPython(SchemaScript, "--pack", pack.Path);
            if (code is null)
            {
                yield return new QaIssue("QA-SCHEMA", pack.Path, 0, "no se pudo ejecutar Python (python ni python3): la compuerta exige Python 3 con jsonschema");
                continue;
            }

            if (code != 0)
            {
                string detail = string.Join(" | ", output.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !l.EndsWith("paquetes validos", StringComparison.Ordinal)).Take(4));
                yield return new QaIssue("QA-SCHEMA", pack.Path, 0, AsciiText.Of($"validate_schema.py --pack sale con {code}: {detail}"));
            }
        }
    }

    private static (int? Code, string Output) RunPython(params string[] args)
    {
        foreach (string exe in new[] { "python", "python3" })
        {
            var psi = new ProcessStartInfo(exe)
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
                // No existe ese ejecutable: se prueba el siguiente.
            }
        }

        return (null, "");
    }

    /// <summary>
    /// QA-REPRO: regenera la campaña <paramref name="first"/>-<paramref name="last"/> y compara cada pack. Devuelve el resultado
    /// de la generación (o null si no se pudo generar) y los pack_id que salen idénticos.
    /// </summary>
    public static (List<QaIssue> Issues, CampaignResult? Result, HashSet<string> Reproduced) Reproduce(
        CampaignSpec spec, IReadOnlyList<QaPack> packs, int first, int last)
    {
        var issues = new List<QaIssue>();
        var reproduced = new HashSet<string>(StringComparer.Ordinal);
        if (SpecValidator.Validate(spec).Any(i => !i.Warning))
        {
            issues.Add(new QaIssue("QA-REPRO", "-", 0, "la especificacion no pasa --validate-spec: no se puede regenerar"));
            return (issues, null, reproduced);
        }

        var sw = Stopwatch.StartNew();
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"reproduccion: regenerando la campana {first}-{last} con {spec.Path.Replace('\\', '/')} (03 sec. 17 #10)"));
        CampaignResult? result = CampaignRunner.Generate(spec, first, last, msg => Console.WriteLine("  " + msg.Trim()), verbose: false);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"reproduccion: {sw.Elapsed.TotalSeconds:0.0} s"));
        if (result is null)
        {
            issues.Add(new QaIssue("QA-REPRO", "-", 0, string.Create(CultureInfo.InvariantCulture, $"la generacion de {first}-{last} no termina (algun bloque sin colocacion)")));
            return (issues, null, reproduced);
        }

        foreach (QaPack pack in packs)
        {
            GeneratedPack? gen = result.Packs.FirstOrDefault(g => g.PackId == pack.PackId);
            if (gen is null)
            {
                issues.Add(new QaIssue("QA-REPRO", pack.Path, 0, string.Create(CultureInfo.InvariantCulture,
                    $"la especificacion no genera el pack {pack.PackId} en {first}-{last}")));
                continue;
            }

            byte[] onDisk = File.ReadAllBytes(pack.Path);
            if (onDisk.AsSpan().SequenceEqual(gen.Bytes))
            {
                reproduced.Add(pack.PackId);
                continue;
            }

            (int line, int level) = FirstDifference(onDisk, gen.Bytes);
            issues.Add(new QaIssue("QA-REPRO", pack.Path, level, string.Create(CultureInfo.InvariantCulture,
                $"el pack no se reproduce: primera diferencia en la linea {line} ({onDisk.Length} bytes publicados, {gen.Bytes.Length} regenerados)")));
        }

        return (issues, result, reproduced);
    }

    /// <summary>Primera línea distinta (los packs llevan un nivel por línea) y su nivel, o 0 si es la cabecera.</summary>
    private static (int Line, int Level) FirstDifference(byte[] a, byte[] b)
    {
        string[] la = Encoding.UTF8.GetString(a).Split('\n');
        string[] lb = Encoding.UTF8.GetString(b).Split('\n');
        for (int i = 0; i < Math.Max(la.Length, lb.Length); i++)
        {
            string x = i < la.Length ? la[i] : "";
            string y = i < lb.Length ? lb[i] : "";
            if (x == y)
            {
                continue;
            }

            const string key = "\"level\":";
            string probe = x.Contains(key, StringComparison.Ordinal) ? x : y;
            int at = probe.IndexOf(key, StringComparison.Ordinal);
            int level = 0;
            if (at >= 0)
            {
                string digits = new(probe.Skip(at + key.Length).TakeWhile(char.IsDigit).ToArray());
                _ = int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out level);
            }

            return (i + 1, level);
        }

        return (0, 0);
    }

    /// <summary>QA-BAND-Q sobre los niveles presentes, con el pool de la campaña regenerada.</summary>
    public static IEnumerable<QaIssue> Bands(QaContext ctx, CampaignResult result, HashSet<string> reproduced)
    {
        var generated = result.Levels.ToDictionary(l => l.Plan.Level);
        var presentByLevel = ctx.Levels.GroupBy(e => e.Level.Level).ToDictionary(g => g.Key, g => g.First().Level);
        foreach (LevelEval e in ctx.Levels)
        {
            QaLevel lv = e.Level;
            LevelPlan? plan = e.Plan;
            if (plan is null || !generated.TryGetValue(lv.Level, out GeneratedLevel? gl) || !result.Stats.TryGetValue(plan.Range, out RangeStats? stats))
            {
                continue;
            }

            bool exact = reproduced.Contains(PackIdOf(ctx, lv));
            double q = exact ? gl.Chosen!.Quantile : stats.Quantile(lv.D);
            string approx = exact ? "" : " (aprox.: el pack no se reproduce; cuantil de la D publicada)";
            string band = Pool.BandName(Pool.BandIndex(q));
            QaIssue Issue(string msg, bool warning = false) => new("QA-BAND-Q", lv.Pack, lv.Level, AsciiText.Of(msg + approx), warning);

            switch (plan.Role)
            {
                case SlotRole.Comfort or SlotRole.Remanso when !Placement.ComfortBandOk(q):
                    yield return Issue(string.Create(CultureInfo.InvariantCulture,
                        $"{plan.RoleName}: q {q:0.000} ({band}) por encima de la banda de comodidad (<= 0,70)"));
                    break;

                case SlotRole.Recovery when !plan.Ramp:
                {
                    double? min = ComfortMin(ctx.Spec, plan, presentByLevel);
                    if (!Placement.RecoveryOk(q, lv.D, min))
                    {
                        yield return Issue(string.Create(CultureInfo.InvariantCulture,
                            $"recuperacion: q {q:0.000} ({band}) fuera de la banda de recuperacion (<= 0,10) y D {lv.D:0.00} no baja de la comodidad mas facil del ciclo ({(min is double m ? m.ToString("0.00", CultureInfo.InvariantCulture) : "sin comodidades en los paquetes")})"));
                    }
                    else if (Pool.BandIndex(q) > 0)
                    {
                        yield return Issue(string.Create(CultureInfo.InvariantCulture,
                            $"recuperacion en banda {band} (q {q:0.000}); cumple el minimo: D {lv.D:0.00} < comodidad mas facil del ciclo ({min:0.00})"), warning: true);
                    }

                    break;
                }

                case SlotRole.Milestone:
                {
                    int excess = lv.Par - (2 * lv.K);
                    if (!Placement.MilestoneOk(q, lv.Par, lv.K, stats))
                    {
                        yield return Issue(string.Create(CultureInfo.InvariantCulture,
                            $"hito: q {q:0.000} ({band}) fuera de la banda de hito (> 0,95) y par - 2K = {excess} por debajo de la media del pool del rango ({stats.ParExcess.Mean:0.0})"));
                    }
                    else if (Pool.BandIndex(q) < 3)
                    {
                        yield return Issue(string.Create(CultureInfo.InvariantCulture,
                            $"hito en banda {band} (q {q:0.000}); cumple el minimo: par - 2K = {excess} >= media del pool ({stats.ParExcess.Mean:0.0})"), warning: true);
                    }

                    break;
                }
            }
        }
    }

    /// <summary>D publicada de la comodidad más fácil del ciclo (sin estrenos) entre los niveles presentes, o null.</summary>
    private static double? ComfortMin(CampaignSpec spec, LevelPlan plan, Dictionary<int, QaLevel> present)
    {
        int start = spec.CycleFrom + (((plan.Level - spec.CycleFrom) / 20) * 20);
        List<double> ds = Enumerable.Range(start, 20)
            .Where(n => present.ContainsKey(n) && spec.Calendar.ContainsKey(n) && spec.Plan(n).IsComfortReference)
            .Select(n => present[n].D).ToList();
        return ds.Count == 0 ? null : ds.Min();
    }

    /// <summary>
    /// QA-CVD-ADJ (Q-B9-13 bis): el fallo del tablero base pasa a aviso cuando el pack se reproduce byte a byte y el generador,
    /// en ese nivel, no encontró en sus 50 permutaciones ninguna asignación sin contactos del simulador (TintResult.CvdAdjacent).
    /// </summary>
    public static void CvdUnavoidable(List<QaIssue> issues, QaContext ctx, CampaignResult result, HashSet<string> reproduced)
    {
        var generated = result.Levels.ToDictionary(l => l.Plan.Level);
        for (int i = 0; i < issues.Count; i++)
        {
            QaIssue issue = issues[i];
            if (issue.Check != "QA-CVD-ADJ" || issue.Warning)
            {
                continue;
            }

            QaPack? pack = ctx.Packs.FirstOrDefault(p => p.Path == issue.Pack);
            if (pack is not null && reproduced.Contains(pack.PackId)
                && generated.TryGetValue(issue.Level, out GeneratedLevel? gl) && gl.Chosen is { Tints.CvdAdjacent: > 0 })
            {
                issues[i] = issue with { Message = issue.Message + "; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)", Warning = true };
            }
        }
    }

    private static string PackIdOf(QaContext ctx, QaLevel lv) => ctx.Packs.First(p => p.Path == lv.Pack).PackId;
}
