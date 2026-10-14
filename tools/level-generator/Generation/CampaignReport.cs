// Informe de la generación de la campaña (tools/out/campaign_v1_report.md). Sin tiempos ni fechas: dos ejecuciones con
// la misma especificación y los mismos --levels dan los mismos bytes (los tiempos van a la consola).
// Tinaja (D-019; propuesta de la tinaja c.3 punto 6): Aguas turbias («Tu») son las ocultas fuera de la tinaja, la tinaja y su
// cuia no cuentan como Cuia alta ni Cuia chica, y la sección «Tinaja» (solo si algún nivel la lleva) separa los rechazos
// «sin vertido seguro» de los de «presupuesto agotado».
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AF.Core.Board;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal static class CampaignReport
{
    public static string Build(
        CampaignSpec spec, List<GeneratedLevel> levels, Dictionary<RangeInfo, RangeStats> stats,
        List<(WorldInfo World, string Path, byte[] Bytes, int Levels)> packs, string levelsArg)
    {
        var sb = new StringBuilder();
        void W(string line = "") => sb.Append(line).Append('\n');
        string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);

        string specPath = spec.Path.Replace('\\', '/');
        string outDir = packs.Count == 0 ? "." : (Path.GetDirectoryName(packs[0].Path) ?? ".").Replace('\\', '/');
        int packSchema = PackWriter.PackSchemaFor(levels.Select(l => new PackLevel(l.Plan, l.Chosen!, l.Relief)).ToList());
        bool tinaja = levels.Any(l => l.Chosen!.IsTinaja) || spec.MechanicTable.Any(m => m.Code == "Ti");
        W(F($"# Campaña {spec.FirstLevel}-{spec.LastLevel}: informe de `level-generator --spec`"));
        W();
        W(F($"Generado por `dotnet run --project tools/level-generator -c Release -- --spec {specPath} --levels {levelsArg} --out {outDir} --report tools/out/{spec.SpecId}_report.md`: no se edita a mano y no lleva tiempos. Generador {PackWriter.GeneratorVersion}, `pack_schema` {packSchema}, modo de vertido `run` (D-005)."));
        W();
        W("Fuentes: D-009 (ciclo escalonado) y D-007 (debuts) con la propuesta `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` "
          + "(c.1-c.4, d.1-d.4); GDD 03 §2, §4, §7-§13 y §17; 02 §8-§11; 08 §4.1. Todo se mide con `AF.Core` y `AF.Solver`: A* w = 1 a 300.000 "
          + "(par), bot ×40 con la semilla del nivel (`bot_fail` y etiqueta), `EarlyTraps` (trampas tempranas, Q16 B), ruta de referencia "
          + "para ramificación y trampas (`--parity`), `SafePath` en Aguas turbias y `LevelValidation` en cada tablero.");
        W();
        if (tinaja)
        {
            W("Tinaja (M13; D-019, Q-Ti1 A; 03 §8; propuesta de la tinaja c.3): paso «Ti» de la superposición, búsquedas sin poda de "
              + "fútiles (`SolverOptions.ForLevel`), y en cada candidato que se mide `SafePath`, paseos seguros y casuales, «S0 sin "
              + "trampas» en el estreno y la compuerta de coste de Morpho (`TinajaWalks`); ver la sección «Tinaja».");
            W();
        }

        W("## Paquetes");
        W();
        W("| Pack | Niveles | Bytes | Bytes sin relief | Bytes sin relief por 100 niveles (≤ 100 KB, 03 §17 #11) | SHA-256 |");
        W("|---|---|---|---|---|---|");
        foreach ((WorldInfo world, string path, byte[] bytes, int count) in packs)
        {
            int noRelief = NoReliefBytes(spec, world, levels);
            W(F($"| `{path.Replace('\\', '/')}` | {count} | {bytes.Length} | {noRelief} | {(100.0 * noRelief / count / 1024):0.0} KB | `{Convert.ToHexStringLower(SHA256.HashData(bytes))}` |"));
        }

        W();
        W("## Pool por rango e índice D (03 §7)");
        W();
        W("D = 0,35·z(bot_fail) + 0,25·z(par − 2K) + 0,20·z(ramificación) + 0,10·z(trampas) + 0,10·[E libres = 1], con media y desviación del pool del rango "
          + "(todos los candidatos en ventana de sus slots; congelados tras la primera pasada). Banda = cuantil de D en el pool: recuperación ≤ 0,10, "
          + "comodidad ≤ 0,70, reto ≤ 0,95, hito > 0,95.");
        W();
        W("| Rango | Slots | Pool | Pool / slots | bot_fail μ ± σ | par − 2K μ ± σ | Ramif. μ ± σ | Trampas μ ± σ |");
        W("|---|---|---|---|---|---|---|---|");
        foreach (RangeStats s in stats.Values.OrderBy(s => s.Range.First))
        {
            W(F($"| {s.Range.Name} | {s.Slots} | {s.PoolSize} | {(double)s.PoolSize / s.Slots:0.0} | {s.Bot.Mean:0.00} ± {s.Bot.Sd:0.00} | {s.ParExcess.Mean:0.0} ± {s.ParExcess.Sd:0.0} | {s.Branching.Mean:0.00} ± {s.Branching.Sd:0.00} | {s.Traps.Mean:0.00} ± {s.Traps.Sd:0.00} |"));
        }

        W();
        W("## Etiquetas por rango frente a 03 §11");
        W();
        W("| Rango (niveles generados) | Difícil | Muy difícil | 03 §11 | Aviso (> 5 puntos) |");
        W("|---|---|---|---|---|");
        foreach (RangeInfo r in spec.Ranges)
        {
            List<GeneratedLevel> inRange = levels.Where(l => l.Plan.Range == r).ToList();
            if (inRange.Count == 0)
            {
                continue;
            }

            int hard = inRange.Count(l => l.Chosen!.MeasuredLabel == "hard");
            int vhard = inRange.Count(l => l.Chosen!.MeasuredLabel == "very_hard");
            double ph = 100.0 * hard / inRange.Count;
            double pv = 100.0 * vhard / inRange.Count;
            bool warn = ph < r.PctHardMin - 5 || ph > r.PctHardMax + 5 || pv < r.PctVeryHardMin - 5 || pv > r.PctVeryHardMax + 5;
            W(F($"| {r.Name} ({inRange.Count}) | {hard} ({ph:0.0} %) | {vhard} ({pv:0.0} %) | {r.PctHardMin}-{r.PctHardMax} % / {r.PctVeryHardMin} % | {(warn ? "**aviso**" : "—")} |"));
        }

        W();
        W("Nota: con D-009 desde el 61 los cinco retos apuntan a Difícil y el hito a Muy difícil (25 % y 5 % por ciclo), por encima del 15 % de 03 §11 "
          + "para 61-100: es la cuenta del ciclo, no un error de la generación [P: la tabla de 03 §11 se revisa en la integración].");
        W();
        W("## Ciclos");
        W();
        W("| Ciclo | Temporada | Puros | Difícil | Muy difícil | Objetivo D / MD | Acierto de etiqueta (c.7) | Trampas tempranas | Relajaciones |");
        W("|---|---|---|---|---|---|---|---|---|");
        foreach (IGrouping<string, GeneratedLevel> g in levels.GroupBy(l => CycleName(spec, l.Plan)))
        {
            List<GeneratedLevel> ls = g.ToList();
            int pure = ls.Count(l => l.Plan.Cell.IsPure);
            int hard = ls.Count(l => l.Chosen!.MeasuredLabel == "hard");
            int vhard = ls.Count(l => l.Chosen!.MeasuredLabel == "very_hard");
            int targetD = ls.Count(l => l.Plan.TargetLabel == "hard");
            int targetMD = ls.Count(l => l.Plan.TargetLabel == "very_hard");
            List<GeneratedLevel> targeted = ls.Where(l => l.Plan.TargetLabel is not null).ToList();
            int hit = targeted.Count(l => CampaignSpec.LabelRank(l.Chosen!.MeasuredLabel) >= CampaignSpec.LabelRank(l.Plan.TargetLabel));
            string accuracy = targeted.Count == 0 ? "—" : F($"{hit}/{targeted.Count} ({100.0 * hit / targeted.Count:0} %){(100 * hit < 60 * targeted.Count ? " **< 60 %**" : "")}");
            int traps = ls.Sum(l => l.Chosen!.EarlyTraps);
            int relaxed = ls.Count(l => l.Chosen!.Config.Stage > 0 || !l.Chosen!.InWindow);
            W(F($"| {g.Key} | {ls[0].Plan.Season} | {pure}/{ls.Count} | {hard} | {vhard} | {targetD} / {targetMD} | {accuracy} | {traps} | {relaxed} |"));
        }

        W();
        W("## Niveles");
        W();
        W("Columnas: p = posición en el ciclo (0 en la rampa); E = vacíos de capacidad ≥ 4 (con la Hoja o el Matapi vacíos), libres = sin mecánica; "
          + "q = cuantil de D en el pool del rango (3 decimales: la banda de hito empieza por encima de 0,95) y su banda; TT = trampas tempranas; alivio = tipos publicados en `relief` en orden (− = minus_color, + = plus_empty).");
        W();
        W("| Nivel | p | Rol | Calendario | K | E (libres) | Cuias | Fuente | Par | Estados A* | bot_fail | Ventana | Etiqueta | Objetivo | D | q (banda) | Ramif. | Trampas | TT | Compl. | Alivio | Notas |");
        W("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (GeneratedLevel l in levels)
        {
            Candidate c = l.Chosen!;
            LevelPlan p = l.Plan;
            var notes = new List<string>(c.Config.Notes);
            notes.AddRange(l.Notes);
            if (!c.InWindow)
            {
                notes.Add("fuera de ventana (relajacion agotada)");
            }

            if (l.Slot.MurkyRounds > 0)
            {
                notes.Add(F($"{l.Slot.MurkyRounds} ronda(s) extra de candidatos turbios"));
            }

            if (l.Slot.MurkyDropped)
            {
                notes.Add("sin Aguas turbias (T1)");
            }

            if (c.Tints.RiskAdjacent > 0)
            {
                notes.Add(F($"risk_adjacent: {c.Tints.RiskAdjacent} contacto(s)"));
            }

            if (l.Rounds > 0)
            {
                notes.Add(F($"{l.Rounds} ronda(s) extra de candidatos"));
            }

            if (l.BandRounds > 0)
            {
                notes.Add(F($"{l.BandRounds} ronda(s) de banda"));
            }

            if (c.Murky)
            {
                notes.Add(F($"turbia: {c.Board.Hidden.Length} dosis ocultas en {c.Board.Hidden.Select(h => h[0]).Distinct().Count()} cuia(s), {c.MaxRealities} realidades"));
            }

            if (c.TinajaCheck is TinajaCheckResult t)
            {
                notes.Add(F($"tinaja: {t.Doses} dosis ({t.Doses - 1} ocultas), {t.MaxRealities} realidades"));
            }

            foreach (ReliefBoard r in l.ReliefRejected)
            {
                notes.Add($"alivio {r.Type} no publicado ({r.Note})");
            }

            string relief = l.Relief.Count == 0 ? "—" : string.Join(" ", l.Relief.Select(r => r.Type == "minus_color" ? F($"−{r.Removed}({r.Par}; {r.BotFail:0.00})") : F($"+({r.BotFail:0.00})")));
            int band = Pool.BandIndex(c.Quantile);
            W(F($"| {p.Level} | {p.Position} | {p.RoleName} | {p.Cell.Short} | {c.K} | {c.Empties} ({c.FreeEmpties}) | {c.Board.Caps.Length} | {c.Source} | {c.Par} | {c.Expanded} | {c.Bot:0.000} | {p.Window} | {c.MeasuredLabel} | {p.TargetLabel ?? "—"} | {c.D:0.00} | {c.Quantile:0.000} ({Pool.BandName(band)}) | {c.Branching:0.00} | {c.Traps:0.00} | {c.EarlyTraps} | {c.Complexity} | {relief} | {string.Join("; ", notes)} |"));
        }

        W();
        W("## Par / 2K por fuente frente a 03 §18");
        W();
        W("| Fuente | Niveles | Par / 2K medio | bot_fail medio | 03 §18 (par / 2K) |");
        W("|---|---|---|---|---|");
        foreach (IGrouping<string, GeneratedLevel> g in levels.GroupBy(l => l.Chosen!.Source).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            double ratio = g.Average(l => l.Chosen!.Par / (2.0 * l.Chosen!.K));
            double bot = g.Average(l => l.Chosen!.Bot);
            string proto = g.Key switch { "walk" => "1,09", "deep" => "1,17", _ => "1,53" };
            W(F($"| {g.Key} | {g.Count()} | {ratio:0.00} | {bot:0.00} | {proto} |"));
        }

        W();
        W("## Debuts");
        W();
        W("| Nivel | intro_mechanic | skippable | Celda |");
        W("|---|---|---|---|");
        foreach (GeneratedLevel l in levels.Where(l => l.Plan.Intro is not null))
        {
            W($"| {l.Plan.Level} | `{l.Plan.Intro}` | false | {l.Plan.Cell.Short} |");
        }

        W();
        W("## Apariciones en los packs (debut real y huecos)");
        W();
        W("Sobre los tableros publicados, no sobre el calendario: primera aparición (debut real), niveles en los que sale, hueco "
          + F($"máximo entre dos apariciones y niveles desde la última hasta el {spec.LastLevel} (resumen, punto 6; d.2 regla 4: ≤ {SpecValidator.MaxGap}; ")
          + "level-qa lo exige con QA-GAP-120). Las capacidades cuentan también cuando el generador las usa como forma de tablero (02 §8); "
          + "la cueva de la Piraña no cuenta como Cuia chica.");
        W();
        W("| Mecánica o capacidad | Debut del calendario | Debut real | Niveles | Hueco máximo | Hasta el final |");
        W("|---|---|---|---|---|---|");
        foreach ((string name, int? debut, List<int> seen) in Appearances(spec, levels))
        {
            int best = 0;
            for (int i = 1; i < seen.Count; i++)
            {
                best = seen[i] - seen[i - 1] > seen[best] - seen[Math.Max(0, best - 1)] ? i : best;
            }

            string gap = best == 0 ? "—" : F($"{seen[best] - seen[best - 1]} ({seen[best - 1]}→{seen[best]})");

            string tail = seen.Count == 0 ? "—" : F($"{levels[^1].Plan.Level - seen[^1]}");
            string first = seen.Count == 0 ? "**no sale**" : seen[0].ToString(CultureInfo.InvariantCulture);
            W(F($"| {name} | {(debut is int d ? d.ToString(CultureInfo.InvariantCulture) : "—")} | {first} | {seen.Count} | {gap} | {tail} |"));
        }

        W();
        W("## Remanso frente a la comodidad del ciclo (c.4)");
        W();
        W("p19 es «puro, más fácil que la media de comodidad» (c.4; c.3 regla 5: respiro antes del pico): la colocación exige D(p19) "
          + "< media de D de las comodidades del ciclo que no son tableros de estreno, y level-qa lo comprueba (QA-REMANSO).");
        W();
        W("| Ciclo | Remanso | D | bot_fail | Par | Comodidades | D medio | bot_fail medio | Puesto del remanso por D (de las comodidades y él) |");
        W("|---|---|---|---|---|---|---|---|---|");
        foreach (IGrouping<string, GeneratedLevel> g in levels.Where(l => !l.Plan.Ramp).GroupBy(l => CycleName(spec, l.Plan)))
        {
            GeneratedLevel? rem = g.FirstOrDefault(l => l.Plan.Role == SlotRole.Remanso);
            List<GeneratedLevel> comfort = g.Where(l => l.Plan.IsComfortReference).ToList();
            if (rem is null || comfort.Count == 0)
            {
                continue;
            }

            double dRem = Placement.Rounded(rem.Chosen!.D);
            int rank = 1 + comfort.Count(l => Placement.Rounded(l.Chosen!.D) > dRem + 1e-9);
            W(F($"| {g.Key} | {rem.Plan.Level} | {dRem:0.00} | {rem.Chosen!.Bot:0.000} | {rem.Chosen!.Par} | {comfort.Count} | {comfort.Average(l => Placement.Rounded(l.Chosen!.D)):0.00} | {comfort.Average(l => l.Chosen!.Bot):0.000} | {rank} de {comfort.Count + 1} (1 = el más difícil) |"));
        }

        W();
        W("## Banda medida por rol (03 §7: «la banda coloca»)");
        W();
        W("La colocación exige, con el cuantil q de D en el pool del rango (recuperación ≤ 0,10, comodidad ≤ 0,70, reto ≤ 0,95, "
          + "hito > 0,95): comodidad, estreno y remanso como mucho en banda de comodidad; recuperación (p1-p2, c.3 regla 3) en banda de "
          + "recuperación o, como mínimo, con D por debajo de la comodidad más fácil del ciclo; hito (p20, «Pico» de c.3) en banda de "
          + "hito o, como mínimo, con par − 2K ≥ la media del pool del rango; y en 21-40 los retos suaves por debajo del hito 40. "
          + "level-qa lo comprueba regenerando la campaña (QA-BAND-Q y QA-LADDER).");
        W();
        W("| Ciclo | Recuperación p1 / p2: q (D) | Comodidad más fácil (D) | q máximo de comodidad y estreno | Remanso: q | Retos: D mín.-máx. | Hito: q (banda) · par − 2K / media del pool |");
        W("|---|---|---|---|---|---|---|");
        foreach (IGrouping<string, GeneratedLevel> g in levels.Where(l => !l.Plan.Ramp).GroupBy(l => CycleName(spec, l.Plan)))
        {
            List<GeneratedLevel> ls = g.ToList();
            string Rec(int p) => ls.FirstOrDefault(l => l.Plan.Position == p) is GeneratedLevel r
                ? F($"{r.Chosen!.Quantile:0.000} ({Placement.Rounded(r.Chosen!.D):0.00})")
                : "—";
            List<GeneratedLevel> comfortRefs = ls.Where(l => l.Plan.IsComfortReference).ToList();
            string min = comfortRefs.Count == 0 ? "—" : F($"{comfortRefs.Min(l => Placement.Rounded(l.Chosen!.D)):0.00}");
            List<GeneratedLevel> comfort = ls.Where(l => l.Plan.Role == SlotRole.Comfort).ToList();
            string comfortMax = comfort.Count == 0 ? "—" : F($"{comfort.Max(l => l.Chosen!.Quantile):0.000}");
            string rem = ls.FirstOrDefault(l => l.Plan.Role == SlotRole.Remanso) is GeneratedLevel rm ? F($"{rm.Chosen!.Quantile:0.000}") : "—";
            List<GeneratedLevel> ch = ls.Where(l => l.Plan.Role == SlotRole.Challenge).ToList();
            string challenges = ch.Count == 0 ? "—" : F($"{ch.Min(l => Placement.Rounded(l.Chosen!.D)):0.00} / {ch.Max(l => Placement.Rounded(l.Chosen!.D)):0.00}");
            string hito = "—";
            if (ls.FirstOrDefault(l => l.Plan.Role == SlotRole.Milestone) is GeneratedLevel h)
            {
                Candidate c = h.Chosen!;
                hito = F($"{c.Quantile:0.000} ({Pool.BandName(Pool.BandIndex(c.Quantile))}) · {c.Par - (2 * c.K)} / {stats[h.Plan.Range].ParExcess.Mean:0.0}");
            }

            W(F($"| {g.Key} | {Rec(1)} / {Rec(2)} | {min} | {comfortMax} | {rem} | {challenges} | {hito} |"));
        }

        var minimumOnly = new List<string>();
        foreach (GeneratedLevel l in levels.Where(l => !l.Plan.Ramp))
        {
            int band = Pool.BandIndex(l.Chosen!.Quantile);
            if ((l.Plan.Role == SlotRole.Recovery && band > 0) || (l.Plan.Role == SlotRole.Milestone && band < 3))
            {
                minimumOnly.Add(F($"{l.Plan.Level} ({l.Plan.RoleName}, q {l.Chosen!.Quantile:0.000})"));
            }
        }

        W();
        W(minimumOnly.Count == 0
            ? "Todas las recuperaciones en banda de recuperación y todos los hitos en banda de hito."
            : "Fuera de la banda de su rol, con el mínimo cumplido (aviso QA-BAND-Q): " + string.Join("; ", minimumOnly) + ".");
        W();
        W("## Vacíos de la comodidad y del remanso por rango (03 §4.4 y §11)");
        W();
        W("Comodidades sin estreno y remansos. «2 vacíos» = dos o más vacíos libres (sin mecánica, capacidad ≥ 4); «Cuia chica» = Cuia chica de 2 "
          + "añadida como medio vacío por la relajación de 03 §4.4. Canon: 2 vacíos en 11-30; 1 desde el 31 (03 §11: «2 en respiros y recuperación» en "
          + "31-60, «2 en 30 % de comodidad» en 61-100 y 1 en 101-200; 03 §4.4: «1 (31+) con 2 en el 30 % de los slots») [P: se revisa con el tech-lead].");
        W();
        W("| Rango | Comodidades y remansos | Con 2 vacíos | Con Cuia chica de relajación | Relajados (%) | Canon |");
        W("|---|---|---|---|---|---|");
        foreach (RangeInfo r in spec.Ranges)
        {
            List<GeneratedLevel> rel = levels.Where(l => l.Plan.Range == r && (l.Plan.IsComfortReference || l.Plan.Role == SlotRole.Remanso)).ToList();
            if (rel.Count == 0)
            {
                continue;
            }

            int two = rel.Count(l => l.Chosen!.FreeEmpties >= 2);
            int chica = rel.Count(l => l.Chosen!.Config.Notes.Any(n => n.Contains("Cuia chica de 2 como medio vacio", StringComparison.Ordinal)));
            string canon = r.First switch
            {
                <= 30 => "2",
                <= 60 => "1 (2 en respiros)",
                <= 100 => "1 (2 en el 30 %)",
                _ => "1",
            };
            W(F($"| {r.Name} | {rel.Count} | {two} | {chica} | {100.0 * (two + chica) / rel.Count:0} % | {canon} |"));
        }

        W();
        W("## Cuias por rango frente a 03 §11");
        W();
        W("Con 1 vacío, el K del rango que permiten los pares de riesgo (03 §4.5; `k_plan`) no siempre llega al mínimo de cuias de 03 §11 "
          + "(aviso QA-CUIAS de level-qa) [P: k_plan, vacíos o la tabla se revisan en la integración].");
        W();
        W("| Rango | 03 §11 | Mínimo y máximo generados | Por debajo | Por encima | Niveles por debajo |");
        W("|---|---|---|---|---|---|");
        foreach (RangeInfo r in spec.Ranges)
        {
            List<GeneratedLevel> inRange = levels.Where(l => l.Plan.Range == r).ToList();
            if (inRange.Count == 0)
            {
                continue;
            }

            List<GeneratedLevel> below = inRange.Where(l => l.Chosen!.Board.Caps.Length < r.CuiasMin).ToList();
            int above = inRange.Count(l => l.Chosen!.Board.Caps.Length > r.CuiasMax);
            string list = below.Count == 0 ? "—" : string.Join(", ", below.Select(l => F($"{l.Plan.Level} ({l.Chosen!.Board.Caps.Length})")));
            W(F($"| {r.Name} | {r.CuiasMin}-{r.CuiasMax} | {inRange.Min(l => l.Chosen!.Board.Caps.Length)}-{inRange.Max(l => l.Chosen!.Board.Caps.Length)} | {below.Count} | {above} | {list} |"));
        }

        W();
        W("## Saltos de banda (03 §12: +2,2 / −3,4)");
        W();
        var jumps = new List<string>();
        for (int i = 1; i < levels.Count; i++)
        {
            string? v = Placement.JumpViolation(levels[i - 1].Chosen, levels[i].Chosen!);
            if (v is not null)
            {
                jumps.Add(F($"{levels[i - 1].Plan.Level}→{levels[i].Plan.Level}: {v}"));
            }
        }

        W(jumps.Count == 0 ? "Ninguno fuera del límite." : "Fuera del límite (blando, registrado): " + string.Join("; ", jumps) + ".");
        if (tinaja)
        {
            TinajaSection(levels, W);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Sección de la tinaja (propuesta de la tinaja c.3 puntos 3 y 6; c.8): por nivel con tinaja, sus filtros y los candidatos
    /// rechazados por causa («sin vertido seguro» frente a «presupuesto agotado»); y los niveles en los que no puede ir.
    /// </summary>
    private static void TinajaSection(List<GeneratedLevel> levels, Action<string> w)
    {
        void W(string line = "") => w(line);
        string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
        W();
        W("## Tinaja");
        W();
        W(F($"Filtros de `TinajaWalks` sobre el tablero publicado (03 §8): ruta segura (`SafePath`), {TinajaWalks.SafeWalkCount} paseos seguros ")
            + F($"(todos tienen que ganar), {TinajaWalks.CasualWalkCount} paseos casuales (ganados / perdidos por error / de adivinar / sin terminar en ")
            + F($"{TinajaWalks.MaxSteps} vertidos; ninguno puede ser de adivinar), «S0 sin trampas» en el estreno (vertidos útiles de S0 que no son ")
            + F($"seguros; tiene que ser 0) y la compuerta de coste de Morpho: w = {TinajaWalks.MorphoWeight} y {TinajaWalks.MorphoRealityBudget} nodos ")
            + F($"por realidad desde cero en S0, en la ruta y en los paseos, con un tope de {TinajaWalks.MorphoNodeCap} nodos por consulta ")
            + F($"({TinajaWalks.HintTimeoutMs} ms × {TinajaWalks.PcNodesPerMs} nodos/ms del PC [E] ÷ factor {TinajaWalks.MorphoDeviceFactor}× del teléfono [P], hasta PF1). ")
            + "Todas las búsquedas van sin poda de fútiles. Dosis por tipo de nivel (D-019, Q-Ti1 A): 3 en estreno, comodidad y práctica; "
            + "4-5 en retos; 5 en el hito; vacíos sin contar la pareja: 2 en estreno y comodidad (p3-p5), 1 en práctica, retos e hito [P].");
        W();
        W("| Nivel | p | Rol | Dosis (ocultas) | Realidades | Ruta segura | Paseos seguros | Casuales (g / p / a / s) | S0 sin trampas | Morpho: estados, nodos máx. | Candidatos rechazados por la tinaja |");
        W("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (GeneratedLevel l in levels.Where(l => l.Chosen!.IsTinaja))
        {
            TinajaCheckResult? t = l.Chosen!.TinajaCheck;
            var rejected = l.Slot.Candidates.Where(c => c.TinajaCheck?.Kind is not null)
                .GroupBy(c => c.TinajaCheck!.Kind!).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => F($"{g.Key}: {g.Count()}")).ToList();
            string why = rejected.Count == 0 ? "—" : string.Join("; ", rejected);
            string traps = t is null || t.S0Traps < 0 ? "—" : F($"{t.S0Traps}");
            W(t is null
                ? F($"| {l.Plan.Level} | {l.Plan.Position} | {l.Plan.RoleName} | sin medir | — | — | — | — | — | — | {why} |")
                : F($"| {l.Plan.Level} | {l.Plan.Position} | {l.Plan.RoleName} | {t.Doses} ({t.Doses - 1}) | {t.MaxRealities} | {t.RoutePours} vertidos | {t.SafeWalksWon}/{t.SafeWalks} | {t.CasualWon} / {t.CasualLost} / {t.CasualGuess} / {t.CasualOpen} | {traps} | {t.MorphoStates}, {t.MorphoMaxNodes} | {why} |"));
        }

        List<GeneratedLevel> forbidden = levels.Where(l => l.Chosen!.IsTinaja && Superposition.TinajaDoses(l.Plan) is null).ToList();
        List<GeneratedLevel> dropped = levels.Where(l => l.Plan.Cell.Has("Ti") && !l.Chosen!.IsTinaja).ToList();
        W();
        W(forbidden.Count == 0
            ? "Ningún nivel con tinaja en recuperación (p1-p2), remanso (p19) ni la rampa (D-019, Q-Ti1 A)."
            : "**Tinaja en un nivel puro** (D-019, Q-Ti1 A): " + string.Join(", ", forbidden.Select(l => l.Plan.Level.ToString(CultureInfo.InvariantCulture))) + ".");
        if (dropped.Count > 0)
        {
            W();
            W("**Celdas con tinaja publicadas sin ella:** " + string.Join(", ", dropped.Select(l => l.Plan.Level.ToString(CultureInfo.InvariantCulture))) + ".");
        }

        W();
        W(F($"Alivio (03 §13): `minus_color` nunca quita una fruta de la tinaja y las variantes repiten la ruta segura y los paseos (con las opciones de Morpho: w = {TinajaWalks.MorphoWeight} y {TinajaWalks.MorphoRealityBudget} nodos por realidad [P]) con la máscara de la tinaja, que el pack escribe en su `hidden` (`pack_schema` {PackWriter.TinajaPackSchema}; 08 §4.1: `relief_board` admite `hidden` si y solo si su `attrs` contiene la tinaja)."));
    }

    /// <summary>Niveles en los que sale cada mecánica (por código del calendario) y cada capacidad, sobre los tableros elegidos.</summary>
    private static List<(string Name, int? Debut, List<int> Seen)> Appearances(CampaignSpec spec, List<GeneratedLevel> levels)
    {
        var rows = new List<(string, int?, List<int>)>();
        foreach (MechanicInfo m in spec.MechanicTable)
        {
            rows.Add((F2($"{m.Id} ({m.Code})"), m.Debut, levels.Where(l => Has(l.Chosen!.Board, m.Code)).Select(l => l.Plan.Level).ToList()));
        }

        rows.Add(("Cuia chica (cap 1-3)", spec.CapacityTable.Where(c => c.Id == "chica").Select(c => (int?)c.Debut).Min(),
            levels.Where(l => Caps(l.Chosen!.Board).Any(c => c < Cuia.DefaultCapacity)).Select(l => l.Plan.Level).ToList()));
        rows.Add(("Cuia alta (cap 5-6)", spec.CapacityTable.Where(c => c.Id == "alta").Select(c => (int?)c.Debut).Min(),
            levels.Where(l => Caps(l.Chosen!.Board).Any(c => c > Cuia.DefaultCapacity)).Select(l => l.Plan.Level).ToList()));
        foreach (CapacityInfo cap in spec.CapacityTable.Where(c => c.Code is not ("Ch" or "Al")))
        {
            rows.Add((F2($"{cap.Code} (cap {cap.Cap})"), cap.Debut, levels.Where(l => Caps(l.Chosen!.Board).Contains(cap.Cap)).Select(l => l.Plan.Level).ToList()));
        }

        return rows;

        static string F2(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Capacidades de forma de tablero: sin la cueva de la Piraña ni la tinaja y su cuia (no son Cuia alta ni chica).</summary>
    private static IEnumerable<int> Caps(BoardData b) =>
        b.Caps.Where((c, i) => c != Cuia.DefaultCapacity && b.Attrs[i].Type is not (MechanicType.Pirana or MechanicType.Tinaja or MechanicType.Cucharon));

    private static bool Has(BoardData b, string code) => code switch
    {
        "Tu" => b.Hidden.Any(h => b.Attrs[h[0]].Type != MechanicType.Tinaja),
        "Ti" => b.Attrs.Any(a => a.Type == MechanicType.Tinaja),
        "Ba" => b.Cuias.Any(c => c.Contains(Cuia.MudColor)),
        _ => b.Attrs.Any(a => a.Type == code switch
        {
            "Ho" => MechanicType.Hoja,
            "Br" => MechanicType.Brote,
            "Li" => MechanicType.Liana,
            "Ma" => MechanicType.Matapi,
            "Re" => MechanicType.Remolino,
            "Ll" => MechanicType.Lluvia,
            "Pi" => MechanicType.Pirana,
            _ => MechanicType.None,
        } && a.Type != MechanicType.None),
    };

    private static string CycleName(CampaignSpec spec, LevelPlan p)
    {
        if (p.Ramp)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{spec.RampFirst}-{spec.RampLast} (rampa)");
        }

        int start = spec.CycleFrom + (((p.Level - spec.CycleFrom) / 20) * 20);
        return string.Create(CultureInfo.InvariantCulture, $"{start}-{start + 19}");
    }

    private static int NoReliefBytes(CampaignSpec spec, WorldInfo world, List<GeneratedLevel> levels)
    {
        List<PackLevel> inWorld = levels.Where(l => l.Plan.World == world).Select(l => new PackLevel(l.Plan, l.Chosen!, [])).ToList();
        return PackWriter.Bytes(PackWriter.Write(spec, world, inWorld)).Length;
    }
}
