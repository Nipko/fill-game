// Comprobaciones base (P-7): GDD 03 §17 #1, #2, #4, #5, #11 (y #10 con el --check del generador); 08 §4.1 «Validación en
// tools/level-qa»; D-007 Q16 B (trampas tempranas con tope por etiqueta: 0 en Normal, 1 en Difícil, sin tope en Muy
// difícil). Todo se recalcula con AF.Core y AF.Solver sobre el JSON publicado.
using System.Globalization;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class BaseChecks
{
    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-STRUCT", "caps, cuias y attrs de la misma longitud; <= 14 cuias generadas (02 R1; 08 sec. 4.1)"),
        ("QA-UNITS", "U_c coherente con units_per_color y con las capacidades (02 R3, sec. 8; 03 sec. 17 #5)"),
        ("QA-SEALED", "ninguna cuia sellada al inicio (03 sec. 17 #5)"),
        ("QA-STUCK", "atascado(S0) = false (02 sec. 9.2 regla 1)"),
        ("QA-VALID", "resto de LevelValidation de AF.Core (mecanicas, Barro, Aguas turbias)"),
        ("QA-PAR", "par recalculado (A* w = 1, run) igual al publicado; par >= 2K; dentro del par objetivo del rango +-30 % (03 sec. 17 #1-#2)"),
        ("QA-TINT-DEBUT", "cada tinte con debut anterior o igual al nivel (03 sec. 17 #4)"),
        ("QA-KMAX", "K <= k_max y >= k_min de la cabecera (03 sec. 17 #4)"),
        ("QA-RISK", "risk_pairs recalculado igual al publicado y <= max_risk_pairs (03 sec. 4.5)"),
        ("QA-PALETTE", "palette = tintes presentes en cuias (08 sec. 4.1)"),
        ("QA-BOT", "bot_fail recalculado con la semilla del nivel identico al publicado (03 sec. 6)"),
        ("QA-LABEL", "etiqueta coherente: >= 0,75 hard y >= 0,90 very_hard, desde el 40 y el 60 (03 sec. 7, 11)"),
        ("QA-LABEL-EARLY", "ningun nivel mide >= 0,90 antes del 60 ni >= 0,75 antes del 40, aunque se publique con la etiqueta inferior (contrato 6; 03 sec. 11-12)"),
        ("QA-CUIAS", "aviso: cuias del nivel dentro de las cuias totales del rango (03 sec. 11)"),
        ("QA-FIELDS", "sin solution, safe_path ni qa (08 sec. 4.1)"),
        ("QA-SIZE", "pack <= 100 KB por 100 niveles sin relief (03 sec. 17 #11)"),
        ("QA-EARLY-TRAPS", "trampas tempranas dentro del tope de la etiqueta (Q16 B)"),
    ];

    public static IEnumerable<QaIssue> Run(QaContext ctx, LevelEval e)
    {
        QaLevel lv = e.Level;
        string pack = lv.Pack;
        int n = lv.Level;
        QaIssue Fail(string id, string msg) => new(id, pack, n, msg);
        BoardData b = lv.Board;

        if (b.Caps.Length != b.Cuias.Length || b.Attrs.Length != b.Cuias.Length)
        {
            yield return Fail("QA-STRUCT", "caps, cuias y attrs de distinta longitud");
        }

        if (b.Cuias.Length > 14)
        {
            yield return Fail("QA-STRUCT", string.Create(CultureInfo.InvariantCulture, $"{b.Cuias.Length} cuias (como mucho 14 generadas)"));
        }

        foreach (string f in lv.ForbiddenKeys)
        {
            yield return Fail("QA-FIELDS", $"el nivel publica '{f}'");
        }

        if (e.BuildError is not null)
        {
            string id = e.BuildError.Contains("units_per_color", StringComparison.Ordinal) ? "QA-UNITS" : "QA-STRUCT";
            yield return Fail(id, "BoardBuilder: " + e.BuildError);
            yield break;
        }

        BoardState state = e.State!;
        for (int i = 0; i < state.Count; i++)
        {
            if (state.IsSealed(i))
            {
                yield return Fail("QA-SEALED", string.Create(CultureInfo.InvariantCulture, $"la cuia {i} empieza sellada"));
            }
        }

        if (PourRules.IsStuck(state, true))
        {
            yield return Fail("QA-STUCK", "el nivel empieza en Aguas estancadas");
        }

        if (e.ValidationError is string v && !v.Contains("empieza sellada", StringComparison.Ordinal) && !v.Contains("Aguas estancadas", StringComparison.Ordinal))
        {
            string id = v.StartsWith("02 R3", StringComparison.Ordinal) || v.StartsWith("02 §8", StringComparison.Ordinal) ? "QA-UNITS" : "QA-VALID";
            yield return Fail(id, "LevelValidation: " + v);
        }

        // Par (03 §17 #1-#2).
        ParResult par = e.Par!;
        if (par.Status != SolveStatus.Solved)
        {
            yield return Fail("QA-PAR", $"A* no resuelve el nivel ({par.Status})");
        }
        else if (par.Par != lv.Par)
        {
            yield return Fail("QA-PAR", string.Create(CultureInfo.InvariantCulture, $"par publicado {lv.Par} y recalculado {par.Par}"));
        }

        if (lv.Par < 2 * lv.K)
        {
            yield return Fail("QA-PAR", string.Create(CultureInfo.InvariantCulture, $"par {lv.Par} < 2K = {2 * lv.K}"));
        }

        if (e.Plan is LevelPlan plan)
        {
            RangeInfo r = plan.Range;
            double lo = r.ParMin * 0.7;
            double hi = r.ParMax * 1.3;
            if (lv.Par < lo - 1e-9 || lv.Par > hi + 1e-9)
            {
                yield return Fail("QA-PAR", string.Create(CultureInfo.InvariantCulture, $"par {lv.Par} fuera del objetivo {r.ParMin}-{r.ParMax} +-30 % del rango {r.Name}"));
            }
        }

        // Paleta (03 §17 #4).
        CampaignSpec spec = ctx.Spec;
        var present = state.Count > 0
            ? Enumerable.Range(Cuia.MinColor, Cuia.MaxTintColor).Where(c => state.UnitsOf(c) > 0).ToHashSet()
            : [];
        var paletteEngines = new HashSet<int>();
        foreach (string id in lv.Palette)
        {
            TintInfo? t = spec.Tints.FirstOrDefault(x => x.Id == id);
            if (t is null)
            {
                yield return Fail("QA-PALETTE", $"tinte desconocido '{id}'");
                continue;
            }

            paletteEngines.Add(t.Engine);
            if (t.Debut > n)
            {
                yield return Fail("QA-TINT-DEBUT", string.Create(CultureInfo.InvariantCulture, $"{id} debuta en el {t.Debut}"));
            }
        }

        if (!paletteEngines.SetEquals(present))
        {
            yield return Fail("QA-PALETTE", "palette no coincide con los tintes de cuias");
        }

        QaPack? header = ctx.Packs.FirstOrDefault(p => p.Path == pack);
        if (header is not null && (lv.K > header.KMax || lv.K < header.KMin))
        {
            yield return Fail("QA-KMAX", string.Create(CultureInfo.InvariantCulture, $"K = {lv.K} fuera de k_min-k_max = {header.KMin}-{header.KMax}"));
        }

        int risk = spec.RiskPairs.Count(p => paletteEngines.Contains(p.A) && paletteEngines.Contains(p.B));
        if (risk != lv.RiskPairs)
        {
            yield return Fail("QA-RISK", string.Create(CultureInfo.InvariantCulture, $"risk_pairs publicado {lv.RiskPairs} y recalculado {risk}"));
        }

        int max = header?.MaxRiskPairs ?? int.MaxValue;
        if (e.Plan is LevelPlan p2)
        {
            max = Math.Min(max, p2.MaxRiskPairs);
        }

        if (risk > max)
        {
            yield return Fail("QA-RISK", string.Create(CultureInfo.InvariantCulture, $"{risk} pares de riesgo (como mucho {max})"));
        }

        // Bot y etiqueta (03 §6-§7).
        if (!Evaluator.Same3(e.Bot, lv.BotFail))
        {
            yield return Fail("QA-BOT", string.Create(CultureInfo.InvariantCulture, $"bot_fail publicado {lv.BotFail:0.000} y recalculado {e.Bot:0.000}"));
        }

        string expected = spec.MeasuredLabel(n, lv.BotFail);
        if (expected != lv.Label)
        {
            yield return Fail("QA-LABEL", string.Create(CultureInfo.InvariantCulture, $"etiqueta {lv.Label} con bot_fail {lv.BotFail:0.000}; corresponde {expected}"));
        }

        if (spec.EarlyMeasure(n, lv.BotFail) is string early)
        {
            yield return Fail("QA-LABEL-EARLY", string.Create(CultureInfo.InvariantCulture,
                $"{early} (contrato 6: primer Dificil en el {spec.FirstHard} y primer Muy dificil en el {spec.FirstVeryHard})"));
        }

        // Cuias por rango (03 §11). Aviso: con 1 vacío y el K que permiten los pares de riesgo (03 §4.5) algunos rangos no
        // llegan al mínimo de la tabla; se revisa en la integración (k_plan, vacíos o la tabla).
        if (e.Plan is LevelPlan rp && (b.Cuias.Length < rp.Range.CuiasMin || b.Cuias.Length > rp.Range.CuiasMax))
        {
            yield return new QaIssue("QA-CUIAS", pack, n, string.Create(CultureInfo.InvariantCulture,
                $"{b.Cuias.Length} cuias (K = {lv.K}); el rango {rp.Range.Name} pide {rp.Range.CuiasMin}-{rp.Range.CuiasMax} (03 sec. 11)"), Warning: true);
        }

        // Trampas tempranas (Q16 B).
        int cap = lv.Label switch { "normal" => 0, "hard" => 1, _ => int.MaxValue };
        if (e.EarlyTraps > cap)
        {
            yield return Fail("QA-EARLY-TRAPS", string.Create(CultureInfo.InvariantCulture, $"{e.EarlyTraps} trampas tempranas en un nivel {lv.Label} (tope {cap})"));
        }
    }

    public static IEnumerable<QaIssue> RunPack(QaPack pack)
    {
        // Proyección a 100 niveles: la cabecera una vez y los niveles a su tamaño medio (un pack pequeño no penaliza).
        double per100 = pack.HeaderBytes + (100.0 * (pack.BytesWithoutRelief - pack.HeaderBytes) / Math.Max(1, pack.Levels.Count));
        if (per100 > 100 * 1024)
        {
            yield return new QaIssue("QA-SIZE", pack.Path, 0, string.Create(CultureInfo.InvariantCulture, $"{per100 / 1024:0.0} KB por 100 niveles sin relief (como mucho 100 KB)"));
        }
    }
}
