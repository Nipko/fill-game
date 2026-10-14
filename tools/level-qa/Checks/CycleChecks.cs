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
        ("QA-LADDER", "escalera: 61+ D(p6) <= D(p9) <= D(p12) <= D(p15) <= D(p18) < D(p20); 41-59 D(p6) <= D(p12) <= D(p18) < D(p20), D(p9) < D(p6), D(p15) < D(p12) (c.3 regla 4); 21-40: los retos suaves por debajo del hito 40"),
        ("QA-REMANSO", "p19 es un remanso puro y mas facil que la media de comodidad: D(p19) < media de D de las comodidades del ciclo sin estreno (c.4; c.3 regla 5)"),
        ("QA-RAMP", "filas 11-12 de 02 sec. 13: cuias = 6 colores + 2 vacios, par 15-19 y 16-20, bot_fail 0-10 %"),
        ("QA-PURE-30", "al menos un 30 % de niveles puros por ciclo completo (d.2 regla 1)"),
        ("QA-WINDOW", "bot_fail en la ventana del slot (03 sec. 17 #3; 08 sec. 4.1): falla en recuperacion, comodidad, estreno (< 0,20), remanso y rampa; un reto o un hito fuera de su ventana es aviso (c.2: se publica con la etiqueta que mide)"),
        ("QA-DEBUT", "tableros de estreno con 2 vacios (tambien los de capacidad fuera de la rampa y del hito, 02 sec. 8) y un solo ejemplar de la mecanica; intro_mechanic y skippable = false solo en el nivel de intro (02 sec. 9.2 regla 2)"),
        ("QA-LABEL-ACCURACY", "aviso: acierto de etiqueta por ciclo < 60 % (c.7)"),
    ];

    /// <summary>
    /// El nivel lleva la mecánica o capacidad de su intro_mechanic. Distingue la tinaja (M13) de Aguas turbias y de las
    /// capacidades (propuesta de la tinaja c.5): «murky» pide ocultas fuera de la tinaja; «chica» y «alta», una Cuia chica o
    /// una Cuia alta que no sea la tinaja, su cuia ni la cueva de la Piraña; «tinaja», la tinaja (attrs).
    /// </summary>
    public static bool Presents(BoardData b, string intro) => intro switch
    {
        "murky" => MechanicChecks.MurkyHidden(b).Length > 0,
        "barro" => b.Cuias.Any(c => c.Contains(Cuia.MudColor)),
        "chica" => MechanicChecks.CapacityCaps(b).Any(c => c < Cuia.DefaultCapacity),
        "alta" => MechanicChecks.CapacityCaps(b).Any(c => c > Cuia.DefaultCapacity),
        _ => b.Attrs.Any(a => !a.IsNone && AF.Core.Mechanics.MechanicTypes.ToKey(a.Type) == intro),
    };

    /// <summary>
    /// Nivel puro: ni mecánicas (la tinaja con su cuia, las Aguas turbias y el Barro cuentan; MechanicChecks.Codes) ni
    /// capacidades de 02 §8.
    /// </summary>
    public static bool IsPure(BoardData b) =>
        b.Attrs.All(a => a.IsNone) && MechanicChecks.Codes(b).Count == 0 && !MechanicChecks.CapacityCaps(b).Any();

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

            // Ventana del slot (03 §17 #3 y 08 §4.1: un bot_fail fuera de la ventana del slot falla el CI; < 0,20 en los estrenos,
            // 02 §9.2 regla 2). La única excepción es la de c.2 de D-009: un reto o el hito fuera de su ventana se publica con la
            // etiqueta que mide (sin chip si mide menos del 75 %) y el generador lo registra; ahí queda en aviso
            // (LevelPlan.OutOfWindowAllowed). Las etiquetas por encima de lo permitido las detienen QA-LABEL-SLOT y QA-LABEL-EARLY.
            if (!plan.Window.Contains(lv.BotFail))
            {
                yield return new QaIssue("QA-WINDOW", lv.Pack, lv.Level,
                    string.Create(CultureInfo.InvariantCulture, $"bot_fail {lv.BotFail:0.000} fuera de la ventana {plan.Window} ({plan.RoleName})"),
                    Warning: plan.OutOfWindowAllowed);
            }

            // Estreno (02 §9.2 regla 2; 03 §9 consecuencia 3): 2 vacíos y un solo ejemplar. El estreno de una capacidad fuera de
            // la rampa y del hito (la Cuia chica de 3 en el 22, p2) también lleva 2 vacíos libres (02 §8: «la mecánica debuta en
            // 2-3 tableros fáciles»); su Cuia chica no cuenta como vacío, ni la tinaja y su cuia (propuesta de la tinaja c.3
            // punto 2: «2 vacíos sin contar la pareja»).
            bool mechanicDebut = plan.DebutBoard && plan.Mechanics.Count > 0 && plan.Role == SlotRole.Comfort;
            bool capacityDebut = plan.DebutBoard && plan.Mechanics.Count == 0 && !plan.Ramp && plan.Role is SlotRole.Recovery or SlotRole.Comfort;
            if (mechanicDebut || capacityDebut)
            {
                int empties = lv.Board.Cuias.Select((c, i) => (c, i)).Count(x => x.c.Length == 0 && lv.Board.Caps[x.i] >= Cuia.DefaultCapacity
                    && !MechanicChecks.IsTinajaPair(lv.Board.Attrs[x.i].Type));
                if (empties < 2)
                {
                    yield return Fail("QA-DEBUT", string.Create(CultureInfo.InvariantCulture, $"tablero de estreno con {empties} vacio(s); lleva 2"));
                }
            }

            if (mechanicDebut)
            {
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

            // Filas fijas de la tabla única del arranque (02 §13; especificación ramp.rows).
            if (plan.Row is RampRow row)
            {
                int empties = lv.Board.Cuias.Select((c, i) => (c, i)).Count(x => x.c.Length == 0 && lv.Board.Caps[x.i] == Cuia.DefaultCapacity && lv.Board.Attrs[x.i].IsNone);
                bool uniform = lv.Board.Caps.All(c => c == Cuia.DefaultCapacity) && lv.Board.Attrs.All(a => a.IsNone);
                if (lv.K != row.K || empties != row.Empties || lv.Board.Cuias.Length != row.K + row.Empties || !uniform)
                {
                    yield return Fail("QA-RAMP", string.Create(CultureInfo.InvariantCulture,
                        $"{lv.Board.Cuias.Length} cuias = {lv.K} colores + {empties} vacios; 02 sec. 13 fija {row.K + row.Empties} = {row.K} + {row.Empties} (cuias de 4, sin mecanicas)"));
                }

                if (!row.ParOk(lv.Par))
                {
                    yield return Fail("QA-RAMP", string.Create(CultureInfo.InvariantCulture, $"par {lv.Par} fuera de {row.ParMin}-{row.ParMax} (02 sec. 13)"));
                }

                if (!row.Window.Contains(lv.BotFail))
                {
                    yield return Fail("QA-RAMP", string.Create(CultureInfo.InvariantCulture, $"bot_fail {lv.BotFail:0.000} fuera de {row.Window} (02 sec. 13)"));
                }
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

            // Remanso más fácil que la media de comodidad (c.4), con las comodidades presentes del ciclo que no son estreno.
            List<double> comfort = cycle.Where(e => e.Plan!.IsComfortReference).Select(e => e.Level.D).ToList();
            if (at.TryGetValue(19, out QaLevel? remanso) && comfort.Count > 0 && !(remanso.D < comfort.Average() - 1e-9))
            {
                yield return new QaIssue("QA-REMANSO", remanso.Pack, remanso.Level, string.Create(CultureInfo.InvariantCulture,
                    $"D(p19) = {remanso.D:0.00} no queda por debajo de la media de comodidad del ciclo ({comfort.Average():0.00}, {comfort.Count} niveles sin estreno)"));
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
            // 21-40: retos suaves sin etiqueta y sin escalera, pero todos por debajo del hito 40, el primer Difícil (03 §7: «la
            // banda coloca»; c.3 regla 5: el pico tras el respiro).
            if (at.TryGetValue(20, out QaLevel? first))
            {
                foreach (int p in new[] { 6, 9, 12, 15, 18 })
                {
                    if (at.TryGetValue(p, out QaLevel? soft) && !(soft.D < first.D - 1e-9))
                    {
                        yield return new QaIssue("QA-LADDER", soft.Pack, soft.Level, string.Create(CultureInfo.InvariantCulture,
                            $"reto suave p{p}: D = {soft.D:0.00} no queda por debajo del hito {first.Level} (D = {first.D:0.00})"));
                    }
                }
            }

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
