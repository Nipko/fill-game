// Anti-repetición (P-8; GDD 03 §17 #9): forma distinta de la de los 200 niveles anteriores y distancia de Hamming ≥ 25 %
// con los 20 anteriores, con las mismas definiciones que la colocación del generador (Campaign/BoardShape.cs).
using System.Globalization;
using AmazonFill.Tools.LevelGenerator.Campaign;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class RepetitionChecks
{
    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-REPEAT-SHAPE", "forma distinta de la de los 200 niveles anteriores (03 sec. 17 #9)"),
        ("QA-REPEAT-HAMMING", "Hamming >= 25 % con los 20 niveles anteriores (03 sec. 17 #9)"),
    ];

    public static IEnumerable<QaIssue> Run(QaContext ctx)
    {
        var seq = ctx.Levels.Select(e => e.Level).OrderBy(l => l.Level).ToList();
        var shapes = seq.Select(l => BoardShape.Shape(l.Board.Caps, l.Board.Cuias, l.Board.Attrs.Select(a => (int)a.Type).ToArray())).ToList();
        for (int i = 0; i < seq.Count; i++)
        {
            QaLevel cur = seq[i];
            for (int j = Math.Max(0, i - BoardShape.ShapeWindow); j < i; j++)
            {
                if (shapes[j] == shapes[i] && cur.Level - seq[j].Level <= BoardShape.ShapeWindow)
                {
                    yield return new QaIssue("QA-REPEAT-SHAPE", cur.Pack, cur.Level, string.Create(CultureInfo.InvariantCulture, $"misma forma que el nivel {seq[j].Level}"));
                    break;
                }
            }

            for (int j = Math.Max(0, i - BoardShape.HammingWindow); j < i; j++)
            {
                if (cur.Level - seq[j].Level > BoardShape.HammingWindow)
                {
                    continue;
                }

                double d = BoardShape.Hamming(seq[j].Board.Cuias, cur.Board.Cuias);
                if (d < BoardShape.MinHamming)
                {
                    yield return new QaIssue("QA-REPEAT-HAMMING", cur.Pack, cur.Level, string.Create(CultureInfo.InvariantCulture, $"Hamming {d:0.00} con el nivel {seq[j].Level} (< 0,25)"));
                    break;
                }
            }
        }
    }
}
