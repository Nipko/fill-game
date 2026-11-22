// Comprobaciones de la curva 2 de la 1.1 (D-068; 03 §17, comprobaciones 16-19; 08 §4.1 «Validación en tools/level-qa»):
//   QA-T-ROLE       T (cuias) y K (colores) dentro del tope de su papel (role_caps de la especificación; 03 §12; cúspide solo en
//                   la firma del mundo); en un pack kind curve, replaces = el id de la curva 1 del mismo level y mundo, e id y
//                   seed con el sufijo de la curva; difficulty.pressure, si está, igual al que dan las palancas del tablero (aviso).
//   QA-MURKY-NORMAL un nivel Normal con Aguas turbias: comodidad (nunca recuperación ni remanso), 1 cuia turbia con como mucho
//                   murky_normal.max_hidden_units (2) dosis ocultas, desde murky_normal.from_level; por ciclo, como mucho
//                   comfort_per_cycle comodidades turbias (aviso si hay menos: el generador puede publicar sin turbias por T1),
//                   2 retos turbios y 1 hito turbio cada 2 ciclos (02 §11, T1).
//   QA-AGUAPE       cada group con 2-5 cuias contiguas, sin Barro; el espeso (n = 2) con 4-5; con 11 cuias o más, rows
//                   obligatorio; rows suma las cuias, con los topes de 02 §5; intro aguape/aguape_n2 solo en su estreno; nunca con la
//                   tinaja. Con el núcleo (F2): LevelValidation.ValidateRows en el nivel y en cada variante de relief (con rows o,
//                   con 10 cuias o menos, las filas fijas de 02 §5), una cuia del grupo (dos con frutas distintas en el espeso) que
//                   sella solo recibiendo, atascado(S0) = false y cada grupo se suelta en la solución del par (03 §17 #17).
//   QA-LANCHA       una lancha por nivel; carga de 1-3 dosis visibles, sin Barro; k >= vacíos iniciales + 1 (toda cuia sin dosis,
//                   como el núcleo); nunca con Aguas turbias; con la Liana solo desde el 401; nunca en un hito ni en la cúspide
//                   (T >= K + k + 1 no cabe en su tope; D-068, preguntas de F3 y F3b); intro lancha solo en su estreno; con
//                   el núcleo, atascado(S0) = false y la lancha atraca en la solución del par (03 §17 #18).
//   QA-BOT-ATENTO   aviso (informe; 03 §17 #20): dentro de cada mundo de la curva 2, bot_atento(hito n+1) >= bot_atento(hito n),
//                   con el bot atento ×40 (AF.Solver.Bot.AttentiveBot, semilla propia del nivel). Nunca decide la etiqueta.
// PackReader rechaza el Aguapé, la Lancha, rows y pressure con un pack_schema menor que 5.
// La comprobación de par de la curva 2 (QA-PAR: solo par >= 2K) y las trampas tempranas (QA-EARLY-TRAPS: 0 en Normal y 1 en
// Difícil) viven en BaseChecks y valen igual aquí.
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
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
        ("QA-AGUAPE", "grupos de 2-5 cuias contiguas sin Barro (espeso n = 2: 4-5); rows con >= 11 cuias, que suma las cuias y respeta 02 sec. 5; LevelValidation.ValidateRows en el nivel y en cada relief; una cuia que sella solo recibiendo (dos en el espeso); atascado(S0) = false; el grupo se suelta en la solucion; estreno solo en su nivel; nunca con la tinaja (03 sec. 17 #17)"),
        ("QA-LANCHA", "una lancha por nivel; carga de 1-3 dosis visibles sin Barro; k >= vacios + 1; sin Aguas turbias; con la Liana solo desde el 401; nunca en un hito ni en la cuspide (D-068); estreno solo en su nivel; atascado(S0) = false y la lancha atraca en la solucion (03 sec. 17 #18)"),
        ("QA-BOT-ATENTO", "aviso: dentro de cada mundo de la curva 2, bot atento x40 del hito n+1 >= el del hito n (03 sec. 17 #20; solo informe)"),
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
            .Concat(AguapeCore(e))
            .Concat(Lancha(ctx.Spec, e.Level))
            .Concat(LanchaCore(e));
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
        if (e.Plan is not { Curve: > 0 } plan)
        {
            yield break;
        }

        QaIssue Fail(string msg) => new("QA-MURKY-NORMAL", lv.Pack, lv.Level, msg);
        MurkyNormalRule? rule = spec.MurkyNormal;
        int debutLevel = rule is { Debut.Count: > 0 } ? rule.Debut[0] : -1;
        if (lv.Intro == "murky" && lv.Level != debutLevel)
        {
            yield return Fail(string.Create(CultureInfo.InvariantCulture,
                $"intro_mechanic murky fuera del estreno de las turbias en Normal ({(debutLevel < 0 ? "sin murky_normal.debut" : debutLevel.ToString(CultureInfo.InvariantCulture))})"));
        }

        int[][] murky = MechanicChecks.MurkyHidden(lv.Board);
        if (murky.Length == 0)
        {
            yield break;
        }

        int cuias = murky.Select(h => h.Length > 0 ? h[0] : -1).Distinct().Count();

        // Retos e hitos turbios de la curva 2 (02 §11, T1): 2-4 dosis en <= 2 cuias y 3-4 dosis en <= 3 cuias.
        if (lv.Label != "normal")
        {
            (int min, int maxCuias) = plan.Role == SlotRole.Milestone ? (3, 3) : (2, 2);
            if (plan.Role is SlotRole.Challenge or SlotRole.Milestone && (murky.Length < min || murky.Length > 4 || cuias > maxCuias))
            {
                yield return Fail(string.Create(CultureInfo.InvariantCulture,
                    $"{murky.Length} dosis ocultas en {cuias} cuias en un {plan.RoleName}; la curva 2 pide {min}-4 dosis en {maxCuias} cuias como mucho (02 sec. 11, T1)"));
            }

            yield break;
        }

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

        if (cuias != 1 || murky.Length > rule.MaxHiddenUnits)
        {
            yield return Fail(string.Create(CultureInfo.InvariantCulture,
                $"{murky.Length} dosis ocultas en {cuias} cuias; en Normal, 1 cuia con como mucho {rule.MaxHiddenUnits}"));
        }

        if (rule.Debut.Contains(lv.Level) && murky.Length != 1)
        {
            yield return Fail(string.Create(CultureInfo.InvariantCulture,
                $"estreno de las turbias en Normal con {murky.Length} dosis ocultas; lleva 1 (02 sec. 11, T1)"));
        }
    }

    /// <summary>
    /// Cabecera de un pack con niveles de la curva 2: kind curve (o reserve), pack_schema 5 y pack_id curva_{mm}_v{v} (o
    /// reserva_curva_{mm}_v{v}); y un pack curve solo con niveles de la curva 2 (QA-T-ROLE, también con --no-reproduce).
    /// </summary>
    public static IEnumerable<QaIssue> RunPack(CampaignSpec spec, QaPack pack)
    {
        QaIssue Fail(string msg) => new("QA-T-ROLE", pack.Path, 0, msg);
        bool curveLevels = pack.Levels.Any(l => spec.IsCurveLevel(l.Level) && spec.Calendar.ContainsKey(l.Level));
        WorldInfo? world = spec.Worlds.FirstOrDefault(w => w.World == pack.World);
        if (curveLevels && pack.Kind is not ("curve" or "reserve"))
        {
            yield return Fail($"niveles de la curva 2 en un pack de kind {pack.Kind} (se espera curve)");
        }

        if (pack.Kind == "curve" || (curveLevels && pack.Kind == "reserve"))
        {
            if (pack.PackSchema != PackReader.CurvePackSchema)
            {
                yield return Fail(string.Create(CultureInfo.InvariantCulture, $"pack_schema {pack.PackSchema}; la curva 2 va con el {PackReader.CurvePackSchema}"));
            }

            string? expected = world is null ? null : pack.Kind == "curve" ? PackWriter.CurvePackId(world, spec.Lap) : ReserveRunner.PackIdFor(spec, world);
            if (expected is not null && pack.PackId != expected)
            {
                yield return Fail($"pack_id {pack.PackId}; se espera {expected}");
            }
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
                else if (comfort < 2)
                {
                    // 02 §11, T1: «en 2-3 niveles por ciclo»; menos de 2 falla (el dueño pidió 3: F1-1).
                    yield return new QaIssue("QA-MURKY-NORMAL", pack, start, string.Create(CultureInfo.InvariantCulture,
                        $"ciclo {start}-{start + 19}: {comfort} comodidades turbias (02 sec. 11, T1: 2-3 por ciclo)"));
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

        if (lv.Intro is "aguape" or "aguape_n2" && spec.MechanicTable.FirstOrDefault(m => m.Id == lv.Intro) is { } m && m.Debut != lv.Level)
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

        if (lv.RawAttrs.Concat(lv.Relief.SelectMany(r => r.RawAttrs)).Any(a => a?.Type is "tinaja" or "cucharon"))
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

    public static IEnumerable<QaIssue> Lancha(CampaignSpec spec, QaLevel lv)
    {
        QaIssue Fail(string msg) => new("QA-LANCHA", lv.Pack, lv.Level, msg);
        int[] boats = Enumerable.Range(0, lv.RawAttrs.Length).Where(i => lv.RawAttrs[i]?.Type == "lancha").ToArray();
        if (lv.Intro == "lancha" && boats.Length == 0)
        {
            yield return Fail("intro_mechanic lancha y el nivel no la lleva");
        }

        if (lv.Intro == "lancha" && spec.MechanicTable.FirstOrDefault(m => m.Id == "lancha") is { } debut && debut.Debut != lv.Level)
        {
            yield return Fail(string.Create(CultureInfo.InvariantCulture, $"intro_mechanic lancha fuera de su estreno ({debut.Debut})"));
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

        foreach (int b in boats)
        {
            // Vacías para k: toda cuia sin dosis menos la lancha, de cualquier capacidad y mecánica (como el núcleo; 02 §9.1 M11).
            int empties = Enumerable.Range(0, lv.Board.Cuias.Length).Count(i => i != b && lv.Board.Cuias[i].Length == 0);
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

        if (spec.Curve is { } curve && lv.Level >= curve.FromLevel && lv.Level <= spec.LastLevel && spec.Plan(lv.Level).Role == SlotRole.Milestone)
        {
            yield return Fail("Lancha en un hito o en la cuspide: necesita T >= K + k + 1 y no cabe en su tope (D-068)");
        }
    }

    /// <summary>
    /// QA-AGUAPE con el núcleo (03 §17 #17): LevelValidation.ValidateRows en el nivel y en cada variante de relief (con sus rows o,
    /// con 10 cuias o menos, las filas fijas de 02 §5: CurveBoards.LayoutRows), una cuia de cada grupo que sella solo recibiendo
    /// (dos con frutas distintas en el espeso), atascado(S0) = false y cada grupo se suelta en la solución del par.
    /// </summary>
    public static IEnumerable<QaIssue> AguapeCore(LevelEval e)
    {
        QaLevel lv = e.Level;
        if (e.State is not BoardState state || !Enumerable.Range(0, state.Count).Any(i => state[i].Mechanic == MechanicType.Aguape))
        {
            yield break;
        }

        QaIssue Fail(string msg) => new("QA-AGUAPE", lv.Pack, lv.Level, msg);
        if (RowsCheck(state, lv.Rows) is string bad)
        {
            yield return Fail("nivel: " + bad);
        }

        foreach (ReliefEval r in e.Relief.Where(r => r.State is not null))
        {
            if (RowsCheck(r.State!, r.Relief.Rows) is string rb)
            {
                yield return Fail($"relief {r.Relief.Type}: {rb}");
            }
        }

        foreach (IGrouping<int, int> g in Enumerable.Range(0, state.Count).Where(i => state[i].Mechanic == MechanicType.Aguape)
            .GroupBy(i => state[i].MechanicPairId).OrderBy(g => g.Key))
        {
            int[] masks = [.. g.Select(i => CurveMechanics.SealableByReceiving(state, i)).Where(m => m != 0)];
            bool thick = state[g.First()].MechanicN == CurveMechanics.AguapeThickSeals;
            bool ok = thick
                ? Enumerable.Range(0, masks.Length).Any(a => Enumerable.Range(a + 1, masks.Length - a - 1)
                    .Any(b => masks[a] != masks[b] || (masks[a] & (masks[a] - 1)) != 0))
                : masks.Length >= 1;
            if (!ok)
            {
                yield return Fail(string.Create(CultureInfo.InvariantCulture,
                    $"grupo {g.Key}: {(thick ? "no hay dos cuias que sellen solo recibiendo con frutas distintas" : "ninguna cuia sella solo recibiendo")} (02 sec. 9.1 M8, regla 6)"));
            }
        }

        if (PourRules.IsStuck(state, true))
        {
            yield return Fail("el nivel empieza atascado (02 sec. 9.2 regla 1)");
        }

        if (e.Par is { Status: AF.Solver.Search.SolveStatus.Solved } par)
        {
            if (CurveBoards.Replay(state, par.Solution, out string? error) is not (int[] unreleased, _))
            {
                yield return Fail("no se puede repetir la solucion del par: " + error);
            }
            else if (unreleased.Length > 0)
            {
                yield return Fail($"el grupo {string.Join(", ", unreleased)} no se suelta en la solucion del par");
            }
        }
    }

    /// <summary>LevelValidation.ValidateRows con las filas del tablero (rows o las fijas de 02 §5); null si valen o si no se saben.</summary>
    private static string? RowsCheck(BoardState state, int[]? rows)
    {
        int[]? layout = rows ?? CurveBoards.LayoutRows(state.Count, aguape: false);
        return layout is null ? null : LevelValidation.ValidateRows(state, layout);
    }

    /// <summary>QA-LANCHA con el núcleo (03 §17 #18): atascado(S0) = false y la lancha atraca en la solución del par.</summary>
    public static IEnumerable<QaIssue> LanchaCore(LevelEval e)
    {
        QaLevel lv = e.Level;
        if (e.State is not BoardState state || !Enumerable.Range(0, state.Count).Any(i => state[i].Mechanic == MechanicType.Lancha))
        {
            yield break;
        }

        QaIssue Fail(string msg) => new("QA-LANCHA", lv.Pack, lv.Level, msg);
        if (PourRules.IsStuck(state, true))
        {
            yield return Fail("el nivel empieza atascado (02 sec. 9.2 regla 1)");
        }

        if (e.Par is { Status: AF.Solver.Search.SolveStatus.Solved } par)
        {
            if (CurveBoards.Replay(state, par.Solution, out string? error) is not (_, bool docked))
            {
                yield return Fail("no se puede repetir la solucion del par: " + error);
            }
            else if (!docked)
            {
                yield return Fail("la lancha no atraca en la solucion del par");
            }
        }
    }

    /// <summary>
    /// QA-BOT-ATENTO (aviso; 03 §17 #20): en cada mundo de la curva 2, los hitos en orden con el bot atento que no baja.
    /// </summary>
    public static IEnumerable<QaIssue> AttentiveLadder(QaContext ctx)
    {
        foreach (IGrouping<int, LevelEval> world in ctx.Levels
            .Where(e => e.Plan is { Curve: > 0, Role: SlotRole.Milestone, Reserve: 0 } && e.Attentive is not null && ctx.Packs.Any(p => p.Path == e.Level.Pack && p.Kind == "curve"))
            .GroupBy(e => e.Plan!.World.World).OrderBy(g => g.Key))
        {
            LevelEval? previous = null;
            foreach (LevelEval e in world.OrderBy(e => e.Level.Level))
            {
                if (previous is not null && e.Attentive < previous.Attentive - 1e-9)
                {
                    yield return new QaIssue("QA-BOT-ATENTO", e.Level.Pack, e.Level.Level, string.Create(CultureInfo.InvariantCulture,
                        $"bot atento {e.Attentive * 100:0} % en el hito {e.Level.Level}, por debajo del hito {previous.Level.Level} ({previous.Attentive * 100:0} %): el pico no crece (para la curacion)"), Warning: true);
                }

                previous = e;
            }
        }
    }
}
