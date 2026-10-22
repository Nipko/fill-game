// level-qa --daily (R4 del encargo docs/dev/traspaso/spark-reto-del-rio.md): QA del pool del Reto del río.
//   dotnet run --project tools/level-qa -c Release -- --daily <daily_pool_v1.json> [--spec <daily_v1.json>] [--partial] [--no-reproduce]
//   dotnet run --project tools/level-qa -- --self-test daily
// Program.cs despacha aquí antes de su --self-test genérico (que rechazaría "daily"; el --self-test sin nombre también
// ejecuta el de aquí). Las comprobaciones, en Daily/DailyChecks.cs (QA-DAILY-*).
//   - --spec: por defecto tools/level-generator/specs/daily_v1.json; los tintes y los pares de riesgo, de su tints_from.
//   - --partial: valida una muestra (p. ej. tools/out/daily_sample): los días que faltan no fallan, pero sale siempre el aviso
//     QA-DAILY-DAYS «muestra parcial … no es el pool completo». El tamaño (QA-DAILY-SIZE) se mira siempre. Sin --partial, el
//     pack tiene que traer las 3 bandas × 729 días (R-Q2b).
//   - --no-reproduce: salta QA-DAILY-REPRO (aviso); nunca en el CI.
//   - Reproducción (QA-DAILY-REPRO): regenera en memoria, sin proceso hijo, las bandas y los días del pack con el código de
//     generación del generador, enlazado en level-qa.csproj (tools/level-generator/Daily/*.cs salvo DailyCommand*.cs:
//     DailySpec, DailyIds, DailyRunner, DailyTints, DailyPackWriter y DailyReport), y compara cada tablero entero (igualdad
//     profunda del JSON: 03 §17 #10 pide el mismo tablero, no solo el mismo hash).
// Imprime ASCII. Códigos: 0 = sin fallos (los avisos no fallan); 1 = algún fallo; 2 = uso, especificación o pack ilegibles.
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Daily;
using AmazonFill.Tools.LevelGenerator.Generation;

namespace AmazonFill.Tools.LevelQa.Daily;

internal static class DailyQaCommand
{
    public const string DefaultSpec = "tools/level-generator/specs/daily_v1.json";
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
        List<DailyIssue> issues = RunChecks(read, spec!, tints!, partial, reproduce, schema: true);
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

    public static List<DailyIssue> RunChecks(DailyQaPack pack, DailySpec spec, CampaignSpec tints, bool partial, bool reproduce, bool schema)
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
        issues.AddRange(DailyChecks.Size(pack.Bytes));
        issues.AddRange(DailyChecks.Repetition(pack));
        if (reproduce)
        {
            var cvdUnavoidable = new HashSet<string>(StringComparer.Ordinal);
            issues.AddRange(Reproduce(pack, spec, tints, cvdUnavoidable));
            // QA-DAILY-CVD-ADJ (Q-B9-13 bis): aviso, no fallo, si el tablero se reproduce y el generador no encontró en sus 50
            // permutaciones ninguna asignación sin contactos del simulador.
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i] is { Check: "QA-DAILY-CVD-ADJ", Warning: false } c && cvdUnavoidable.Contains(c.Board))
                {
                    issues[i] = c with { Message = c.Message + "; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)", Warning = true };
                }
            }
        }
        else
        {
            issues.Add(new DailyIssue("QA-DAILY-REPRO", "-", "--no-reproduce: no se comprueba que el generador reproduzca los tableros (03 sec. 17 #10)", Warning: true));
        }

        return issues;
    }

    /// <summary>
    /// QA-DAILY-REPRO: regenera en memoria, con el DailyRunner y el DailyPackWriter del generador (enlazados), las bandas y los
    /// días del pack, y compara cada tablero entero con el regenerado.
    /// </summary>
    private static IEnumerable<DailyIssue> Reproduce(DailyQaPack pack, DailySpec spec, CampaignSpec tints, HashSet<string> cvdUnavoidable)
    {
        List<DailyQaLevel> slots = pack.Levels.Where(l => l.Band is int b && l.Day is int d && DailyIds.Problem(b, d) is null).ToList();
        if (slots.Count == 0)
        {
            return [new DailyIssue("QA-DAILY-REPRO", "-", "ningun tablero con slot valido que regenerar")];
        }

        List<int> bands = slots.Select(l => l.Band!.Value).Distinct().Order().ToList();
        List<int> days = slots.Select(l => l.Day!.Value).Distinct().Order().ToList();
        var dailyTints = new DailyTints(tints, spec);
        DailyRun run = DailyRunner.Run(spec, dailyTints, bands, days, Environment.ProcessorCount);
        var issues = new List<DailyIssue>();

        // Lo que haría fallar al generador (y no escribir el pack): un día sin tablero en la ventana (R-Q11 (a)) o un tablero
        // que no cumple lo que promete el pack.
        foreach (string e in run.Errors)
        {
            issues.Add(new DailyIssue("QA-DAILY-REPRO", "-", "el generador falla: " + e));
        }

        foreach (DailyBoard b in run.Boards)
        {
            if (DailyPackWriter.CheckBoard(b) is string problem)
            {
                issues.Add(new DailyIssue("QA-DAILY-REPRO", b.Id, "el generador rechaza su tablero: " + problem));
            }
        }

        if (run.Boards.Count == 0)
        {
            return issues.Count > 0 ? issues : [new DailyIssue("QA-DAILY-REPRO", "-", "el generador no da ningun tablero")];
        }

        // El mismo texto que escribe el generador (DailyCommand: PackWriter.Bytes de DailyPackWriter.Write), leído de vuelta.
        JsonNode regen = JsonNode.Parse(PackWriter.Bytes(DailyPackWriter.Write(spec, dailyTints, run.Boards)))!;
        var byId = regen["levels"]!.AsArray().Where(x => x is not null).ToDictionary(x => x!["id"]!.GetValue<string>(), x => x!.AsObject(), StringComparer.Ordinal);
        foreach (DailyQaLevel lv in slots)
        {
            string id = DailyIds.Id(lv.Band!.Value, lv.Day!.Value);
            if (!byId.TryGetValue(id, out JsonObject? r))
            {
                issues.Add(new DailyIssue("QA-DAILY-REPRO", lv.Name, $"el generador no da {id}"));
                continue;
            }

            if (JsonNode.DeepEquals(lv.Json, r))
            {
                if (run.Boards.FirstOrDefault(x => x.Id == id) is { Palette.CvdAdjacent: > 0 })
                {
                    cvdUnavoidable.Add(lv.Name);
                }
            }
            else
            {
                string[] diff = lv.Json.Select(kv => kv.Key).Union(r.Select(kv => kv.Key), StringComparer.Ordinal)
                    .Where(k => !JsonNode.DeepEquals(lv.Json[k], r[k])).ToArray();
                issues.Add(new DailyIssue("QA-DAILY-REPRO", lv.Name, $"la semilla no reproduce el tablero: difieren {string.Join(", ", diff)}"));
            }
        }

        return issues;
    }

    /// <summary>
    /// Fixtures de tools/level-qa/fixtures/daily/: archivo, opciones, la comprobación que tiene que fallar y las únicas otras
    /// que pueden fallar a la vez (las que el cambio del tablero arrastra: otra forma, otro bot o la paleta de B3).
    /// </summary>
    private static readonly (string File, bool Partial, bool Reproduce, string Expected, string[] Tolerated)[] Fixtures =
    [
        ("QA-DAILY-SCHEMA__clave_desconocida.json", true, false, "QA-DAILY-SCHEMA", []),
        ("QA-DAILY-HEADER__otro_pack_id.json", true, false, "QA-DAILY-HEADER", []),
        ("QA-DAILY-STRUCT__cuia_con_tres_dosis.json", true, false, "QA-DAILY-STRUCT", ["QA-DAILY-SHAPE"]),
        ("QA-DAILY-SLOT__id_de_otro_dia.json", true, false, "QA-DAILY-SLOT", []),
        ("QA-DAILY-DAYS__dia_repetido.json", true, false, "QA-DAILY-DAYS", []),
        ("QA-DAILY-SHAPE__k_de_otro_dia.json", true, false, "QA-DAILY-SHAPE", ["QA-DAILY-BOT"]),
        ("QA-DAILY-CUIAS-14__quince_cuias.json", true, false, "QA-DAILY-CUIAS-14", ["QA-DAILY-BOT", "QA-DAILY-RISK", "QA-DAILY-SHAPE"]),
        ("QA-DAILY-PALETTE__tinte_antes_de_su_debut.json", true, false, "QA-DAILY-PALETTE", []),
        ("QA-DAILY-RISK__b2_tres_pares.json", true, false, "QA-DAILY-RISK", ["QA-DAILY-CVD-ADJ"]),
        ("QA-DAILY-MECH__murky_con_ocultos.json", true, false, "QA-DAILY-MECH", []),
        ("QA-DAILY-FIELDS__con_difficulty.json", true, false, "QA-DAILY-FIELDS", []),
        ("QA-DAILY-RELIEF__con_variante.json", true, false, "QA-DAILY-RELIEF", []),
        ("QA-DAILY-WATER__mundo_de_otro_dia.json", true, false, "QA-DAILY-WATER", []),
        ("QA-DAILY-ASTAR__irresoluble.json", true, false, "QA-DAILY-ASTAR", ["QA-DAILY-BOT", "QA-DAILY-CVD-ADJ"]),
        ("QA-DAILY-PAR__casi_resuelto.json", true, false, "QA-DAILY-PAR", ["QA-DAILY-BOT"]),
        ("QA-DAILY-BOT__fuera_de_ventana.json", true, false, "QA-DAILY-BOT", ["QA-DAILY-CVD-ADJ"]),
        ("QA-DAILY-CVD-ADJ__par_del_simulador_junto.json", true, false, "QA-DAILY-CVD-ADJ", ["QA-DAILY-BOT"]),
        ("QA-DAILY-REPRO__otros_candidatos.json", true, true, "QA-DAILY-REPRO", []),
        ("QA-DAILY-REPRO__cuias_permutadas.json", true, true, "QA-DAILY-REPRO", ["QA-DAILY-BOT"]),
        ("daily_valido.json", false, false, "QA-DAILY-DAYS", []),
    ];

    public const string ValidFixture = "daily_valido.json";

    /// <summary>Copia de un tablero leído con otro día (y su índice), para las pruebas de la anti-repetición en memoria.</summary>
    private static DailyQaLevel DailyChecksTestLevel(DailyQaLevel lv, int day, int index) => new()
    {
        Index = index,
        Json = lv.Json,
        Id = DailyIds.Id(lv.Band!.Value, day),
        Seed = DailyIds.Seed(lv.Band!.Value, day),
        Band = lv.Band,
        Day = day,
        World = lv.World,
        WaterBase = lv.WaterBase,
        Palette = lv.Palette,
        Units = lv.Units,
        Caps = lv.Caps,
        Cuias = lv.Cuias,
        RiskPairs = lv.RiskPairs,
    };

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

        Check(DailyChecks.Size(DailyChecks.MaxBytes).Any(i => !i.Warning) && !DailyChecks.Size(DailyChecks.MaxBytes - 1).Any(),
            "QA-DAILY-SIZE: 2 MB falla y 2 MB - 1 pasa (se mira siempre, tambien con --partial)");

        // El fixture válido: sin fallos como muestra parcial (con reproducción y esquema) y con el aviso de muestra parcial.
        string valid = FixtureDir + "/" + ValidFixture;
        DailyQaPack? vp = DailyChecks.Read(valid, out string? vErr);
        if (vp is null)
        {
            Check(false, $"{valid} se lee: {vErr}");
        }
        else
        {
            List<DailyIssue> vi = RunChecks(vp, spec!, tints!, partial: true, reproduce: true, schema: true);
            List<DailyIssue> vf = vi.Where(i => !i.Warning).ToList();
            Check(vf.Count == 0, $"{ValidFixture} con --partial: 0 fallos ({string.Join("; ", vf.Select(i => i.Check + " " + i.Board + ": " + i.Message))})");
            Check(vi.Any(i => i.Warning && i.Check == "QA-DAILY-DAYS" && i.Message.Contains("no es el pool completo", StringComparison.Ordinal)),
                $"{ValidFixture} con --partial: aviso QA-DAILY-DAYS de muestra parcial");

            // R-Q1b (a): B3 con max_risk_pairs libre; el fixture lleva d3_n0001 con mas pares que el antiguo tope de 2 y no falla.
            Check(vp.Levels.Any(l => l.Band == 3 && l.RiskPairs > 2) && vi.All(i => i.Check != "QA-DAILY-RISK"),
                $"QA-DAILY-RISK: B3 libre no falla con {vp.Levels.Where(l => l.Band == 3).Select(l => l.RiskPairs).FirstOrDefault()} pares de riesgo (R-Q1b (a))");

            // R-Q10: d1_n0000 tiene 1 trampa temprana con etiqueta normal (tope 0): aviso, nunca fallo.
            DailyIssue? early = vi.FirstOrDefault(i => i.Check == "QA-DAILY-EARLY-TRAPS" && i.Board == "d1_n0000");
            Check(early is { Warning: true } && vi.All(i => i.Check != "QA-DAILY-EARLY-TRAPS" || i.Warning) && vf.Count == 0,
                $"QA-DAILY-EARLY-TRAPS es aviso y no fallo: {early?.Message ?? "no sale"}");

            // R-Q10: el tablero d1_n0001 del fixture casi igual (las dos dosis de arriba de su ultima cuia llena intercambiadas:
            // los colores ya salieron antes, asi que el renombrado de BoardShape.Hamming no cambia) en otro dia impar cercano de
            // B1 (mismo K). Hamming < 0,25 da el aviso QA-DAILY-REPEAT-HAMMING y ningun fallo. El bot depende de la semilla del
            // dia, asi que se usa el primer dia impar 3-21 cuya semilla deja el bot en la ventana (determinista).
            const int baseIndex = 1;
            int baseDay = vp.Levels[baseIndex].Day!.Value;
            JsonObject near = vp.Levels[baseIndex].Json.DeepClone().AsObject();
            JsonArray cuias = near["cuias"]!.AsArray();
            int last = Enumerable.Range(0, cuias.Count).Last(i => cuias[i]!.AsArray().Count == 4
                && cuias[i]![3]!.GetValue<int>() != cuias[i]![2]!.GetValue<int>());
            JsonArray top = cuias[last]!.AsArray();
            (top[2], top[3]) = (top[3]!.DeepClone(), top[2]!.DeepClone());
            DailyIssue? ham = null;
            List<DailyIssue> hf = [];
            int usedDay = -1;
            for (int day = baseDay + 2; day <= baseDay + AmazonFill.Tools.LevelGenerator.Campaign.BoardShape.HammingWindow && ham is null; day += 2)
            {
                near["slot"]!["day"] = day;
                near["id"] = DailyIds.Id(1, day);
                near["seed"] = DailyIds.Seed(1, day);
                near["world"] = spec!.WorldOfDay(day).World;
                near["water_base"] = spec.WorldOfDay(day).WaterBase;
                string tmp = Path.Combine(Path.GetTempPath(), "af-daily-qa-hamming-" + Guid.NewGuid().ToString("N") + ".json");
                JsonObject packJson = JsonNode.Parse(File.ReadAllText(valid))!.AsObject();
                packJson["levels"] = new JsonArray(vp.Levels[0].Json.DeepClone(), vp.Levels[baseIndex].Json.DeepClone(), near.DeepClone());
                File.WriteAllText(tmp, packJson.ToJsonString());
                try
                {
                    List<DailyIssue> hi = RunChecks(DailyChecks.Read(tmp, out _)!, spec, tints!, partial: true, reproduce: false, schema: false);
                    hf = hi.Where(i => !i.Warning).ToList();
                    if (hf.Count == 0)
                    {
                        ham = hi.FirstOrDefault(i => i.Check == "QA-DAILY-REPEAT-HAMMING");
                        usedDay = day;
                        break;
                    }
                }
                finally
                {
                    File.Delete(tmp);
                }
            }

            Check(ham is { Warning: true } && ham.Board == DailyIds.Id(1, usedDay) && hf.Count == 0,
                string.Create(CultureInfo.InvariantCulture, $"QA-DAILY-REPEAT-HAMMING es aviso y no fallo con dos dias casi iguales (dias {baseDay} y {usedDay}): {ham?.Message ?? "no sale"} (fallos: {string.Join("; ", hf.Select(i => i.Check + " " + i.Message))})"));

            // Vuelta del ciclo: el ultimo dia (728) y el dia 0 son vecinos (dia mod 729).
            var wp = new DailyQaPack
            {
                Path = "memoria",
                Bytes = 0,
                Json = [],
                Levels = [DailyChecksTestLevel(vp.Levels[0], vp.Levels[0].Day!.Value, 0), DailyChecksTestLevel(vp.Levels[0], DailyIds.Days - 1, 1)],
            };
            Check(DailyChecks.Repetition(wp).Any(i => i.Warning && i.Check == "QA-DAILY-REPEAT-SHAPE" && i.Board == "d1_n0000"),
                "anti-repeticion con la vuelta del ciclo: el dia 0 se compara con el 728 (solo aviso)");
        }

        // Cada fixture malo falla con su id.
        foreach ((string file, bool partial, bool reproduce, string expected, string[] tolerated) in Fixtures)
        {
            string path = FixtureDir + "/" + file;
            DailyQaPack? p = DailyChecks.Read(path, out string? err);
            if (p is null)
            {
                Check(false, $"{file} se lee: {err}");
                continue;
            }

            List<DailyIssue> issues = RunChecks(p, spec!, tints!, partial, reproduce, schema: true);
            var ids = issues.Where(i => !i.Warning).Select(i => i.Check).Distinct().Order(StringComparer.Ordinal).ToList();
            DailyIssue? hit = issues.FirstOrDefault(i => !i.Warning && i.Check == expected);
            string[] extra = ids.Where(i => i != expected && !tolerated.Contains(i)).ToArray();
            Check(hit is not null && extra.Length == 0, $"{file}{(partial ? " --partial" : "")}: falla {expected} ({hit?.Message ?? "no"}; fallos: {string.Join(", ", ids)}{(extra.Length > 0 ? "; NO tolerados: " + string.Join(", ", extra) : "")})");
        }

        // Avisos de R-Q10: el día repetido da la misma forma.
        DailyQaPack? dup = DailyChecks.Read(Path.Combine(FixtureDir, "QA-DAILY-DAYS__dia_repetido.json"), out _);
        Check(dup is not null && DailyChecks.Repetition(dup).Any(i => i.Warning && i.Check == "QA-DAILY-REPEAT-SHAPE"),
            "anti-repeticion: el dia repetido da el aviso QA-DAILY-REPEAT-SHAPE (solo aviso, R-Q10)");

        Console.WriteLine(failures == 0 ? "self-test daily: todo PASS" : $"self-test daily: {failures} FAIL");
        return failures == 0 ? 0 : 1;
    }
}
