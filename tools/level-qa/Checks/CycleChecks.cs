// Ciclo de 20 de D-009 (P-8): propuesta c.2-c.7 (reglas del escalón y acierto de etiqueta), 03 §12-§13 y §17 #3.
// Con p = ((n − 1) mod 20) + 1 y la especificación de la campaña (tools/level-generator/specs/campaign_v1.json).
// Las comprobaciones entre niveles solo miran los niveles presentes en los paquetes (los fixtures traen pocos).
using System.Globalization;
using AF.Core.Board;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class CycleChecks
{
    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-BAND", "difficulty.band igual al rol de la posicion (03 sec. 12; remanso = comfort)"),
        ("QA-LABEL-SLOT", "etiquetas solo donde se permiten (41-59: p6, p12, p18; 61+: retos e hito; 40: hito) (D-009)"),
        ("QA-LABEL-ADJ", "nunca dos niveles con etiqueta seguidos (c.3 regla 1)"),
        ("QA-AFTER-LABEL", "tras cada etiqueta, el siguiente con D menor (condicion necesaria de bajar una banda; c.3 regla 2)"),
        ("QA-LADDER", "escalera: 61+ D(p6) <= D(p9) <= D(p12) <= D(p15) <= D(p18) < D(p20); 41-59 D(p6) <= D(p12) <= D(p18) < D(p20), D(p9) < D(p6), D(p15) < D(p12) (c.3 regla 4)"),
        ("QA-REMANSO", "p19 es un remanso puro y precede al hito; p18 es un reto (c.3 regla 5)"),
        ("QA-PURE-30", "al menos un 30 % de niveles puros por ciclo completo (d.2 regla 1)"),
        ("QA-WINDOW", "bot_fail < 0,20 en los tableros de estreno (03 sec. 17 #3); fuera de la ventana en otro slot, aviso (relajacion registrada por el generador)"),
        ("QA-DEBUT", "tableros de estreno con 2 vacios y un solo ejemplar de la mecanica; intro_mechanic y skippable = false solo en el nivel de intro (02 sec. 9.2 regla 2)"),
        ("QA-LABEL-ACCURACY", "aviso: acierto de etiqueta por ciclo < 60 % (c.7)"),
    ];

    /// <summary>El nivel lleva la mecánica o capacidad de su intro_mechanic.</summary>
    public static bool Presents(BoardData b, string intro) => intro switch
    {
        "murky" => b.Hidden.Length > 0,
        "barro" => b.Cuias.Any(c => c.Contains(Cuia.MudColor)),
        "chica" => b.Caps.Where((c, i) => b.Attrs[i].Type != AF.Core.Mechanics.MechanicType.Pirana).Any(c => c < Cuia.DefaultCapacity),
        "alta" => b.Caps.Any(c => c > Cuia.DefaultCapacity),
        _ => b.Attrs.Any(a => !a.IsNone && AF.Core.Mechanics.MechanicTypes.ToKey(a.Type) == intro),
    };

    public static bool IsPure(BoardData b) =>
        b.Attrs.All(a => a.IsNone) && b.Hidden.Length == 0 && b.Caps.All(c => c == Cuia.DefaultCapacity)
        && b.Cuias.All(c => !c.Contains(Cuia.MudColor));

    public static IEnumerable<QaIssue> Run(QaContext ctx)
    {
        CampaignSpec spec = ctx.Spec;
        var byLevel = ctx.Levels.GroupBy(e => e.Level.Level).ToDictionary(g => g.Key, g => g.First());
        foreach (LevelEval e in ctx.Levels)
        {
            QaLevel lv = e.Level;
            LevelPlan? plan = e.Plan;
            if (plan is null)
            {
                yield return new QaIssue("QA-BAND", lv.Pack, lv.Level, "nivel fuera de la especificacion de la campana");
                continue;
            }

            QaIssue Fail(string id, string msg) => new(id, lv.Pack, lv.Level, msg);
            if (lv.Band != plan.Band)
            {
                yield return Fail("QA-BAND", $"band {lv.Band}; la posicion p{plan.Position} es {plan.Band}");
            }

            if (CampaignSpec.LabelRank(lv.Label) > CampaignSpec.LabelRank(plan.AllowedLabel))
            {
                yield return Fail("QA-LABEL-SLOT", $"etiqueta {lv.Label} en un slot que admite {plan.AllowedLabel ?? "ninguna"}");
            }

            // Ventana del slot (03 §17 #3): obligatoria en los tableros de estreno (< 0,20, 02 §9.2 regla 2); en el resto es el
            // objetivo del generador, que registra su relajación (03 §4.4; c.2: un reto que mide menos del 75 % se publica sin
            // etiqueta), así que aquí es un aviso. Las etiquetas por encima de lo permitido las detiene QA-LABEL-SLOT.
            if (!plan.Window.Contains(lv.BotFail))
            {
                bool debut = plan.DebutBoard && plan.Role == SlotRole.Comfort;
                yield return new QaIssue("QA-WINDOW", lv.Pack, lv.Level,
                    string.Create(CultureInfo.InvariantCulture, $"bot_fail {lv.BotFail:0.000} fuera de la ventana {plan.Window} ({plan.RoleName})"),
                    Warning: !debut);
            }

            // Estreno (02 §9.2 regla 2; 03 §9 consecuencia 3).
            if (plan.DebutBoard && plan.Mechanics.Count > 0 && plan.Role == SlotRole.Comfort)
            {
                int empties = lv.Board.Cuias.Select((c, i) => (c, i)).Count(x => x.c.Length == 0 && lv.Board.Caps[x.i] >= Cuia.DefaultCapacity);
                if (empties < 2)
                {
                    yield return Fail("QA-DEBUT", string.Create(CultureInfo.InvariantCulture, $"tablero de estreno con {empties} vacio(s); lleva 2"));
                }

                int instances = MechanicChecks.Instances(lv.Board);
                if (instances != 1)
                {
                    yield return Fail("QA-DEBUT", string.Create(CultureInfo.InvariantCulture, $"tablero de estreno con {instances} ejemplares de mecanica; lleva uno"));
                }
            }

            if (plan.Intro != lv.Intro)
            {
                yield return Fail("QA-DEBUT", $"intro_mechanic {lv.Intro ?? "null"}; corresponde {plan.Intro ?? "null"}");
            }

            if (lv.Intro is string intro && !Presents(lv.Board, intro))
            {
                yield return Fail("QA-DEBUT", $"intro_mechanic {intro} y el nivel no lo lleva");
            }

            if (lv.Skippable != (plan.Intro is null))
            {
                yield return Fail("QA-DEBUT", $"skippable = {(lv.Skippable ? "true" : "false")}; es false solo en el nivel de intro");
            }

            // Remanso (p19).
            if (plan.Role == SlotRole.Remanso && !IsPure(lv.Board))
            {
                yield return Fail("QA-REMANSO", "el remanso (p19) lleva mecanicas o capacidades");
            }

            // Vecinos.
            if (byLevel.TryGetValue(lv.Level + 1, out LevelEval? next) && lv.Label != "normal")
            {
                if (next.Level.Label != "normal")
                {
                    yield return Fail("QA-LABEL-ADJ", string.Create(CultureInfo.InvariantCulture, $"{lv.Level} ({lv.Label}) y {lv.Level + 1} ({next.Level.Label}) llevan etiqueta seguidos"));
                }

                if (!(next.Level.D < lv.D - 1e-9))
                {
                    yield return Fail("QA-AFTER-LABEL", string.Create(CultureInfo.InvariantCulture, $"tras la etiqueta, D({lv.Level + 1}) = {next.Level.D:0.00} no baja de D({lv.Level}) = {lv.D:0.00}"));
                }
            }
        }

        // Escalera y ciclos completos.
        foreach (IGrouping<int, LevelEval> cycle in ctx.Levels.Where(e => e.Plan is { Ramp: false }).GroupBy(e => (e.Level.Level - spec.CycleFrom) / 20))
        {
            int start = spec.CycleFrom + (cycle.Key * 20);
            var at = cycle.GroupBy(e => e.Plan!.Position).ToDictionary(g => g.Key, g => g.First().Level);
            foreach (QaIssue i in Ladder(spec, start, at))
            {
                yield return i;
            }

            if (at.Count == 20)
            {
                int pure = at.Values.Count(l => IsPure(l.Board));
                if (pure * 10 < 60)
                {
                    yield return new QaIssue("QA-PURE-30", at[1].Pack, start, string.Create(CultureInfo.InvariantCulture, $"ciclo {start}-{start + 19}: {pure}/20 niveles puros (< 30 %)"));
                }

                // Acierto de etiqueta (c.7): aviso si baja del 60 %.
                var targeted = cycle.Where(e => e.Plan!.TargetLabel is not null).ToList();
                int hit = targeted.Count(e => CampaignSpec.LabelRank(e.Level.Label) >= CampaignSpec.LabelRank(e.Plan!.TargetLabel));
                if (targeted.Count > 0 && hit * 10 < targeted.Count * 6)
                {
                    yield return new QaIssue("QA-LABEL-ACCURACY", at[1].Pack, start, string.Create(CultureInfo.InvariantCulture, $"ciclo {start}-{start + 19}: acierto de etiqueta {hit}/{targeted.Count} (< 60 %)"), Warning: true);
                }
            }
        }
    }

    private static IEnumerable<QaIssue> Ladder(CampaignSpec spec, int start, Dictionary<int, QaLevel> at)
    {
        if (start + 19 <= spec.FirstHard)
        {
            yield break;
        }

        int[] ladder = start < spec.FirstVeryHard ? spec.Labeled4159 : [6, 9, 12, 15, 18];
        int? prev = null;
        foreach (int p in ladder)
        {
            if (!at.TryGetValue(p, out QaLevel? cur))
            {
                continue;
            }

            if (prev is int q && at[q].D > cur.D + 1e-9)
            {
                yield return new QaIssue("QA-LADDER", cur.Pack, cur.Level, string.Create(CultureInfo.InvariantCulture, $"D(p{p}) = {cur.D:0.00} < D(p{q}) = {at[q].D:0.00}: la escalera baja"));
            }

            prev = p;
        }

        if (prev is int last && at.TryGetValue(20, out QaLevel? hito) && !(hito.D > at[last].D + 1e-9))
        {
            yield return new QaIssue("QA-LADDER", hito.Pack, hito.Level, string.Create(CultureInfo.InvariantCulture, $"D(p20) = {hito.D:0.00} no supera D(p{last}) = {at[last].D:0.00}"));
        }

        if (start < spec.FirstVeryHard)
        {
            foreach ((int soft, int labeled) in new[] { (9, 6), (15, 12) })
            {
                if (at.TryGetValue(soft, out QaLevel? s) && at.TryGetValue(labeled, out QaLevel? l) && !(s.D < l.D - 1e-9))
                {
                    yield return new QaIssue("QA-LADDER", s.Pack, s.Level, string.Create(CultureInfo.InvariantCulture, $"reto suave p{soft}: D = {s.D:0.00} no queda por debajo de D(p{labeled}) = {l.D:0.00}"));
                }
            }
        }
    }
}
