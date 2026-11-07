// --self-test curva (D-068, fase F3): las comprobaciones de la curva 2 (Checks/CurveChecks.cs) sobre tableros sintéticos, sin el
// núcleo (QA-AGUAPE y QA-LANCHA son el esqueleto que se activa con F2): QA-T-ROLE (topes por papel con la especificación
// campaign_v3, ids, semillas y replaces de la curva), QA-MURKY-NORMAL, los grupos y las filas de QA-AGUAPE, QA-LANCHA y la
// lectura de un pack con el Aguapé (rows, attrs en bruto y PendingTypes mientras el núcleo no lo conozca). Desde la raíz del
// repositorio. Códigos: 0 = todo PASS; 1 = algún FAIL; 2 = falta la especificación.
using System.Globalization;
using System.Text;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class CurveQaSelfTest
{
    private const string Spec = "tools/level-generator/specs/campaign_v3.json";

    public static int Run()
    {
        if (!File.Exists(Spec))
        {
            Console.Error.WriteLine("level-qa: --self-test curva se ejecuta desde la raiz del repositorio (falta " + Spec + ")");
            return 2;
        }

        CampaignSpec spec = CampaignSpec.Load(Spec, null);
        int fails = 0;
        void Expect(string name, IEnumerable<QaIssue> issues, string? id)
        {
            List<QaIssue> failures = issues.Where(i => !i.Warning).ToList();
            bool ok = id is null ? failures.Count == 0 : failures.Any(i => i.Check == id);
            fails += ok ? 0 : 1;
            string got = failures.Count == 0 ? "ningun fallo" : string.Join("; ", failures.Select(i => i.Check + ": " + i.Message));
            Console.WriteLine(AsciiText.Of($"{(ok ? "PASS" : "FAIL")}  curva/{name}: {(id is null ? "espera 0 fallos" : "espera " + id)}; da {got}"));
        }

        // QA-T-ROLE: el reto 306 (K 9; 10-12 cuias) y su id, semilla y replaces de la curva 2.
        LevelPlan p306 = spec.Plan(306);
        QaLevel reto = Level(spec, 306, 10, 9);
        Expect("t_role_reto_valido", CurveChecks.TRole(spec, Eval(spec, reto), "curve"), null);
        Expect("t_role_reto_13_cuias", CurveChecks.TRole(spec, Eval(spec, Level(spec, 306, 13, 9)), "curve"), "QA-T-ROLE");
        Expect("t_role_reto_k_11", CurveChecks.TRole(spec, Eval(spec, Level(spec, 306, 12, 11)), "curve"), "QA-T-ROLE");
        Expect("t_role_replaces", CurveChecks.TRole(spec, Eval(spec, reto with { Replaces = "m07_v1_n0307" }), "curve"), "QA-T-ROLE");
        Expect("t_role_id_sin_c2", CurveChecks.TRole(spec, Eval(spec, reto with { Id = p306.CampaignId }), "curve"), "QA-T-ROLE");
        Expect("t_role_seed_sin_c2", CurveChecks.TRole(spec, Eval(spec, reto with { Seed = "amazonfill:v1:m07:v1:n0306" }), "curve"), "QA-T-ROLE");
        Expect("t_role_historia_en_curve", CurveChecks.TRole(spec, Eval(spec, Level(spec, 250, 12, 10)), "curve"), "QA-T-ROLE");
        // La cúspide del 420 (13-14 cuias, K 11-12) frente a un hito normal (12-13): 14 cuias valen solo en la cúspide.
        Expect("t_role_cuspide_14", CurveChecks.TRole(spec, Eval(spec, Level(spec, 420, 14, 12)), "curve"), null);
        Expect("t_role_hito_14", CurveChecks.TRole(spec, Eval(spec, Level(spec, 400, 14, 12)), "curve"), "QA-T-ROLE");
        // La reserva: solo los topes (su replaces lo mira QA-RES-SLOT).
        Expect("t_role_reserva", CurveChecks.TRole(spec, Eval(spec, reto with { Replaces = "x" }), "reserve"), null);
        Check("ids de la curva 2", p306.Id == "m07_v1_n0306_c2" && p306.SeedText == "amazonfill:v1:m07:v1:n0306:c2" && p306.ReplacesId == "m07_v1_n0306");

        // QA-MURKY-NORMAL: comodidad Normal del 331 con una cuia turbia de 1-2 dosis; dos cuias, tres dosis o una recuperación fallan.
        QaLevel comfort = Level(spec, 331, 9, 8);
        Expect("murky_normal_valido", CurveChecks.MurkyNormal(spec, Eval(spec, Hidden(comfort, [[2, 0], [2, 1]]))), null);
        Expect("murky_normal_dos_cuias", CurveChecks.MurkyNormal(spec, Eval(spec, Hidden(comfort, [[2, 0], [3, 0]]))), "QA-MURKY-NORMAL");
        Expect("murky_normal_tres_dosis", CurveChecks.MurkyNormal(spec, Eval(spec, Hidden(comfort, [[2, 0], [2, 1], [2, 2]]))), "QA-MURKY-NORMAL");
        Expect("murky_normal_recuperacion", CurveChecks.MurkyNormal(spec, Eval(spec, Hidden(Level(spec, 341, 10, 8), [[2, 0]]))), "QA-MURKY-NORMAL");
        Expect("murky_normal_antes_del_estreno", CurveChecks.MurkyNormal(spec, Eval(spec, Hidden(Level(spec, 304, 10, 8), [[2, 0]]))), "QA-MURKY-NORMAL");
        Expect("murky_reto_dos_dosis", CurveChecks.MurkyNormal(spec, Eval(spec, Hidden(reto, [[2, 0], [3, 0]]) with { Label = "hard" })), null);
        Expect("murky_reto_una_dosis", CurveChecks.MurkyNormal(spec, Eval(spec, Hidden(reto, [[2, 0]]) with { Label = "hard" })), "QA-MURKY-NORMAL");
        Expect("murky_reto_tres_cuias", CurveChecks.MurkyNormal(spec, Eval(spec, Hidden(reto, [[2, 0], [3, 0], [4, 0]]) with { Label = "hard" })), "QA-MURKY-NORMAL");
        QaLevel hito = Level(spec, 400, 13, 11);
        Expect("murky_hito_dos_dosis", CurveChecks.MurkyNormal(spec, Eval(spec, Hidden(hito, [[2, 0], [3, 0]]) with { Label = "very_hard" })), "QA-MURKY-NORMAL");
        Expect("murky_hito_tres_dosis", CurveChecks.MurkyNormal(spec, Eval(spec, Hidden(hito, [[2, 0], [2, 1], [3, 0]]) with { Label = "very_hard" })), null);
        // Estreno en Normal (323-324): 1 dosis; intro murky solo en el 323.
        QaLevel debut = Level(spec, 323, 10, 7) with { Intro = "murky", Skippable = false };
        Expect("murky_estreno_una_dosis", CurveChecks.MurkyNormal(spec, Eval(spec, Hidden(debut, [[2, 0]]))), null);
        Expect("murky_estreno_dos_dosis", CurveChecks.MurkyNormal(spec, Eval(spec, Hidden(debut, [[2, 0], [2, 1]]))), "QA-MURKY-NORMAL");
        Expect("murky_intro_fuera_del_estreno", CurveChecks.MurkyNormal(spec, Eval(spec, Hidden(comfort, [[2, 0]]) with { Intro = "murky" })), "QA-MURKY-NORMAL");

        // QA-AGUAPE: grupos y filas (esqueleto F2). 9 cuias: el grupo 0 en 4-5.
        QaLevel ag = Aguape(Level(spec, 450, 9, 8), [(2, 0, null), (3, 0, null)]);
        Expect("aguape_valido", CurveChecks.Aguape(spec, ag), null);
        Expect("aguape_no_contiguo", CurveChecks.Aguape(spec, Aguape(ag, [(1, 0, null), (3, 0, null)])), "QA-AGUAPE");
        Expect("aguape_una_cuia", CurveChecks.Aguape(spec, Aguape(ag, [(2, 0, null)])), "QA-AGUAPE");
        Expect("aguape_espeso_3", CurveChecks.Aguape(spec, Aguape(ag, [(1, 1, 2), (2, 1, 2), (3, 1, 2)])), "QA-AGUAPE");
        Expect("aguape_espeso_4", CurveChecks.Aguape(spec, Aguape(ag, [(0, 1, 2), (1, 1, 2), (2, 1, 2), (3, 1, 2)])), null);
        Expect("aguape_pendiente_f2", CurveChecks.Aguape(spec, ag with { PendingTypes = ["aguape"] }), "QA-AGUAPE");
        QaLevel big = Aguape(Level(spec, 460, 12, 11), [(4, 0, null), (5, 0, null)]);
        Expect("aguape_11_sin_rows", CurveChecks.Aguape(spec, big), "QA-AGUAPE");
        Expect("aguape_rows_validas", CurveChecks.Aguape(spec, big with { Rows = [4, 4, 4] }), null);
        Expect("aguape_grupo_partido", CurveChecks.Aguape(spec, Aguape(big, [(3, 0, null), (4, 0, null)]) with { Rows = [4, 4, 4] }), "QA-AGUAPE");
        Expect("aguape_rows_no_suman", CurveChecks.Aguape(spec, big with { Rows = [4, 4, 3] }), "QA-AGUAPE");
        // Sin rows y con 10 cuias o menos, las filas de 02 sec. 5 [P]: 9 cuias = 5 + 4; un grupo en 4-5 queda partido.
        Expect("aguape_partido_sin_rows", CurveChecks.Aguape(spec, Aguape(ag, [(3, 0, null), (4, 0, null), (5, 0, null)])), "QA-AGUAPE");
        Check("rows: 2 filas de 7 valen, 3 de 6 no, 13 cuias en 2 filas no", CurveChecks.RowsProblem([7, 5], 12) is null
            && CurveChecks.RowsProblem([6, 6, 2], 14) is not null && CurveChecks.RowsProblem([7, 6], 13) is not null && CurveChecks.RowsProblem([5, 4, 4], 13) is null);

        // QA-LANCHA (esqueleto F2): la carga de 1-3 dosis en la cuia 3, k >= 2 y >= vacíos + 1 (1 vacío: k 3 vale y k 1 no).
        QaLevel boat = Lancha(Level(spec, 450, 9, 8), 3, 3);
        Expect("lancha_valida", CurveChecks.Lancha(spec, boat), null);
        Expect("lancha_k_corto", CurveChecks.Lancha(spec, Lancha(boat, 3, 1)), "QA-LANCHA");
        Expect("lancha_dos", CurveChecks.Lancha(spec, Lancha(boat, 4, 3, keep: true)), "QA-LANCHA");
        Expect("lancha_turbia", CurveChecks.Lancha(spec, Hidden(boat, [[5, 0]])), "QA-LANCHA");
        Expect("lancha_con_liana_antes_del_401", CurveChecks.Lancha(spec, Lancha(Liana(Level(spec, 380, 9, 8), 6), 3, 3)), "QA-LANCHA");
        Expect("lancha_con_liana_desde_el_401", CurveChecks.Lancha(spec, Lancha(Liana(Level(spec, 450, 9, 8), 6), 3, 3)), null);
        Expect("lancha_intro_en_su_estreno", CurveChecks.Lancha(spec, Lancha(Level(spec, 363, 9, 8), 3, 3) with { Intro = "lancha" }), null);
        Expect("lancha_intro_fuera_de_su_estreno", CurveChecks.Lancha(spec, boat with { Intro = "lancha" }), "QA-LANCHA");

        // Lectura: un pack con el Aguapé y rows (pack_schema 5).
        string path = Path.Combine(Path.GetTempPath(), "af_curve_selftest.json");
        File.WriteAllText(path, AguapePackJson(), new UTF8Encoding(false));
        try
        {
            QaLevel read = PackReader.Read(path).Levels[0];
            bool coreKnows = MechanicTypes.TryParse("aguape", out _);
            Check(string.Create(CultureInfo.InvariantCulture, $"PackReader lee rows, attrs en bruto y el Aguape ({(coreKnows ? "el nucleo lo conoce" : "pendiente de F2")})"),
                read.Rows is [5, 4] && read.RawAttrs[0]?.Type == "aguape" && read.RawAttrs[1]?.Group == 0 && read.Pressure == 3
                && (coreKnows ? read.PendingTypes.Length == 0 : read.PendingTypes.SequenceEqual(["aguape"])));
            if (!coreKnows)
            {
                LevelEval e = Evaluator.Evaluate(spec, [PackReader.Read(path)])[0];
                Check("sin F2, el nivel con el Aguape no se construye (BuildError)", e.BuildError?.Contains("aguape", StringComparison.Ordinal) == true);
            }
        }
        finally
        {
            File.Delete(path);
        }

        // El Aguapé (y rows) con un pack_schema menor que 5: el lector falla cerrado, con F2 o sin él (D-068, contrato 21).
        File.WriteAllText(path, AguapePackJson().Replace("\"pack_schema\":5", "\"pack_schema\":4", StringComparison.Ordinal), new UTF8Encoding(false));
        try
        {
            bool rejected = false;
            try
            {
                PackReader.Read(path);
            }
            catch (PackFormatException)
            {
                rejected = true;
            }

            Check("PackReader rechaza el Aguape con pack_schema 4", rejected);
        }
        finally
        {
            File.Delete(path);
        }

        Console.WriteLine(fails == 0 ? "--self-test curva: todo PASS" : string.Create(CultureInfo.InvariantCulture, $"--self-test curva: {fails} FAIL"));
        return fails == 0 ? 0 : 1;

        void Check(string what, bool ok)
        {
            fails += ok ? 0 : 1;
            Console.WriteLine(AsciiText.Of($"{(ok ? "PASS" : "FAIL")}  curva: {what}"));
        }
    }

    private static LevelEval Eval(CampaignSpec spec, QaLevel lv) =>
        new() { Level = lv, Plan = spec.Calendar.ContainsKey(lv.Level) ? spec.Plan(lv.Level) : null };

    /// <summary>Nivel sintético de la curva 2 con <paramref name="t"/> cuias y <paramref name="k"/> colores (ids, semilla y replaces del plan).</summary>
    private static QaLevel Level(CampaignSpec spec, int level, int t, int k)
    {
        LevelPlan plan = spec.Plan(level);
        int[][] cuias = Enumerable.Range(0, t).Select(i => i < k ? new[] { i + 1, i + 1, i + 1, i + 1 } : Array.Empty<int>()).ToArray();
        var board = new BoardData(Enumerable.Repeat(4, t).ToArray(), cuias, Enumerable.Repeat(MechanicSpec.None, t).ToArray(), []);
        return new QaLevel(
            "sintetico.json", 0, plan.Id, level, plan.World.World, plan.SeedText, plan.World.WaterBase,
            Enumerable.Range(1, k).Select(i => "t" + i.ToString(CultureInfo.InvariantCulture)).ToArray(), 4, new Dictionary<int, int>(),
            board, false, "normal", 0.3, 2 * k, 0, 0, 0, 0, plan.Band, false, 0, null, true, true, [], [], "walk", plan.ReplacesId)
        {
            RawAttrs = new RawAttr?[t],
        };
    }

    private static QaLevel Hidden(QaLevel lv, int[][] hidden)
    {
        int[][] cuias = lv.Board.Cuias.Select((c, i) => hidden.Any(h => h[0] == i) ? new[] { 1, 2, 3 } : c).ToArray();
        return lv with { Board = lv.Board with { Cuias = cuias, Hidden = hidden }, Murky = true };
    }

    private static QaLevel Aguape(QaLevel lv, (int Cuia, int Group, int? N)[] cells)
    {
        var raw = new RawAttr?[lv.Board.Cuias.Length];
        foreach ((int cuia, int group, int? n) in cells)
        {
            raw[cuia] = new RawAttr("aguape", group, n, null);
        }

        return lv with { RawAttrs = raw };
    }

    private static QaLevel Lancha(QaLevel lv, int cuia, int k, bool keep = false)
    {
        RawAttr?[] raw = keep ? [.. lv.RawAttrs] : lv.RawAttrs.Select(a => a?.Type == "lancha" ? null : a).ToArray();
        raw[cuia] = new RawAttr("lancha", null, null, k);
        int[][] cuias = lv.Board.Cuias.Select((c, i) => i == cuia ? c.Take(2).ToArray() : c).ToArray();
        return lv with { RawAttrs = raw, Board = lv.Board with { Cuias = cuias } };
    }

    private static QaLevel Liana(QaLevel lv, int cuia)
    {
        RawAttr?[] raw = [.. lv.RawAttrs];
        raw[cuia] = new RawAttr("liana", null, null, null);
        return lv with { RawAttrs = raw };
    }

    /// <summary>Un pack de un nivel con un grupo de Aguapé en las cuias 0-1, rows [5, 4] y difficulty.pressure 3.</summary>
    private static string AguapePackJson() =>
        "{\"pack_schema\":5,\"schema_file\":\"schemas/af-level-1.schema.json\",\"pack_id\":\"curva_08_v1\",\"kind\":\"curve\",\"world\":8,"
        + "\"world_id\":\"w08_madidi\",\"lap\":1,\"water_base\":\"white\",\"pool\":[],\"k_min\":7,\"k_max\":7,\"max_risk_pairs\":10,\"levels\":[\n"
        + "{\"schema\":\"af-level/1\",\"id\":\"m08_v1_n0450_c2\",\"replaces\":\"m08_v1_n0450\",\"level\":450,\"world\":8,\"lap\":1,"
        + "\"seed\":\"amazonfill:v1:m08:v1:n0450:c2\",\"gen\":{\"tool\":\"level-generator\",\"version\":\"1.0.0\",\"source\":\"walk\"},"
        + "\"water_base\":\"white\",\"palette\":[],\"units_per_color\":4,\"caps\":[4,4,4,4,4,4,4,4,4],"
        + "\"cuias\":[[1,1],[2,2,2],[3,3,3,3],[4,4,4,4],[5,5,5,5],[6,6,6,6],[7,7,7,7],[1,1,2],[]],"
        + "\"attrs\":[{\"type\":\"aguape\",\"group\":0},{\"type\":\"aguape\",\"group\":0},null,null,null,null,null,null,null],"
        + "\"rows\":[5,4],\"murky\":false,\"hidden\":[],\"difficulty\":{\"label\":\"normal\",\"bot_fail\":0.300,\"par\":14,\"band\":\"comfort\",\"pressure\":3},"
        + "\"risk_pairs\":0,\"intro_mechanic\":null,\"skippable\":true,\"hint_supported\":true,\"relief\":[]}\n]}\n";
}
