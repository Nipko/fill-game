// --self-test curve (D-068, fase F3): la especificación de la curva 2 (specs/campaign_v3.json) y lo que el generador hace con
// ella sin generar tableros: --validate-spec sin fallos, K por papel y por celda (k_cells), el papel del tope (CapRole, con la
// cúspide en la firma del mundo), ids, semillas y replaces de la curva y de su reserva, los packs curva_ y reserva_curva_ con
// pack_schema 5, el intro de las turbias en Normal, los estrenos de la 1.1 (Aguape, espeso y Lancha), las filas y el alivio con el Aguape (CurveBoards), que cada slot de 301-700 tiene alguna etapa
// dentro del tope de su papel, el escalón de presión de una celda y que campaign_v1 y campaign_v2 siguen en la curva 1.
// Desde la raíz del repositorio. Códigos: 0 = todo PASS; 1 = algún FAIL.
using System.Globalization;
using AmazonFill.Tools.LevelGenerator.Campaign;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal static class CurveSelfTest
{
    public const string SpecPath = "tools/level-generator/specs/campaign_v3.json";

    public static int Run()
    {
        int fails = 0;
        void Check(bool ok, string what)
        {
            fails += ok ? 0 : 1;
            Console.WriteLine(AsciiText.Of($"{(ok ? "PASS" : "FAIL")}  curve: {what}"));
        }

        CampaignSpec v3 = CampaignSpec.Load(SpecPath, null);
        CampaignSpec v2 = CampaignSpec.Load("tools/level-generator/specs/campaign_v2.json", null);
        List<SpecIssue> issues = SpecValidator.Validate(v3);
        Check(!issues.Any(i => !i.Warning), string.Create(CultureInfo.InvariantCulture,
            $"campaign_v3 pasa --validate-spec ({issues.Count(i => !i.Warning)} fallos, {issues.Count(i => i.Warning)} avisos PRESSURE)"));
        Check(issues.Where(i => i.Warning).All(i => i.Rule == "PRESSURE"), "los avisos son solo de PRESSURE");

        // K: k_plan por papel (respiros y comodidad 8, retos 9, hitos 11) y k_cells (329, 346 y 349 con 10; 343-345 y 323-324 con 7;
        // cúspides con 12).
        string ks = string.Join(",", new[] { 301, 304, 306, 319, 320, 329, 343, 346, 349, 323, 420, 440, 560, 700 }.Select(l => v3.Plan(l).K.ToString(CultureInfo.InvariantCulture)));
        Check(ks == "8,8,9,8,11,10,7,10,10,7,12,11,12,12", $"K de 301, 304, 306, 319, 320, 329, 343, 346, 349, 323, 420, 440, 560 y 700: {ks}");
        Check(v3.KAt(329) == 10 && v3.KAt(306) == 9, "KAt lee k_cells antes que el k_plan");
        string roles = string.Join(",", new[] { 301, 307, 343, 306, 319, 400, 420 }.Select(l => v3.Plan(l).CapRole));
        Check(roles == "respite,comfort,debut,challenge,respite,milestone,summit", $"papeles del tope: {roles}");

        // Ids y semillas (08 sec. 4.1): curva 2 desde el 301, historia antes.
        LevelPlan p = v3.Plan(306);
        Check(p.Curve == 2 && p.Id == "m07_v1_n0306_c2" && p.SeedText == "amazonfill:v1:m07:v1:n0306:c2" && p.ReplacesId == "m07_v1_n0306",
            $"el 306 de la curva 2: {p.Id}, {p.SeedText}, replaces {p.ReplacesId}");
        Check(v3.Plan(300).Curve == 0 && v3.Plan(300).Id == "m06_v1_n0300", "el 300 es historia (curva 1)");
        LevelPlan r = ReserveRunner.Plan(v3, 306);
        Check(r.Id == "m07_v1_n0306_c2_r1" && r.SeedText == "amazonfill:v1:m07:v1:n0306:c2:r1" && r.ReplacesId == "m07_v1_n0306_c2" && r.Mechanics.Count == 0,
            $"reserva del 306: {r.Id}, {r.SeedText}, replaces {r.ReplacesId}");
        WorldInfo w7 = v3.WorldOf(306);
        Check(PackWriter.PackIdFor(v3, w7) == "curva_07_v1" && ReserveRunner.PackIdFor(v3, w7) == "reserva_curva_07_v1" && ReserveRunner.PackSchemaFor(v3, w7) == 5,
            "packs del mundo 7: curva_07_v1 y reserva_curva_07_v1 (pack_schema 5)");
        Check(CampaignRunner.GenerationFirst(v3) == 301 && CampaignRunner.GenerationFirst(v2) == 201, "se genera desde curve.from_level (301)");

        // Las turbias en Normal: estreno 323 (intro murky) y 324 con estrella; los slots de F2 declarados.
        Check(v3.Plan(323).Intro == "murky" && v3.Plan(324).Intro is null && v3.Plan(323).DebutBoard && v3.Plan(323).Window.Hi <= 0.2,
            "323-324: estreno de las turbias en Normal (intro murky en el 323, ventana de estreno)");
        Check(v3.Plan(303).Intro == "aguape" && v3.Plan(347).Intro == "aguape_n2" && v3.Plan(363).Intro == "lancha"
            && new[] { 303, 304, 305, 347, 348, 363, 364, 365 }.All(l => v3.Plan(l).DebutBoard && v3.Plan(l).Mechanics.Count == 1),
            "estrenos de la 1.1: Aguape 303-305 (intro aguape), espeso 347-348 (aguape_n2) y Lancha 363-365 (lancha), solos y con estrella");

        // Filas y alivio con el Aguapé (CurveBoards): las 3 filas de D-006, un grupo nunca entre dos filas, el vacío de plus_empty en
        // una ranura que no lo parte y la lancha con k >= vacíos + 1.
        Check(string.Join(" ", new[] { 11, 12, 13, 14 }.Select(n => string.Join("/", CurveBoards.ThreeRows(n)))) == "4/4/3 4/4/4 5/4/4 5/5/4",
            "rows con el Aguape: 4/4/3, 4/4/4, 5/4/4 y 5/5/4");
        AF.Core.Mechanics.MechanicSpec ag = new(AF.Core.Mechanics.MechanicType.Aguape, group: 0);
        AF.Core.Mechanics.MechanicSpec no = AF.Core.Mechanics.MechanicSpec.None;
        Check(CurveBoards.GroupsInRows([no, no, no, no, ag, ag, no, no], [4, 4]) && !CurveBoards.GroupsInRows([no, no, no, ag, ag, no, no, no], [4, 4]),
            "un grupo en 4-5 de 8 cuias (4 + 4) vale; en 3-4 se parte");
        int[][] empty8 = [.. Enumerable.Range(0, 8).Select(i => i < 7 ? new[] { 1 } : Array.Empty<int>())];
        var board = new Metrics.BoardData([.. Enumerable.Repeat(4, 8)], empty8, [no, no, no, no, no, no, ag, ag], []);
        Metrics.BoardData? plus = CurveBoards.PlusEmpty(board);
        Check(plus is not null && plus.Caps.Length == 9 && CurveBoards.GroupsInRows(plus.Attrs, CurveBoards.LayoutRows(9, true)!),
            "plus_empty con el grupo en 6-7 de 8 cuias: el vacio no parte el grupo con 9 cuias (5 + 4)");
        var boat = new Metrics.BoardData([.. Enumerable.Repeat(4, 8)], empty8,
            [no, new AF.Core.Mechanics.MechanicSpec(AF.Core.Mechanics.MechanicType.Lancha, k: 2), no, no, no, no, no, no], []);
        Check(CurveBoards.PlusEmpty(boat)?.Attrs[1].K == 3 && CurveBoards.WithLanchaK(boat) == boat,
            "la lancha de k 2 con 1 vacio sube a k 3 en plus_empty (2 vacios) y no cambia en el nivel");

        // Cada slot de 301-700 tiene alguna etapa dentro del tope de su papel (si no, no tendría candidatos).
        var gen = new SlotGenerator(v3);
        List<int> none = Enumerable.Range(301, 400).Where(l => Enumerable.Range(0, SlotGenerator.Stages).All(s => gen.Config(v3.Plan(l), s, dropMurky: false) is null)).ToList();
        Check(none.Count == 0, $"todos los slots de 301-700 tienen alguna etapa dentro del tope ({(none.Count == 0 ? "-" : string.Join(", ", none))})");

        // Superposición de la 1.1 (F3b): el estreno del Aguapé (303), del espeso (347) y de la Lancha (363) dan candidatos en la
        // ventana con su mecánica, el grupo en una fila, la lancha con k = vacíos + 1 y la solución del par que suelta y atraca.
        foreach ((int level, string code) in new[] { (303, MechanicCodes.Aguape), (347, MechanicCodes.AguapeThick), (363, MechanicCodes.Lancha) })
        {
            Candidate? c = gen.Generate(v3.Plan(level)).Candidates.FirstOrDefault(x => x.PoolMember);
            Metrics.ParResult? par = c is null ? null : gen.Metrics.Par(c.State);
            int lancha = c is null ? -1 : Array.FindIndex(c.Board.Attrs, a => a.Type == AF.Core.Mechanics.MechanicType.Lancha);
            bool ok = c is not null && c.Board.Attrs.Any(a => MechanicCodes.CodeOf(a) == code)
                && CurveBoards.GroupsInRows(c.Board.Attrs, CurveBoards.LayoutRows(c.Board.Caps.Length, aguape: true)!)
                && (lancha < 0 || c.Board.Attrs[lancha].K == Enumerable.Range(0, c.Board.Cuias.Length).Count(i => i != lancha && c.Board.Cuias[i].Length == 0) + 1)
                && par is { Status: AF.Solver.Search.SolveStatus.Solved } && CurveBoards.SolutionProblem(c.State, par.Solution) is null;
            Check(ok, string.Create(CultureInfo.InvariantCulture,
                $"el {level} ({code}) da un candidato en la ventana con su mecanica, el grupo en una fila y la solucion que suelta o atraca"));
        }

        // Escalón de presión de una celda: el 320 (Ha+Ll+Br, K 11) = vacío 1 + 3 mecánicas 2 + Lluvia 1 + K 11 1 = 5.
        int pressure = Pressure.OfCell(v3.Plan(320).Cell, v3.Plan(320).K, milestone: true);
        Check(pressure == 5, string.Create(CultureInfo.InvariantCulture, $"escalon del hito 320: {pressure} (se espera 5)"));

        // La curva 1 no cambia: campaign_v2 sin curva, ids de siempre y sin topes.
        LevelPlan old = v2.Plan(306);
        Check(old.Curve == 0 && old.Id == "m07_v1_n0306" && old.ReplacesId is null && v2.CapOf(old) is null && PackWriter.PackIdFor(v2, v2.WorldOf(306)) == "mundo_07_v1",
            "campaign_v2 sigue en la curva 1 (mundo_07_v1, sin topes por papel)");

        Console.WriteLine(fails == 0 ? "--self-test curve: todo PASS" : string.Create(CultureInfo.InvariantCulture, $"--self-test curve: {fails} FAIL"));
        return fails == 0 ? 0 : 1;
    }
}
