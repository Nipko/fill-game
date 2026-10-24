// level-qa --reserve: QA de los packs de reserva (D-033, añadido «la reserva del 20 %», Q-B11-7 (a); 03 §17; 08 §4.1). Cada
// pack de reserva (kind reserve, pack_schema 4) se comprueba con su pack de campaña publicado (el que sustituye) y con la
// especificación de la campaña (la misma de --spec del generador):
//   - las comprobaciones base de la campaña sobre cada tablero (estructura, U_c, par recalculado y en el rango, par >= 2K,
//     paleta, K, pares de riesgo, bot ×40 con su semilla, etiqueta coherente con bot_fail, contrato 6, trampas tempranas),
//     el alivio (ReliefChecks), Aguas turbias y la tinaja (que no lleva) y el tamaño;
//   - QA-RES-*: cabecera, slot e ids, sin la mecánica (ni ninguna otra), ventana y etiqueta del slot, tope del 20 % y
//     reproducibilidad (regenera el pack en memoria con Generation/ReserveRunner.cs, el código del generador, y compara byte
//     a byte);
//   - QA-SCHEMA con python tools/validate_schema.py --pack.
// Todo lo que se imprime es ASCII (.claude/rules/ci.md).
using System.Globalization;
using System.Text;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;
using AmazonFill.Tools.LevelQa.Checks;

namespace AmazonFill.Tools.LevelQa.Reserve;

internal static class ReserveQaCommand
{
    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-RES-HEADER", "pack_schema 4, kind reserve, pack_id reserva_{mm}_v1 del mundo del pack de campana (contrato 21)"),
        ("QA-RES-SLOT", "replaces = el id de campana del mismo level y mundo, presente en el pack de campana; id = replaces + _r1; seed = la del slot + :r1"),
        ("QA-RES-MECH", "el sustituido usa una mecanica con kill switch (mech_enabled_*, 08 sec. 9); el de reserva no lleva ninguna mecanica ni Barro (tampoco su alivio), ni ocultas ni intro_mechanic [P]"),
        ("QA-RES-WINDOW", "bot_fail en la ventana del slot, etiqueta permitida en el slot y band del slot (03 sec. 12; D-009)"),
        ("QA-RES-COUNT", "como mucho el 20 % de los niveles del mundo; aviso si hay menos que min(tope, niveles con mecanica)"),
        ("QA-RES-D", "sin difficulty.D (la reserva no entra en el pool del rango, 03 sec. 7)"),
        ("QA-RES-REPRO", "el pack se regenera byte a byte desde el pack de campana y la especificacion (03 sec. 17 #10)"),
    ];

    public static int Run(string[] args, Func<string, int> usageError)
    {
        var reserves = new List<string>();
        var campaigns = new List<string>();
        string? specPath = null;
        string? variant = null;
        string? report = null;
        bool reproduce = true;
        List<string>? target = null;
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a == "--reserve")
            {
                target = reserves;
            }
            else if (a == "--campaign")
            {
                target = campaigns;
            }
            else if (a == "--spec" && i + 1 < args.Length && specPath is null)
            {
                specPath = args[++i];
                target = null;
            }
            else if (a == "--variant" && i + 1 < args.Length && variant is null)
            {
                variant = args[++i];
                target = null;
            }
            else if (a == "--report" && i + 1 < args.Length && report is null)
            {
                report = args[++i];
                target = null;
            }
            else if (a == "--no-reproduce" && reproduce)
            {
                reproduce = false;
                target = null;
            }
            else if (!a.StartsWith('-') && target is not null)
            {
                target.Add(a);
            }
            else
            {
                return usageError($"--reserve: opcion no valida, repetida o sin valor: {a}");
            }
        }

        if (reserves.Count == 0 || campaigns.Count == 0 || specPath is null)
        {
            return usageError("--reserve necesita al menos un pack de reserva, --campaign con sus packs de campana y --spec.");
        }

        CampaignSpec spec;
        try
        {
            spec = CampaignSpec.Load(specPath, variant);
        }
        catch (CampaignSpecException e)
        {
            Console.Error.WriteLine(AsciiText.Of($"level-qa: {e.Message}"));
            return 2;
        }

        List<QaPack> packs;
        List<QaIssue> issues;
        int boards;
        try
        {
            (packs, issues, boards) = Collect(spec, reserves, campaigns, reproduce);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException
            or PackFormatException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            Console.Error.WriteLine(AsciiText.Of($"level-qa: no se puede leer un pack: {e.Message}"));
            return 2;
        }

        List<QaIssue> ordered = issues.OrderBy(i => i.Warning).ThenBy(i => i.Level).ThenBy(i => i.Check, StringComparer.Ordinal).ToList();
        foreach (QaPack pack in packs)
        {
            int fails = issues.Count(i => !i.Warning && i.Pack == pack.Path);
            Console.WriteLine(AsciiText.Of(string.Create(CultureInfo.InvariantCulture,
                $"{pack.Path.Replace('\\', '/')}: pack_schema {pack.PackSchema}, kind {pack.Kind}, {pack.Levels.Count} niveles, {fails} fallo(s)")));
        }

        foreach (QaIssue i in ordered)
        {
            Console.WriteLine(AsciiText.Of(string.Create(CultureInfo.InvariantCulture,
                $"{(i.Warning ? "AVISO" : "FAIL ")} [{i.Check}] nivel {i.Level}: {i.Message}")));
        }

        int failures = issues.Count(i => !i.Warning);
        List<(string Id, string Rule)> allIds = AllIds();
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{boards} tableros de reserva, {allIds.Count} comprobaciones: {failures} fallo(s), {issues.Count(i => i.Warning)} aviso(s)"));
        if (report is not null)
        {
            WriteReport(report, args, boards, failures, ordered, allIds, issues);
        }

        return failures == 0 ? 0 : 1;
    }

    /// <summary>Ids de todas las comprobaciones de --reserve, para el resumen y el informe.</summary>
    public static List<(string Id, string Rule)> AllIds() =>
        BaseChecks.Ids.Concat(ReliefChecks.Ids).Concat(MurkyChecks.Ids).Concat(TinajaChecks.Ids).Concat(Ids)
            .Concat(GateChecks.Ids.Where(g => g.Id == "QA-SCHEMA")).ToList();

    /// <summary>
    /// Todas las comprobaciones de --reserve sobre <paramref name="reserves"/> con sus packs de campaña
    /// <paramref name="campaigns"/> (emparejados por mundo): los packs leídos, las incidencias y el número de tableros. Lanza si
    /// un pack no se puede leer.
    /// </summary>
    public static (List<QaPack> Packs, List<QaIssue> Issues, int Boards) Collect(
        CampaignSpec spec, IReadOnlyList<string> reserves, IReadOnlyList<string> campaigns, bool reproduce)
    {
        var packs = new List<QaPack>();
        var campaignByWorld = new Dictionary<int, (string Path, QaPack Pack, List<ReserveSource> Sources)>();
        foreach (string path in reserves)
        {
            packs.Add(PackReader.Read(path));
        }

        foreach (string path in campaigns)
        {
            QaPack c = PackReader.Read(path);
            campaignByWorld[c.World] = (path, c, ReserveRunner.ReadCampaign(path).Levels);
        }

        List<LevelEval> evals = Evaluator.Evaluate(spec, packs);
        var ctx = new QaContext { Spec = spec, Packs = packs, Levels = evals };
        var issues = new List<QaIssue>();
        var cvdUnavoidable = new HashSet<(string Pack, int Level)>();
        foreach (QaPack pack in packs)
        {
            issues.AddRange(BaseChecks.RunPack(pack));
            if (!campaignByWorld.TryGetValue(pack.World, out var campaign))
            {
                issues.Add(new QaIssue("QA-RES-HEADER", pack.Path, 0, string.Create(CultureInfo.InvariantCulture, $"no se ha dado el pack de campana del mundo {pack.World} (--campaign)")));
                continue;
            }

            issues.AddRange(Header(spec, pack));
            issues.AddRange(Count(spec, pack, campaign.Sources));
            if (reproduce)
            {
                issues.AddRange(Reproduce(spec, pack, campaign.Path, cvdUnavoidable));
            }
        }

        foreach (LevelEval e in evals)
        {
            issues.AddRange(BaseChecks.Run(ctx, e));
            issues.AddRange(MurkyChecks.Run(e));
            issues.AddRange(TinajaChecks.Run(e));
            issues.AddRange(ReliefChecks.Run(e));
            QaPack pack = packs.First(p => p.Path == e.Level.Pack);
            if (campaignByWorld.TryGetValue(pack.World, out var campaign))
            {
                issues.AddRange(Level(spec, e.Level, campaign.Pack, campaign.Sources));
            }
        }

        // QA-CVD-ADJ (Q-B9-13 bis): aviso, no fallo, si el pack se reproduce y el generador no encontró en sus 50 permutaciones
        // ninguna asignación sin contactos del simulador.
        for (int i = 0; i < issues.Count; i++)
        {
            if (issues[i] is { Check: "QA-CVD-ADJ", Warning: false } c && cvdUnavoidable.Contains((c.Pack, c.Level)))
            {
                issues[i] = c with { Message = c.Message + "; ninguna de las 50 permutaciones lo evita midiendo igual (reproducido)", Warning = true };
            }
        }

        issues.AddRange(GateChecks.Schema(packs));
        if (!reproduce)
        {
            issues.Add(new QaIssue("QA-RES-REPRO", "-", 0, "--no-reproduce: no se comprueba la reproducibilidad (03 sec. 17 #10)", Warning: true));
        }

        foreach (int level in evals.GroupBy(e => e.Level.Level).Where(g => g.Count() > 1).Select(g => g.Key))
        {
            issues.Add(new QaIssue("QA-STRUCT", "-", level, "slot repetido en los packs de reserva"));
        }

        return (packs, issues, evals.Count);
    }

    /// <summary>
    /// --self-test reserve: el pack de reserva publicado del mundo 3 (Assets/_Project/Data/Levels/reserva_03_v1.json) pasa sin
    /// fallos con --no-reproduce, y cada copia estropeada (en un archivo temporal) falla con su id: pack_schema 3, kind
    /// campaign, pack_id de otro mundo, replaces de otro slot, id y seed sin el sufijo, la Hoja en attrs, el Barro en cuias,
    /// intro_mechanic, D publicado, bot_fail fuera de la ventana (y el bot que no se recalcula igual) y un slot repetido que
    /// pasa el tope del 20 %. Desde la raíz del repositorio. Códigos: 0 = todo PASS; 1 = algún FAIL; 2 = faltan archivos.
    /// </summary>
    public static int SelfTest()
    {
        const string Reserve = "Assets/_Project/Data/Levels/reserva_03_v1.json";
        const string Campaign = "Assets/_Project/Data/Levels/mundo_03_v1.json";
        const string Spec = "tools/level-generator/specs/campaign_v1.json";
        if (!File.Exists(Reserve) || !File.Exists(Campaign) || !File.Exists(Spec))
        {
            Console.Error.WriteLine("level-qa: --self-test reserve se ejecuta desde la raiz del repositorio (faltan los packs del mundo 3)");
            return 2;
        }

        CampaignSpec spec = CampaignSpec.Load(Spec, null);
        string text = File.ReadAllText(Reserve, Encoding.UTF8);
        string[] lines = text.Split('\n');
        string first = lines[1]; // el primer nivel (el 43, estreno de la Hoja): la cabecera va en la línea 0
        int fails = 0;

        void Expect(string name, string mutated, string id, bool shouldFail = true, bool reproduce = false)
        {
            string path = Path.Combine(Path.GetTempPath(), "af_reserve_selftest_" + name + ".json");
            File.WriteAllText(path, mutated, new UTF8Encoding(false));
            try
            {
                (List<QaPack> _, List<QaIssue> issues, int _) = Collect(spec, [path], [Campaign], reproduce);
                List<QaIssue> failures = issues.Where(i => !i.Warning).ToList();
                bool ok = shouldFail ? failures.Any(i => i.Check == id) : failures.Count == 0;
                fails += ok ? 0 : 1;
                string got = failures.Count == 0 ? "ningun fallo" : string.Join(", ", failures.Select(i => i.Check).Distinct());
                Console.WriteLine(AsciiText.Of($"{(ok ? "PASS" : "FAIL")}  reserve/{name}: {(shouldFail ? "espera " + id : "espera 0 fallos")}; da {got}"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        string Line1(string from, string to)
        {
            if (!first.Contains(from, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("el nivel 43 de la reserva no contiene " + from);
            }

            return text.Replace(first, first.Replace(from, to, StringComparison.Ordinal), StringComparison.Ordinal);
        }

        // El pack publicado tiene contactos del simulador de daltonismo que el generador no pudo evitar (Q-B9-13 bis): sin la
        // reproducción, QA-CVD-ADJ falla; con ella, la reproducción lo demuestra y queda en aviso.
        Expect("valido", text, "", shouldFail: false, reproduce: true);
        Expect("cvd_sin_reproduccion", text, "QA-CVD-ADJ");
        Expect("pack_schema_3", text.Replace("\"pack_schema\":4", "\"pack_schema\":3", StringComparison.Ordinal), "QA-RES-HEADER");
        Expect("kind_campaign", text.Replace("\"kind\":\"reserve\"", "\"kind\":\"campaign\"", StringComparison.Ordinal), "QA-RES-HEADER");
        Expect("pack_id_otro_mundo", text.Replace("\"pack_id\":\"reserva_03_v1\"", "\"pack_id\":\"reserva_04_v1\"", StringComparison.Ordinal), "QA-RES-HEADER");
        Expect("replaces_otro_slot", Line1("\"replaces\":\"m03_v1_n0043\"", "\"replaces\":\"m03_v1_n0044\""), "QA-RES-SLOT");
        Expect("id_sin_sufijo", Line1("\"id\":\"m03_v1_n0043_r1\"", "\"id\":\"m03_v1_n0043\""), "QA-RES-SLOT");
        Expect("seed_sin_sufijo", Line1("\"seed\":\"amazonfill:v1:m03:v1:n0043:r1\"", "\"seed\":\"amazonfill:v1:m03:v1:n0043\""), "QA-RES-SLOT");
        Expect("con_hoja", Line1("\"attrs\":[null,", "\"attrs\":[{\"type\":\"hoja\",\"key\":7},"), "QA-RES-MECH");
        Expect("con_barro", Line1("\"cuias\":[[],[7,7,7,8]", "\"cuias\":[[],[7,7,7,15]"), "QA-RES-MECH");
        Expect("intro", Line1("\"intro_mechanic\":null,\"skippable\":true", "\"intro_mechanic\":\"hoja\",\"skippable\":false"), "QA-RES-MECH");
        Expect("con_D", Line1("\"complexity\":", "\"D\":0.00,\"complexity\":"), "QA-RES-D");
        Expect("fuera_de_ventana", Line1("\"bot_fail\":0.000,\"par\":14", "\"bot_fail\":0.250,\"par\":14"), "QA-RES-WINDOW");
        // Nueve tableros en el mundo 3 (tope 8): el 43 repetido como 41, que además no usa mecánica (QA-RES-MECH) y es otro slot.
        string extra = first.Replace("_n0043", "_n0041", StringComparison.Ordinal).Replace(":n0043", ":n0041", StringComparison.Ordinal)
            .Replace("\"level\":43", "\"level\":41", StringComparison.Ordinal);
        Expect("pasa_el_20", text.Replace(first + "\n", extra + "\n" + first + "\n", StringComparison.Ordinal), "QA-RES-COUNT");

        Console.WriteLine(fails == 0 ? "--self-test reserve: todo PASS" : string.Create(CultureInfo.InvariantCulture, $"--self-test reserve: {fails} FAIL"));
        return fails == 0 ? 0 : 1;
    }

    private static IEnumerable<QaIssue> Header(CampaignSpec spec, QaPack pack)
    {
        QaIssue Fail(string msg) => new("QA-RES-HEADER", pack.Path, 0, msg);
        if (pack.PackSchema != ReserveRunner.PackSchema)
        {
            yield return Fail(string.Create(CultureInfo.InvariantCulture, $"pack_schema {pack.PackSchema}; la reserva va con el {ReserveRunner.PackSchema}"));
        }

        if (pack.Kind != "reserve")
        {
            yield return Fail($"kind {pack.Kind}; se espera reserve");
        }

        WorldInfo? world = spec.Worlds.FirstOrDefault(w => w.World == pack.World);
        if (world is null)
        {
            yield return Fail(string.Create(CultureInfo.InvariantCulture, $"el mundo {pack.World} no esta en la especificacion {spec.SpecId}"));
        }
        else if (pack.PackId != ReserveRunner.PackId(world, spec.Lap))
        {
            yield return Fail($"pack_id {pack.PackId}; se espera {ReserveRunner.PackId(world, spec.Lap)}");
        }
    }

    private static IEnumerable<QaIssue> Count(CampaignSpec spec, QaPack pack, List<ReserveSource> sources)
    {
        WorldInfo? world = spec.Worlds.FirstOrDefault(w => w.World == pack.World);
        if (world is null)
        {
            yield break;
        }

        int cap = ReserveRunner.Cap(world);
        int withMechanics = sources.Count(s => s.Mask != 0);
        if (pack.Levels.Count > cap)
        {
            yield return new QaIssue("QA-RES-COUNT", pack.Path, 0, string.Create(CultureInfo.InvariantCulture,
                $"{pack.Levels.Count} tableros; el tope es el 20 % de los {world.Last - world.First + 1} niveles del mundo, {cap}"));
        }
        else if (pack.Levels.Count < Math.Min(cap, withMechanics))
        {
            yield return new QaIssue("QA-RES-COUNT", pack.Path, 0, string.Create(CultureInfo.InvariantCulture,
                $"{pack.Levels.Count} tableros de {Math.Min(cap, withMechanics)} posibles (tope {cap}, {withMechanics} niveles con mecanica): algun slot se salto"), Warning: true);
        }
    }

    private static IEnumerable<QaIssue> Reproduce(CampaignSpec spec, QaPack pack, string campaignPath, HashSet<(string Pack, int Level)> cvdUnavoidable)
    {
        ReserveResult? r = ReserveRunner.Generate(spec, campaignPath, _ => { }, out string error);
        byte[] onDisk = File.ReadAllBytes(pack.Path);
        if (r is null)
        {
            yield return new QaIssue("QA-RES-REPRO", pack.Path, 0, AsciiText.Of("no se puede regenerar: " + error));
        }
        else if (!onDisk.AsSpan().SequenceEqual(r.Bytes))
        {
            yield return new QaIssue("QA-RES-REPRO", pack.Path, 0, string.Create(CultureInfo.InvariantCulture,
                $"el pack regenerado ({r.Bytes.Length} bytes) no coincide con el del disco ({onDisk.Length} bytes)"));
        }
        else
        {
            foreach (ReserveLevel l in r.Levels.Where(l => l.Chosen.Tints.CvdAdjacent > 0))
            {
                cvdUnavoidable.Add((pack.Path, l.Plan.Level));
            }
        }
    }

    private static IEnumerable<QaIssue> Level(CampaignSpec spec, QaLevel lv, QaPack campaign, List<ReserveSource> sources)
    {
        QaIssue Fail(string id, string msg) => new(id, lv.Pack, lv.Level, msg);
        if (!spec.Calendar.ContainsKey(lv.Level))
        {
            yield return Fail("QA-RES-SLOT", "el slot no esta en la especificacion");
            yield break;
        }

        LevelPlan plan = ReserveRunner.Plan(spec, lv.Level);
        if (lv.Replaces != plan.CampaignId)
        {
            yield return Fail("QA-RES-SLOT", $"replaces {lv.Replaces ?? "(falta)"}; el slot {lv.Level} es {plan.CampaignId}");
        }

        if (lv.Id != plan.Id || lv.Seed != plan.SeedText)
        {
            yield return Fail("QA-RES-SLOT", $"id {lv.Id} y seed {lv.Seed}; se esperan {plan.Id} y {plan.SeedText}");
        }

        if (lv.World != campaign.World || plan.World.World != campaign.World)
        {
            yield return Fail("QA-RES-SLOT", string.Create(CultureInfo.InvariantCulture, $"el slot es del mundo {plan.World.World} y el pack de campana, del {campaign.World}"));
        }

        ReserveSource? source = sources.FirstOrDefault(s => s.Id == lv.Replaces && s.Level == lv.Level);
        if (source is null)
        {
            yield return Fail("QA-RES-SLOT", $"{lv.Replaces} no esta en el pack de campana {campaign.PackId} con el nivel {lv.Level}");
        }
        else if (source.Mask == 0)
        {
            yield return Fail("QA-RES-MECH", $"{source.Id} no usa ninguna mecanica con kill switch: no necesita reserva");
        }

        if (lv.Board.HasMechanics || lv.Board.Cuias.Any(c => c.Contains(ReserveRunner.BarroUnit)) || lv.Murky || lv.Board.Hidden.Length > 0)
        {
            yield return Fail("QA-RES-MECH", "el tablero de reserva lleva mecanicas, Barro u ocultas");
        }

        if (lv.Relief.Any(r => r.Board.HasMechanics || r.Board.Cuias.Any(c => c.Contains(ReserveRunner.BarroUnit))))
        {
            yield return Fail("QA-RES-MECH", "una variante de alivio lleva mecanicas o Barro");
        }

        if (lv.Intro is not null || !lv.Skippable)
        {
            yield return Fail("QA-RES-MECH", $"intro_mechanic {lv.Intro ?? "null"} y skippable {lv.Skippable}: la reserva no estrena nada");
        }

        if (!plan.Window.Contains(lv.BotFail))
        {
            yield return Fail("QA-RES-WINDOW", string.Create(CultureInfo.InvariantCulture, $"bot_fail {lv.BotFail:0.000} fuera de la ventana {plan.Window} del slot ({plan.RoleName})"));
        }

        if (CampaignSpec.LabelRank(lv.Label) > CampaignSpec.LabelRank(plan.AllowedLabel))
        {
            yield return Fail("QA-RES-WINDOW", $"etiqueta {lv.Label}; el slot admite como mucho {plan.AllowedLabel ?? "normal"}");
        }

        if (lv.Band != plan.Band)
        {
            yield return Fail("QA-RES-WINDOW", $"band {lv.Band}; el slot es {plan.Band}");
        }

        if (lv.HasD)
        {
            yield return Fail("QA-RES-D", "publica difficulty.D");
        }
    }

    private static void WriteReport(string report, string[] args, int levels, int failures, List<QaIssue> ordered, List<(string Id, string Rule)> allIds, List<QaIssue> issues)
    {
        var sb = new StringBuilder();
        sb.Append("# Informe de `level-qa --reserve`\n\n");
        sb.Append("Generado por `dotnet run --project tools/level-qa -c Release -- ")
          .Append(string.Join(" ", args.Select(a => a.Replace('\\', '/'))))
          .Append("`: no se edita a mano.\n\n");
        sb.Append(string.Create(CultureInfo.InvariantCulture,
            $"- Tableros de reserva: {levels}; fallos: **{failures}**; avisos: {issues.Count(i => i.Warning)}.\n\n"));
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
}
