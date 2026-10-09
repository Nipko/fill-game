// Informe del modo --handmade (docs/dev/prototipo-v0.md b: por nivel, la solución óptima y el camino más corto a
// Aguas estancadas; las soluciones nunca entran en el pack, 08 §4.1), en el modo de vertido del pack (D-005: run por
// defecto; single, solo pruebas), con el par del otro modo como referencia. Sin fechas: dos ejecuciones dan los mismos bytes.
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Core.Rules;

namespace AmazonFill.Tools.LevelGenerator.Handmade;

internal static class HandmadeReport
{
    public static string Build(List<HandmadeLevel> levels, List<Check> packChecks, string sha, string note, bool allOk, PourMode mode)
    {
        string key = PourModes.ToKey(mode);
        string flag = mode == PourModes.Default ? "" : $" --pour-mode {key}";
        var sb = new StringBuilder();
        void W(string line = "") => sb.Append(line).Append('\n');

        int total = levels.Sum(l => l.Checks.Count) + packChecks.Count;
        int pass = levels.Sum(l => l.Checks.Count(c => c.Ok)) + packChecks.Count(c => c.Ok);
        W("# Tutorial 1-10: informe de `level-generator --handmade` (Tintalú v0)");
        W();
        W("Generado por `dotnet run --project tools/level-generator -- --handmade tools/level-generator/specs/tutorial_v0.json "
          + $"--out Assets/_Project/Data/Levels --report tools/out/tutorial_v0_report.md{flag}`: no se edita a mano. Todas las reglas "
          + "(vertido legal, cantidad, fútiles, sellado, victoria y Aguas estancadas) se evalúan con `AF.Core.PourRules` y el par "
          + "se contrasta con `AF.Solver.AStarSolver`, las mismas que ejecuta el cliente (04 §5.1; `.claude/rules/core.md`). "
          + "Fuentes: 02 §1 y §13, glosario §9 y §11, 03 §3-§5, 08 §4.1 y `docs/dev/prototipo-v0.md` (contrato b).");
        W();
        W("- Diseño a mano: `tools/level-generator/specs/tutorial_v0.json`.");
        W(mode == PourModes.Default
            ? $"- **Modo de vertido: `{key}`** (el modo por defecto, `PourModes.Default`; decisión del dueño D-005 del 2026-10-08, "
              + "que sustituye a D-001; `docs/dev/decisiones.md`): cada vertido mueve la racha visible que cabe, con vertido parcial "
              + "(02 R9). El par del pack, la solución óptima y el camino a Aguas estancadas son de este modo; el par en `single` "
              + "(una dosis por vertido, solo pruebas) sale como referencia. El pack **no declara el modo** (pregunta P7, reescrita en "
              + "`docs/dev/decisiones.md`; el subconjunto v0 de `af-level/1` no tiene ese campo) y asume `rules_pour_mode` = `\"run\"`."
            : $"- **Modo de vertido: `{key}`**, solo pruebas (el modo por defecto del cliente es `run`, D-005): este pack solo vale con "
              + $"`rules_pour_mode` = `\"{key}\"`, porque no declara el modo (pregunta P7) y su par es el de `{key}` (riesgo R-2). "
              + "El par en `run` sale como referencia y es el que se compara con los rangos [P] de 02 §13.");
        W($"- Pack v0: `Assets/_Project/Data/Levels/tutorial_v0.json` (subconjunto de `af-level/1` sin `pack_schema`), SHA-256 `{sha}`{note}.");
        W(string.Create(CultureInfo.InvariantCulture, $"- Resultado: **{pass}/{total} comprobaciones PASS**{(allOk ? "" : " — HAY FALLOS")}."));
        W("- Notación: cuias con índices 0-based en orden visual; dosis de abajo arriba con el índice de motor del glosario §9 "
          + "(8 Açaí, 4 Aguaje, 3 Taperebá, 1 Guanábana, 7 Bacaba); un vertido es `origen→destino ×dosis`"
          + (mode == PourMode.Single ? " (siempre ×1 en `single`)." : " (en `run`, ×dosis es la parte de la racha visible que cabe, 02 R9)."));
        W("- Bot casual ×40 (03 §6): no se calcula para el tutorial, porque el contrato b no lo exige y el pack v0 no lleva "
          + "`difficulty.bot_fail`. El bot ya está en `AF.Solver` (`CasualBot`, con el PRNG del núcleo de 09 R26) y lo usan "
          + "`--spec` y `tools/level-qa`.");
        W();
        W("## Resumen");
        W();
        W($"| Nivel | id | Cuias = colores + vacíos | Colores | Par `run` | Rango [P] de 02 §13 (`run`) | Par `single` (pruebas) | Estados `{key}` | Sin salida | Aguas estancadas | Enseñanza comprobada |");
        W("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (HandmadeLevel l in levels)
        {
            string colors = string.Join(", ", l.Spec.Palette.Select(id => HandmadeCanon.FindTint(id)!.NameEs));
            string stuck = l.StuckPath is null ? "—" : $"{l.StuckPath.Count} vertido(s)";
            string run = ParCell(l, PourMode.Run, mode);
            string single = ParCell(l, PourMode.Single, mode);
            W(string.Create(CultureInfo.InvariantCulture,
                $"| {l.Number} | `{HandmadeCanon.LevelId(l.Number)}` | {l.Board.Count} = {l.Colors.Length} + {l.Empties} | {colors} | {run} | {l.Row.ParMin}-{l.Row.ParMax} | {single} | {l.Graph?.StateCount ?? 0} | {l.Graph?.DeadStateCount ?? 0} | {stuck} | {l.Teaching} |"));
        }

        W();
        W($"En negrita, el par que lleva el pack (`{key}`). Estados = estados canónicos alcanzables desde el inicio en `{key}` (03 §3); "
          + "sin salida = de ellos, los que ya no pueden ganar; Aguas estancadas = vertidos del camino más corto a un estado sin "
          + $"vertidos útiles (02 R13), en `{key}`.");
        W();
        string[] outOfRange = levels.Where(l => l.ParIn(PourMode.Run) is not int p || p < l.Row.ParMin || p > l.Row.ParMax)
            .Select(l => string.Create(CultureInfo.InvariantCulture, $"{l.Number} ({HandmadeLevel.Fmt(l.ParIn(PourMode.Run))} frente a {l.Row.ParMin}-{l.Row.ParMax})"))
            .ToArray();
        string[] drops = levels.Zip(levels.Skip(1))
            .Where(p => p.First.ParIn(PourMode.Run) > p.Second.ParIn(PourMode.Run))
            .Select(p => string.Create(CultureInfo.InvariantCulture,
                $"{p.First.Number} ({HandmadeLevel.Fmt(p.First.ParIn(PourMode.Run))}) > {p.Second.Number} ({HandmadeLevel.Fmt(p.Second.ParIn(PourMode.Run))})"))
            .ToArray();
        W("Rangos y rampa: los rangos [P] de la columna \"Par\" de 02 §13 se fijaron con la racha (`run`), "
          + (mode == PourMode.Run ? "que es el modo del pack (D-005)" : "y no con el modo de este pack (`single`, solo pruebas)")
          + "; cada nivel comprueba su par en `run` contra su rango (ver Comprobaciones). "
          + $"Par en `run` fuera de su rango: {(outOfRange.Length == 0 ? "ninguno" : string.Join(", ", outOfRange))}. "
          + $"Rampa del par en `run` (informativa, no falla): {(drops.Length == 0 ? "no decreciente" : "decrece en " + string.Join(" y ", drops))}.");
        W();
        W("## Comprobaciones");
        W();
        W("| Nivel | Comprobación | Resultado | Detalle |");
        W("|---|---|---|---|");
        foreach (HandmadeLevel l in levels)
        {
            foreach (Check c in l.Checks)
            {
                W($"| {l.Number} | {c.Name} | {(c.Ok ? "PASS" : "**FAIL**")} | {c.Detail} |");
            }
        }

        foreach (Check c in packChecks)
        {
            W($"| pack | {c.Name} | {(c.Ok ? "PASS" : "**FAIL**")} | {c.Detail} |");
        }

        W();
        string[] below = levels.Where(l => l.Par is null || l.Par < 2 * l.Colors.Length)
            .Select(l => string.Create(CultureInfo.InvariantCulture, $"{l.Number} (par {HandmadeLevel.Fmt(l.Par)} < {2 * l.Colors.Length})"))
            .ToArray();
        string verdict = below.Length == 0 ? "lo cumplen todos los niveles"
            : below.Length == 1 ? $"no lo cumple el nivel {below[0]}"
            : $"no lo cumplen los niveles {string.Join(", ", below)}";
        W($"Informativo, no falla: 08 §4.1 exige `par ≥ 2·K` en `tools/level-qa`; en este tutorial (par en `{key}`) {verdict} "
          + "(pregunta 2 de `docs/dev/prototipo-v0.md`: ¿queda exento el tutorial?).");
        HandmadeLevel? level3 = levels.FirstOrDefault(l => l.Number == 3);
        if (level3?.Graph is not null)
        {
            (int? worst, int toDead) = level3.Graph.Detours();
            W();
            string worstText = worst is null ? "—" : worst.Value.ToString("+0;-0;0", CultureInfo.InvariantCulture);
            string deadText = toDead.ToString(CultureInfo.InvariantCulture);
            W($"Informativo, nivel 3 (en `{key}`): el mayor cambio de la distancia del solver con un vertido útil, en cualquier estado resoluble, es "
                + worstText + " (0 = el vertido no acerca y cuesta 1 de más); vertidos útiles hacia un estado sin salida: " + deadText + ". "
                + "Si no llega a +2, el disparador literal del tutorial de Marea de 02 §13 (\"distancia del solver +2\") no puede darse en el 3 "
                + "y queda su respaldo (inicio del nivel 5); la comprobación del nivel 3 usa la lectura débil: vertidos que no acercan.");
        }

        W();
        W("## Detalle por nivel");
        foreach (HandmadeLevel l in levels)
        {
            W();
            W($"### Nivel {l.Number} · `{HandmadeCanon.LevelId(l.Number)}` · par {HandmadeLevel.Fmt(l.Par)} (`{key}`; en `{PourModes.ToKey(l.ReferenceMode)}`: {HandmadeLevel.Fmt(l.ReferencePar)})");
            W();
            if (!string.IsNullOrEmpty(l.Spec.Note))
            {
                W(l.Spec.Note);
                W();
            }

            for (int i = 0; i < l.Doses.Length; i++)
            {
                string names = l.Doses[i].Length == 0 ? "vacía" : string.Join(", ", l.Doses[i].Select(c => HandmadeCanon.TintByEngine(c).NameEs));
                W(string.Create(CultureInfo.InvariantCulture, $"- cuia {i}: {names} `[{string.Join(", ", l.Doses[i])}]`"));
            }

            W();
            W(l.Solution is null
                ? "Solución óptima: no existe"
                : string.Create(CultureInfo.InvariantCulture, $"Solución óptima en `{key}` ({l.Solution.Count} vertidos): `{Moves(l.Solution)}`"));
            W();
            if (l.StuckPath is null)
            {
                W($"Camino más corto a Aguas estancadas en `{key}`: no existe (ningún estado alcanzable se queda sin vertidos útiles).");
            }
            else
            {
                W(string.Create(CultureInfo.InvariantCulture,
                    $"Camino más corto a Aguas estancadas en `{key}` ({l.StuckPath.Count} vertido(s)): `{Moves(l.StuckPath)}` → `{State(Final(l.Graph!, l.Board, l.StuckPath))}`"));
            }
        }

        return sb.ToString();
    }

    private static string Moves(List<Pour> moves) =>
        moves.Count == 0 ? "—" : string.Join(" · ", moves.Select(HandmadeLevel.Fmt));

    private static BoardState Final(StateGraph graph, BoardState start, List<Pour> moves)
    {
        BoardState s = start;
        foreach (Pour m in moves)
        {
            s = graph.Apply(s, m);
        }

        return s;
    }

    /// <summary>Par de <paramref name="l"/> en <paramref name="cell"/>; en negrita si es el del pack.</summary>
    private static string ParCell(HandmadeLevel l, PourMode cell, PourMode pack)
    {
        string value = HandmadeLevel.Fmt(l.ParIn(cell));
        return cell == pack ? $"**{value}**" : value;
    }

    private static string State(BoardState s)
    {
        var parts = new List<string>();
        for (int i = 0; i < s.Count; i++)
        {
            Cuia c = s[i];
            var doses = new List<string>();
            for (int k = 0; k < c.Count; k++)
            {
                doses.Add(c.ColorAt(k).ToString(CultureInfo.InvariantCulture));
            }

            parts.Add("[" + string.Join(",", doses) + "]");
        }

        return string.Join(" ", parts);
    }
}
