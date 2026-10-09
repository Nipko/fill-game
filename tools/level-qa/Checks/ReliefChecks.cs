// Alivio «El río bajó» (P-8): GDD 03 §13, §17 #8 y 08 §4.1 (criterio de alivio de tools/level-qa).
using System.Globalization;
using AF.Core.Board;
using AF.Solver.Search;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class ReliefChecks
{
    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-RELIEF-MIN", "relief con al menos una variante desde el nivel 31 (03 sec. 13; 08 sec. 4.1)"),
        ("QA-RELIEF-VALID", "cada variante es un tablero valido (BoardBuilder y LevelValidation)"),
        ("QA-RELIEF-MINUS", "minus_color resoluble (A* a 100.000) con el par entero publicado y sin el color removed (03 sec. 13)"),
        ("QA-RELIEF-PLUS", "plus_empty = el tablero base con exactamente un vacio mas (03 sec. 13)"),
        ("QA-RELIEF-BOT", "bot_fail de la variante recalculado; baja >= 0,20, o con base < 0,20 par menor (minus_color) o el vacio extra (plus_empty) (03 sec. 17 #8)"),
    ];

    public static IEnumerable<QaIssue> Run(LevelEval e)
    {
        QaLevel lv = e.Level;
        QaIssue Fail(string id, string msg) => new(id, lv.Pack, lv.Level, msg);
        if (lv.Level >= 31 && lv.Relief.Count == 0)
        {
            yield return Fail("QA-RELIEF-MIN", "nivel >= 31 sin variantes de alivio");
        }

        int baseEmpties = lv.Board.Cuias.Count(c => c.Length == 0);
        foreach (ReliefEval r in e.Relief)
        {
            string name = r.Relief.Type;
            if (r.State is null || r.Error is not null)
            {
                yield return Fail("QA-RELIEF-VALID", $"{name}: {r.Error ?? "tablero no valido"}");
                continue;
            }

            if (name == "minus_color")
            {
                ParResult? par = r.Par;
                if (par is null || par.Status != SolveStatus.Solved)
                {
                    yield return Fail("QA-RELIEF-MINUS", $"minus_color irresoluble ({par?.Status.ToString() ?? "sin resolver"})");
                }
                else if (r.Relief.Par != par.Par)
                {
                    yield return Fail("QA-RELIEF-MINUS", string.Create(CultureInfo.InvariantCulture, $"minus_color con par publicado {r.Relief.Par?.ToString(CultureInfo.InvariantCulture) ?? "null"} y recalculado {par.Par}"));
                }

                if (r.Relief.Removed is not int removed || r.Relief.Board.Cuias.Any(c => c.Contains(removed)) || !lv.Board.Cuias.Any(c => c.Contains(removed)))
                {
                    yield return Fail("QA-RELIEF-MINUS", "minus_color: removed no es un color del base que la variante quite entero");
                }
            }
            else
            {
                bool shape = r.Relief.Board.Caps.Length == lv.Board.Caps.Length + 1
                    && r.Relief.Board.Caps[^1] == Cuia.DefaultCapacity && r.Relief.Board.Cuias[^1].Length == 0
                    && r.Relief.Board.Cuias.Count(c => c.Length == 0) == baseEmpties + 1;
                if (!shape)
                {
                    yield return Fail("QA-RELIEF-PLUS", "plus_empty no anade exactamente un vacio de 4 al base");
                }
            }

            if (!Evaluator.Same3(r.Bot, r.Relief.BotFail))
            {
                yield return Fail("QA-RELIEF-BOT", string.Create(CultureInfo.InvariantCulture, $"{name}: bot_fail publicado {r.Relief.BotFail:0.000} y recalculado {r.Bot:0.000}"));
            }

            if (!Relief.MeetsCriterion(name, lv.BotFail, lv.Par, r.Relief.BotFail, r.Relief.Par))
            {
                yield return Fail("QA-RELIEF-BOT", string.Create(CultureInfo.InvariantCulture, $"{name}: no alivia (base {lv.BotFail:0.000}, variante {r.Relief.BotFail:0.000}, par {r.Relief.Par?.ToString(CultureInfo.InvariantCulture) ?? "null"} frente a {lv.Par})"));
            }
        }
    }
}
