// Informe de la generación de la campaña (tools/out/campaign_v1_report.md). Sin tiempos ni fechas: dos ejecuciones con
// la misma especificación y los mismos --levels dan los mismos bytes (los tiempos van a la consola).
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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

        W("# Campaña 11-200: informe de `level-generator --spec`");
        W();
        W(F($"Generado por `dotnet run --project tools/level-generator -c Release -- --spec tools/level-generator/specs/campaign_v1.json --levels {levelsArg} --out Assets/_Project/Data/Levels --report tools/out/campaign_v1_report.md`: no se edita a mano y no lleva tiempos. Generador {PackWriter.GeneratorVersion}, `pack_schema` {PackWriter.PackSchema}, modo de vertido `run` (D-005)."));
        W();
        W("Fuentes: D-009 (ciclo escalonado) y D-007 (debuts) con la propuesta `docs/research/cultura-ribera/propuesta-mecanicas-dificultad.md` "
          + "(c.1-c.4, d.1-d.4); GDD 03 §2, §4, §7-§13 y §17; 02 §8-§11; 08 §4.1. Todo se mide con `AF.Core` y `AF.Solver`: A* w = 1 a 300.000 "
          + "(par), bot ×40 con la semilla del nivel (`bot_fail` y etiqueta), `EarlyTraps` (trampas tempranas, Q16 B), ruta de referencia "
          + "para ramificación y trampas (`--parity`), `SafePath` en Aguas turbias y `LevelValidation` en cada tablero.");
        W();
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
          + "q = cuantil de D en el pool del rango y su banda; TT = trampas tempranas; alivio = tipos publicados en `relief` en orden (− = minus_color, + = plus_empty).");
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

            if (c.Murky)
            {
                notes.Add(F($"turbia: {c.Board.Hidden.Length} dosis ocultas en {c.Board.Hidden.Select(h => h[0]).Distinct().Count()} cuia(s), {c.MaxRealities} realidades"));
            }

            foreach (ReliefBoard r in l.ReliefRejected)
            {
                notes.Add($"alivio {r.Type} no publicado ({r.Note})");
            }

            string relief = l.Relief.Count == 0 ? "—" : string.Join(" ", l.Relief.Select(r => r.Type == "minus_color" ? F($"−{r.Removed}({r.Par}; {r.BotFail:0.00})") : F($"+({r.BotFail:0.00})")));
            int band = Pool.BandIndex(c.Quantile);
            W(F($"| {p.Level} | {p.Position} | {p.RoleName} | {p.Cell.Short} | {c.K} | {c.Empties} ({c.FreeEmpties}) | {c.Board.Caps.Length} | {c.Source} | {c.Par} | {c.Expanded} | {c.Bot:0.000} | {p.Window} | {c.MeasuredLabel} | {p.TargetLabel ?? "—"} | {c.D:0.00} | {c.Quantile:0.00} ({Pool.BandName(band)}) | {c.Branching:0.00} | {c.Traps:0.00} | {c.EarlyTraps} | {c.Complexity} | {relief} | {string.Join("; ", notes)} |"));
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
        return sb.ToString();
    }

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
