// Informe del modo --handmade (docs/dev/prototipo-v0.md b: por nivel, la solución óptima y el camino más corto a
// Aguas estancadas; las soluciones nunca entran en el pack, 08 §4.1). Sin fechas: dos ejecuciones dan los mismos bytes.
using System.Globalization;
using System.Text;
using AF.Core.Board;
using AF.Core.Rules;

namespace AmazonFill.Tools.LevelGenerator.Handmade;

internal static class HandmadeReport
{
    public static string Build(List<HandmadeLevel> levels, List<Check> packChecks, string sha, string note, bool allOk)
    {
        var sb = new StringBuilder();
        void W(string line = "") => sb.Append(line).Append('\n');

        int total = levels.Sum(l => l.Checks.Count) + packChecks.Count;
        int pass = levels.Sum(l => l.Checks.Count(c => c.Ok)) + packChecks.Count(c => c.Ok);
        W("# Tutorial 1-10: informe de `level-generator --handmade` (Tintalú v0)");
        W();
        W("Generado por `dotnet run --project tools/level-generator -- --handmade tools/level-generator/specs/tutorial_v0.json "
          + "--out Assets/_Project/Data/Levels --report tools/out/tutorial_v0_report.md`: no se edita a mano. Todas las reglas "
          + "(vertido legal, cantidad, fútiles, sellado, victoria y Aguas estancadas) se evalúan con `AF.Core.PourRules` y el par "
          + "se contrasta con `AF.Solver.AStarSolver`, las mismas que ejecuta el cliente (04 §5.1; `.claude/rules/core.md`). "
          + "Fuentes: 02 §1 y §13, glosario §9 y §11, 03 §3-§5, 08 §4.1 y `docs/dev/prototipo-v0.md` (contrato b).");
        W();
        W("- Diseño a mano: `tools/level-generator/specs/tutorial_v0.json`.");
        W($"- Pack v0: `Assets/_Project/Data/Levels/tutorial_v0.json` (subconjunto de `af-level/1` sin `pack_schema`), SHA-256 `{sha}`{note}.");
        W(string.Create(CultureInfo.InvariantCulture, $"- Resultado: **{pass}/{total} comprobaciones PASS**{(allOk ? "" : " — HAY FALLOS")}."));
        W("- Notación: cuias con índices 0-based en orden visual; dosis de abajo arriba con el índice de motor del glosario §9 "
          + "(8 Açaí, 4 Aguaje, 3 Taperebá, 1 Guanábana, 7 Bacaba); un vertido es `origen→destino ×dosis`.");
        W("- Bot casual ×40 (03 §6): no se calcula. El bot aún no existe en `AF.Solver` (su PRNG espera la decisión del dueño "
          + "sobre la paridad con MT19937, riesgo R11 del plan) y el contrato b no lo exige para el tutorial.");
        W();
        W("## Resumen");
        W();
        W("| Nivel | id | Cuias = colores + vacíos | Colores | Par (rango [P]) | Estados | Sin salida | Aguas estancadas | Enseñanza comprobada |");
        W("|---|---|---|---|---|---|---|---|---|");
        foreach (HandmadeLevel l in levels)
        {
            string colors = string.Join(", ", l.Spec.Palette.Select(id => HandmadeCanon.FindTint(id)!.NameEs));
            string stuck = l.StuckPath is null ? "—" : $"{l.StuckPath.Count} vertido(s)";
            W(string.Create(CultureInfo.InvariantCulture,
                $"| {l.Number} | `{HandmadeCanon.LevelId(l.Number)}` | {l.Board.Count} = {l.Colors.Length} + {l.Empties} | {colors} | {HandmadeLevel.Fmt(l.Par)} ({l.Row.ParMin}-{l.Row.ParMax}) | {l.Graph?.StateCount ?? 0} | {l.Graph?.DeadStateCount ?? 0} | {stuck} | {l.Teaching} |"));
        }

        W();
        W("Estados = estados canónicos alcanzables desde el inicio (03 §3); sin salida = de ellos, los que ya no pueden ganar; "
          + "Aguas estancadas = vertidos del camino más corto a un estado sin vertidos útiles (02 R13).");
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
        W($"Informativo, no falla: 08 §4.1 exige `par ≥ 2·K` en `tools/level-qa`; en este tutorial {verdict} "
          + "(pregunta 2 de `docs/dev/prototipo-v0.md`: ¿queda exento el tutorial?).");
        HandmadeLevel? level3 = levels.FirstOrDefault(l => l.Number == 3);
        if (level3?.Graph is not null)
        {
            (int? worst, int toDead) = level3.Graph.Detours();
            W();
            string worstText = worst is null ? "—" : worst.Value.ToString("+0;-0;0", CultureInfo.InvariantCulture);
            string deadText = toDead.ToString(CultureInfo.InvariantCulture);
            W("Informativo, nivel 3: el mayor cambio de la distancia del solver con un vertido útil, en cualquier estado resoluble, es "
                + worstText + " (0 = el vertido no acerca y cuesta 1 de más); vertidos útiles hacia un estado sin salida: " + deadText + ". "
                + "Si no llega a +2, el disparador literal del tutorial de Marea de 02 §13 (\"distancia del solver +2\") no puede darse en el 3 "
                + "y queda su respaldo (inicio del nivel 5); la comprobación del nivel 3 usa la lectura débil: vertidos que no acercan.");
        }

        W();
        W("## Detalle por nivel");
        foreach (HandmadeLevel l in levels)
        {
            W();
            W($"### Nivel {l.Number} · `{HandmadeCanon.LevelId(l.Number)}` · par {HandmadeLevel.Fmt(l.Par)}");
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
                : string.Create(CultureInfo.InvariantCulture, $"Solución óptima ({l.Solution.Count} vertidos): `{Moves(l.Solution)}`"));
            W();
            if (l.StuckPath is null)
            {
                W("Camino más corto a Aguas estancadas: no existe (ningún estado alcanzable se queda sin vertidos útiles).");
            }
            else
            {
                W(string.Create(CultureInfo.InvariantCulture,
                    $"Camino más corto a Aguas estancadas ({l.StuckPath.Count} vertido(s)): `{Moves(l.StuckPath)}` → `{State(Final(l.Board, l.StuckPath))}`"));
            }
        }

        return sb.ToString();
    }

    private static string Moves(List<Pour> moves) =>
        moves.Count == 0 ? "—" : string.Join(" · ", moves.Select(HandmadeLevel.Fmt));

    private static BoardState Final(BoardState start, List<Pour> moves)
    {
        BoardState s = start;
        foreach (Pour m in moves)
        {
            s = PourRules.Apply(s, m.Source, m.Target);
        }

        return s;
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
