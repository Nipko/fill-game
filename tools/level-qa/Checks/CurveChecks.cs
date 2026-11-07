// Comprobaciones de la curva 2 de la 1.1 (D-068; 03 §17, comprobaciones 16-19; 08 §4.1 «Validación en tools/level-qa»):
//   QA-T-ROLE       T (cuias) y K (colores) dentro del tope de su papel (role_caps de la especificación; 03 §12; cúspide solo en
//                   la firma del mundo); en un pack kind curve, replaces = el id de la curva 1 del mismo level y mundo, e id y
//                   seed con el sufijo de la curva; difficulty.pressure, si está, igual al que dan las palancas del tablero (aviso).
//   QA-MURKY-NORMAL un nivel Normal con Aguas turbias: comodidad (nunca recuperación ni remanso), 1 cuia turbia con como mucho
//                   murky_normal.max_hidden_units (2) dosis ocultas, desde murky_normal.from_level; por ciclo, como mucho
//                   comfort_per_cycle comodidades turbias (aviso si hay menos: el generador puede publicar sin turbias por T1),
//                   2 retos turbios y 1 hito turbio cada 2 ciclos (02 §11, T1).
//   QA-AGUAPE       esqueleto que se activa con F2: cada group con 2-5 cuias contiguas, en la misma fila (con rows), sin Barro; el
//                   espeso (n = 2) con 4-5; con 11 cuias o más, rows obligatorio; rows suma las cuias, con los topes de 02 §5 y sin
//                   partir un grupo (también en relief); intro aguape/aguape_n2 solo en su estreno; nunca con la tinaja. Lo que
//                   necesita las reglas del núcleo (una cuia del grupo sella solo recibiendo; atascado(S0)) llega con F2: hasta
//                   entonces un nivel con el Aguapé falla cerrado (el núcleo no lo conoce).
//   QA-LANCHA       esqueleto que se activa con F2: una lancha por nivel; carga de 1-3 dosis visibles, sin Barro; k >= vacíos
//                   iniciales + 1; nunca con Aguas turbias; con la Liana solo desde el 401; intro lancha solo en su estreno. La
//                   atracada en la solución llega con F2; hasta entonces un nivel con la Lancha falla cerrado.
// Al integrar F2 (rama v11/curva-f2-nucleo; Packages/com.amazonfill.core/README.md): QA-AGUAPE llama a
// LevelValidation.ValidateRows(state, rows) para el nivel y para cada variante de relief (Validate no la llama), y el informe
// suma el bot atento (AF.Solver.Bot.AttentiveBot; 03 §17 #20). PackReader ya rechaza el Aguapé, la Lancha, rows y pressure con
// un pack_schema menor que 5.
// La comprobación de par de la curva 2 (QA-PAR: solo par >= 2K) y las trampas tempranas (QA-EARLY-TRAPS: 0 en Normal y 1 en
// Difícil) viven en BaseChecks y valen igual aquí.
using System.Globalization;
using AF.Core.Board;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class CurveChecks
{
    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-T-ROLE", "curva 2: T y K dentro del tope de su papel (role_caps; 03 sec. 12 y sec. 17 #16); en kind curve, replaces = el id de la curva 1 del mismo slot e id/seed con _c{c}/:c{c}; aviso si difficulty.pressure no es el de las palancas del tablero (D-068)"),
        ("QA-MURKY-NORMAL", "curva 2: Normal con turbias solo en comodidad, con 1 cuia y <= 2 dosis ocultas, desde murky_normal.from_level; por ciclo <= comfort_per_cycle comodidades, <= 2 retos y <= 1 hito cada 2 ciclos con turbias (02 sec. 11 T1; 03 sec. 17 #19)"),
        ("QA-AGUAPE", "esqueleto F2: grupos de 2-5 cuias contiguas en la misma fila, sin Barro (espeso n = 2: 4-5); rows con >= 11 cuias, que suma las cuias y respeta 02 sec. 5 sin partir un grupo (tambien en relief); estreno solo en su nivel; nunca con la tinaja; las reglas del nucleo llegan con F2 (03 sec. 17 #17)"),
        ("QA-LANCHA", "esqueleto F2: una lancha por nivel; carga de 1-3 dosis visibles sin Barro; k >= vacios + 1; sin Aguas turbias; con la Liana solo desde el 401; estreno solo en su nivel; la atracada en la solucion llega con F2 (03 sec. 17 #18)"),
    ];

    /// <summary>Primer nivel en que la Lancha puede ir con la Liana (03 §11, fila 401-700).</summary>
    public const int LanchaWithLianaFrom = 401;

    /// <summary>Comprobaciones de un nivel (las de la curva 2 solo si su plan lo es; QA-AGUAPE y QA-LANCHA, siempre que los lleve).</summary>
    public static IEnumerable<QaIssue> Run(QaContext ctx, LevelEval e)
    {
        QaPack? pack = ctx.Packs.FirstOrDefault(p => p.Path == e.Level.Pack);
        return TRole(ctx.Spec, e, pack?.Kind ?? "campaign")
            .Concat(MurkyNormal(ctx.Spec, e))
            .Concat(Aguape(ctx.Spec, e.Level))
            .Concat(Lancha(e.Level));
    }

    public static IEnumerable<QaIssue> TRole(CampaignSpec spec, LevelEval e, string kind)
    {
        QaLevel lv = e.Level;
        QaIssue Fail(string msg, bool warning = false) => new("QA-T-ROLE", lv.Pack, lv.Level, msg, warning);
        if (kind == "curve" && e.Plan is not { Curve: > 0 })
        {
            yield return Fail($"pack kind curve con un nivel fuera de la curva 2 de la especificacion {spec.SpecId}");
            yield break;
        }

        if (e.Plan is not { Curve: > 0 } plan)
        {
            yield break;
        }

        if (spec.CapOf(plan) is not RoleCap cap)
        {
            yield return Fail($"la especificacion no tiene el tope del papel {plan.CapRole} (role_caps)");
        }
        else
        {
            int t = lv.Board.Cuias.Length;
            if (t < cap.TMin || t > cap.TMax || lv.K < cap.KMin || lv.K > cap.KMax)
            {
                yield return Fail(string.Create(CultureInfo.InvariantCulture,
                    $"T = {t} y K = {lv.K}; el tope de {plan.CapRole} es T {cap.TMin}-{cap.TMax} y K {cap.KMin}-{cap.KMax}"));
            }
        }

        if (kind == "curve")
        {
            if (lv.Replaces != plan.CampaignId)
            {
                yield return Fail($"replaces {lv.Replaces ?? "(falta)"}; el slot {lv.Level} de la curva 1 es {plan.CampaignId}");
            }

            if (lv.Id != plan.Id || lv.Seed != plan.SeedText || lv.World != plan.World.World)
            {
                yield return Fail($"id {lv.Id}, seed {lv.Seed} y mundo {lv.World}; se esperan {plan.Id}, {plan.SeedText} y {plan.World.World}");
            }
        }

        if (lv.Pressure is int published && lv.PendingTypes.Length == 0)
        {
            int expected = Pressure.OfBoard(lv.Board, lv.K, plan.Role == SlotRole.Milestone);
            if (published != expected)
            {
                yield return Fail(string.Create(CultureInfo.InvariantCulture, $"difficulty.pressure {published}; las palancas del tablero dan {expected}"), warning: true);
            }
        }
    }

    public static IEnumerable<QaIssue> MurkyNormal(CampaignSpec spec, LevelEval e)
    {
        QaLevel lv = e.Level;
        if (e.Plan is not { Curve: > 0 } plan || lv.Label != "normal")
        {
            yield break;
        }

        int[][] murky = MechanicChecks.MurkyHidden(lv.Board);
        if (murky.Length == 0)
        {
            yield break;
        }

        QaIssue Fail(string msg) => new("QA-MURKY-NORMAL", lv.Pack, lv.Level, msg);
        MurkyNormalRule? rule = spec.MurkyNormal;
        if (rule is null || lv.Level < rule.FromLevel)
        {
            yield return Fail(string.Create(CultureInfo.InvariantCulture,
                $"Aguas turbias en un nivel Normal antes del estreno de las turbias en Normal ({(rule is null ? "sin murky_normal" : rule.FromLevel.ToString(CultureInfo.InvariantCulture))})"));
            yield break;
        }

        if (plan.Role != SlotRole.Comfort || plan.Ramp)
        {
            yield return Fail($"Aguas turbias en un nivel Normal de {plan.RoleName}: en Normal solo en comodidad (nunca en recuperacion ni en remanso)");
        }

        int cuias = murky.Select(h => h.Length > 0 ? h[0] : -1).Distinct().Count();
        if (cuias != 1 || murky.Length > rule.MaxHiddenUnits)
        {
            yield return Fail(string.Create(CultureInfo.InvariantCulture,
                $"{murky.Length} dosis ocultas en {cuias} cuias; en Normal, 1 cuia con como mucho {rule.MaxHiddenUnits}"));
        }
    }

    /// <summary>Recuento por ciclo de la curva 2 (02 §11, T1): comodidades, retos e hitos con Aguas turbias.</summary>
    public static IEnumerable<QaIssue> RunCampaign(QaContext ctx)
    {
        CampaignSpec spec = ctx.Spec;
        if (spec.Curve is null || spec.MurkyNormal is not { } rule)
        {
            yield break;
        }

        var curve = ctx.Levels.Where(e => e.Plan is { Curve: > 0 } && !e.Plan.Ramp).ToList();
        var hitosByPair = new Dictionary<int, int>();
        foreach (IGrouping<int, LevelEval> g in curve.GroupBy(e => (e.Level.Level - spec.CycleFrom) / 20).OrderBy(g => g.Key))
        {
            List<LevelEval> cycle = g.ToList();
            if (cycle.Count != 20)
            {
                continue;
            }

            int start = cycle.Min(e => e.Level.Level);
            string pack = cycle[0].Level.Pack;
            bool Murky(LevelEval e) => MechanicChecks.MurkyHidden(e.Level.Board).Length > 0;
            int comfort = cycle.Count(e => e.Plan!.Role == SlotRole.Comfort && Murky(e));
            int challenges = cycle.Count(e => e.Plan!.Role == SlotRole.Challenge && Murky(e));
            int hitos = cycle.Count(e => e.Plan!.Role == SlotRole.Milestone && Murky(e));
            if (rule.ComfortPerCycle is int per && start + 19 >= rule.FromLevel)
            {
                if (comfort > per)
                {
                    yield return new QaIssue("QA-MURKY-NORMAL", pack, start, string.Create(CultureInfo.InvariantCulture,
                        $"ciclo {start}-{start + 19}: {comfort} comodidades turbias (como mucho {per})"));
                }
                else if (comfort < per)
                {
                    yield return new QaIssue("QA-MURKY-NORMAL", pack, start, string.Create(CultureInfo.InvariantCulture,
                        $"ciclo {start}-{start + 19}: {comfort} comodidades turbias de {per} (alguna se publico sin turbias, T1)"), Warning: true);
                }
            }

            if (challenges > 2)
            {
                yield return new QaIssue("QA-MURKY-NORMAL", pack, start, string.Create(CultureInfo.InvariantCulture,
                    $"ciclo {start}-{start + 19}: {challenges} retos con turbias (como mucho 2)"));
            }

            int pair = (start - spec.CycleFrom) / 40;
            hitosByPair[pair] = hitosByPair.GetValueOrDefault(pair) + hitos;
            if (hitosByPair[pair] > 1)
            {
                yield return new QaIssue("QA-MURKY-NORMAL", pack, start, "hitos con turbias: como mucho 1 cada 2 ciclos (02 sec. 11, T1)");
            }
        }
    }

    /// <summary>
    /// Filas fijas (02 §5; D-030; 08 §4.1, fila rows): 1-3 filas; suman las cuias; 1 fila de 5 como mucho, 2 de 7 y 3 de 5; con 13
    /// cuias o más, 3 filas. Null si valen; si no, el motivo.
    /// </summary>
    public static string? RowsProblem(int[] rows, int cuias)
    {
        if (rows.Length is < 1 or > 3 || rows.Any(r => r < 1))
        {
            return "rows lleva de 1 a 3 filas de al menos 1 cuia";
        }

        if (rows.Sum() != cuias)
        {
            return string.Create(CultureInfo.InvariantCulture, $"rows suma {rows.Sum()} y hay {cuias} cuias");
        }

        int max = rows.Length == 2 ? 7 : 5;
        if (rows.Any(r => r > max))
        {
            return string.Create(CultureInfo.InvariantCulture, $"rows [{string.Join(", ", rows)}]: con {rows.Length} fila(s), {max} cuias por fila como mucho (02 sec. 5)");
        }

        return cuias >= 13 && rows.Length != 3 ? "con 13 cuias o mas, 3 filas (02 sec. 5; D-030)" : null;
    }

    /// <summary>Fila de cada cuia según <paramref name="rows"/> (null sin filas fijas).</summary>
    private static int[]? RowOf(int[]? rows, int cuias)
    {
        if (rows is null || rows.Sum() != cuias)
        {
            return null;
        }

        var row = new int[cuias];
        int i = 0;
        for (int r = 0; r < rows.Length; r++)
        {
            for (int k = 0; k < rows[r]; k++)
            {
                row[i++] = r;
            }
        }

        return row;
    }

    public static IEnumerable<QaIssue> Aguape(CampaignSpec spec, QaLevel lv)
    {
        QaIssue Fail(string msg) => new("QA-AGUAPE", lv.Pack, lv.Level, msg);
        bool any = lv.RawAttrs.Any(a => a?.Type == "aguape") || lv.Relief.Any(r => r.RawAttrs.Any(a => a?.Type == "aguape"));
        if (lv.Rows is int[] rows && RowsProblem(rows, lv.Board.Cuias.Length) is string bad)
        {
            yield return Fail(bad);
        }

        foreach (QaRelief r in lv.Relief.Where(r => r.Rows is not null))
        {
            if (RowsProblem(r.Rows!, r.Board.Cuias.Length) is string rb)
            {
                yield return Fail($"relief {r.Type}: {rb}");
            }
        }

        if (lv.Intro is "aguape" or "aguape_n2" && spec.F2Mechanics.FirstOrDefault(m => m.Intro == lv.Intro) is { } m && m.Debut != lv.Level)
        {
            yield return Fail($"intro_mechanic {lv.Intro} fuera de su estreno ({m.Debut})");
        }

        if (!any)
        {
            yield break;
        }

        if (lv.PendingTypes.Contains("aguape"))
        {
            yield return Fail("el nucleo de esta rama aun no conoce el Aguape (D-068, F2): no se puede comprobar que sella ni que no empieza atascado");
        }

        foreach (string msg in Groups(lv.Board.Cuias, lv.RawAttrs, lv.Rows, "nivel"))
        {
            yield return Fail(msg);
        }

        foreach (QaRelief r in lv.Relief)
        {
            foreach (string msg in Groups(r.Board.Cuias, r.RawAttrs, r.Rows, "relief " + r.Type))
            {
                yield return Fail(msg);
            }
        }

        if (lv.RawAttrs.Any(a => a?.Type is "tinaja" or "cucharon"))
        {
            yield return Fail("Aguape con la tinaja (02 sec. 9.2, regla 7)");
        }
    }

    /// <summary>Grupos del Aguapé de un tablero: tamaño, contigüidad, fila, Barro, espeso y rows con 11 cuias o más.</summary>
    private static IEnumerable<string> Groups(int[][] cuias, RawAttr?[] raw, int[]? rows, string where)
    {
        var groups = Enumerable.Range(0, raw.Length).Where(i => raw[i]?.Type == "aguape")
            .GroupBy(i => raw[i]!.Group ?? -1).OrderBy(g => g.Key).ToList();
        if (groups.Count == 0)
        {
            yield break;
        }

        if (cuias.Length >= 11 && rows is null)
        {
            yield return string.Create(CultureInfo.InvariantCulture, $"{where}: {cuias.Length} cuias con Aguape y sin rows (08 sec. 4.1)");
        }

        int[]? rowOf = RowOf(rows, cuias.Length);
        foreach (IGrouping<int, int> g in groups)
        {
            int[] idx = [.. g.Order()];
            string name = string.Create(CultureInfo.InvariantCulture, $"{where}: grupo {g.Key}");
            int n = idx.Select(i => raw[i]!.N ?? 1).Max();
            int min = n == 2 ? 4 : 2;
            if (g.Key < 0 || idx.Length < min || idx.Length > 5)
            {
                yield return string.Create(CultureInfo.InvariantCulture, $"{name} con {idx.Length} cuias (de {min} a 5{(n == 2 ? ", espeso" : "")})");
            }

            if (idx[^1] - idx[0] != idx.Length - 1)
            {
                yield return $"{name}: cuias no contiguas ({string.Join(", ", idx)})";
            }

            if (rowOf is not null && idx.Select(i => rowOf[i]).Distinct().Count() > 1)
            {
                yield return $"{name}: partido entre filas";
            }

            if (idx.Any(i => cuias[i].Contains(Cuia.MudColor)))
            {
                yield return $"{name}: con Barro (sin otra mecanica)";
            }

            if (idx.Select(i => raw[i]!.N ?? 1).Distinct().Count() > 1)
            {
                yield return $"{name}: n distinto en sus cuias";
            }
        }
    }

    public static IEnumerable<QaIssue> Lancha(QaLevel lv)
    {
        QaIssue Fail(string msg) => new("QA-LANCHA", lv.Pack, lv.Level, msg);
        int[] boats = Enumerable.Range(0, lv.RawAttrs.Length).Where(i => lv.RawAttrs[i]?.Type == "lancha").ToArray();
        if (lv.Intro == "lancha" && boats.Length == 0)
        {
            yield return Fail("intro_mechanic lancha y el nivel no la lleva");
        }

        if (boats.Length == 0)
        {
            yield break;
        }

        if (lv.PendingTypes.Contains("lancha"))
        {
            yield return Fail("el nucleo de esta rama aun no conoce la Lancha (D-068, F2): no se puede comprobar que atraca en la solucion");
        }

        if (boats.Length > 1)
        {
            yield return Fail(string.Create(CultureInfo.InvariantCulture, $"{boats.Length} lanchas (una por nivel)"));
        }

        int empties = Enumerable.Range(0, lv.Board.Cuias.Length).Count(i => lv.Board.Cuias[i].Length == 0 && lv.RawAttrs[i] is null);
        foreach (int b in boats)
        {
            int[] load = lv.Board.Cuias[b];
            if (load.Length is < 1 or > 3 || load.Contains(Cuia.MudColor) || lv.Board.Hidden.Any(h => h.Length > 0 && h[0] == b))
            {
                yield return Fail(string.Create(CultureInfo.InvariantCulture, $"carga de la lancha (cuia {b}): {load.Length} dosis; lleva 1-3 visibles y sin Barro"));
            }

            int k = lv.RawAttrs[b]!.K ?? 0;
            if (k < 2 || k < empties + 1)
            {
                yield return Fail(string.Create(CultureInfo.InvariantCulture, $"k = {k} con {empties} vacios iniciales (k >= 2 y >= vacios + 1)"));
            }
        }

        if (MechanicChecks.MurkyHidden(lv.Board).Length > 0)
        {
            yield return Fail("Lancha con Aguas turbias (03 sec. 17 #7)");
        }

        if (lv.Level < LanchaWithLianaFrom && lv.RawAttrs.Any(a => a?.Type is "liana" or "flor"))
        {
            yield return Fail(string.Create(CultureInfo.InvariantCulture, $"Lancha con la Liana antes del {LanchaWithLianaFrom} (03 sec. 11)"));
        }
    }
}
