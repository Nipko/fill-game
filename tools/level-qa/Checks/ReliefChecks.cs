// Alivio «El río bajó» (P-8): GDD 03 §13, §17 #8 y 08 §4.1 (criterio de alivio de tools/level-qa).
// Tinaja (M13; D-019; 03 §13; propuesta de la tinaja c.3 punto 4 y c.5; P3 de la tanda 4):
//   - minus_color nunca quita una fruta que esté en la tinaja (rompería T4 y la máscara): QA-RELIEF-MINUS;
//   - la variante de un nivel con tinaja la conserva con su máscara en hidden (todas sus capas menos la boca), y una variante
//     sin tinaja no publica hidden (decisión del dueño del 2026-10-14, opción a de la revisión de P2a; 08 §4.1): QA-RELIEF-MASK;
//   - la forma de la tinaja en la variante (una, su cuia contigua y en la misma fila, ≤ 13 cuias, T4 y combinaciones;
//     TinajaChecks.Structure) da QA-RELIEF-VALID. El tope del alivio es el del nivel (12, 02 §5) más la cuia vacía de
//     plus_empty: decisión del dueño del 2026-10-18 (D-027; Superposition.TinajaReliefMaxCuias), la misma que el generador;
//   - plus_empty con tinaja admite el vacío de más en otra ranura que la última, para dejar la pareja en la misma fila
//     (Relief.PlusEmptyWithTinaja): el resto del tablero, igual que el base;
//   - la variante repite la ruta segura y los paseos (TinajaWalks con ReliefOptions [P], pendiente de decisión; sin la
//     compuerta de coste), con la semilla del generador: QA-RELIEF-SAFE.
// Los vacíos se cuentan sin la tinaja ni su cuia (MechanicChecks.FreeEmpties).
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Solver.Search;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class ReliefChecks
{
    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-RELIEF-MIN", "relief con al menos una variante desde el nivel 31 (03 sec. 13; 08 sec. 4.1)"),
        ("QA-RELIEF-VALID", "cada variante es un tablero valido (BoardBuilder y LevelValidation; con tinaja, su forma: una, su cuia contigua y en la misma fila, <= 13 cuias (D-027), T4 y combinaciones)"),
        ("QA-RELIEF-MINUS", "minus_color resoluble (A* w = 1 a 2.000.000: la segunda pasada del generador, Q-N2 (a); la primera, a 100.000) con el par entero publicado y sin el color removed, que nunca es una fruta de la tinaja (03 sec. 13; propuesta de la tinaja c.3 punto 4)"),
        ("QA-RELIEF-PLUS", "plus_empty = el tablero base con exactamente un vacio de 4 mas (al final; con tinaja, en la ranura que deja la pareja en la misma fila) (03 sec. 13)"),
        ("QA-RELIEF-BOT", "bot_fail de la variante recalculado; baja >= 0,20, o con base < 0,20 par menor (minus_color) o el vacio extra (plus_empty) (03 sec. 17 #8)"),
        ("QA-RELIEF-MASK", "variante de un nivel con tinaja: la conserva y su hidden es la mascara (todas sus capas menos la boca); variante sin tinaja: sin hidden (08 sec. 4.1)"),
        ("QA-RELIEF-SAFE", "variante con tinaja: ruta segura y paseos seguros y casuales (03 sec. 13; propuesta de la tinaja c.3 punto 4; opciones de Morpho [P])"),
    ];

    public static IEnumerable<QaIssue> Run(LevelEval e)
    {
        QaLevel lv = e.Level;
        QaIssue Fail(string id, string msg) => new(id, lv.Pack, lv.Level, msg);
        if (lv.Level >= 31 && lv.Relief.Count == 0)
        {
            yield return Fail("QA-RELIEF-MIN", "nivel >= 31 sin variantes de alivio");
        }

        BoardData baseBoard = lv.Board;
        bool baseTinaja = baseBoard.HasTinaja;
        int[] tinajaFruits = Enumerable.Range(0, baseBoard.Attrs.Length)
            .Where(i => baseBoard.Attrs[i].Type == MechanicType.Tinaja).SelectMany(i => baseBoard.Cuias[i]).Distinct().ToArray();
        int baseEmpties = MechanicChecks.FreeEmpties(baseBoard);
        foreach (ReliefEval r in e.Relief)
        {
            string name = r.Relief.Type;
            BoardData board = r.Relief.Board;

            // Máscara de la tinaja en la variante (08 §4.1) y forma de la tinaja: se comprueban sobre el JSON, también si el
            // tablero no es válido.
            bool variantTinaja = board.HasTinaja;
            if (baseTinaja && !variantTinaja)
            {
                yield return Fail("QA-RELIEF-MASK", $"{name}: la variante pierde la tinaja (03 sec. 13: las variantes la conservan con su mascara)");
            }

            if (variantTinaja && !r.Relief.HiddenPresent)
            {
                yield return Fail("QA-RELIEF-MASK", $"{name}: variante con tinaja sin hidden (lleva su mascara)");
            }
            else if (!variantTinaja && r.Relief.HiddenPresent)
            {
                yield return Fail("QA-RELIEF-MASK", string.Create(CultureInfo.InvariantCulture,
                    $"{name}: variante sin tinaja con hidden ({board.Hidden.Length} posiciones): solo la lleva una variante con la tinaja"));
            }

            foreach ((string id, string msg) in TinajaChecks.Structure(board, Superposition.TinajaReliefMaxCuias))
            {
                yield return Fail(id == "QA-TINAJA-MASK" ? "QA-RELIEF-MASK" : "QA-RELIEF-VALID", $"{name}: {msg}");
            }

            // minus_color nunca quita una fruta de la tinaja (sobre el JSON: también si la variante no es válida, que es lo
            // que suele pasar al quitarla).
            if (name == "minus_color" && r.Relief.Removed is int gone && tinajaFruits.Contains(gone))
            {
                yield return Fail("QA-RELIEF-MINUS", string.Create(CultureInfo.InvariantCulture,
                    $"minus_color quita el color {gone}, que esta en la tinaja (rompe T4 y la mascara; propuesta de la tinaja c.3 punto 4)"));
            }

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

                if (r.Relief.Removed is not int removed || board.Cuias.Any(c => c.Contains(removed)) || !baseBoard.Cuias.Any(c => c.Contains(removed)))
                {
                    yield return Fail("QA-RELIEF-MINUS", "minus_color: removed no es un color del base que la variante quite entero");
                }
            }
            else if (!IsPlusEmptyOf(baseBoard, board, anySlot: baseTinaja) || MechanicChecks.FreeEmpties(board) != baseEmpties + 1)
            {
                yield return Fail("QA-RELIEF-PLUS", baseTinaja
                    ? "plus_empty no es el base con un vacio de 4 mas en una ranura"
                    : "plus_empty no anade exactamente un vacio de 4 al base");
            }

            if (!Evaluator.Same3(r.Bot, r.Relief.BotFail))
            {
                yield return Fail("QA-RELIEF-BOT", string.Create(CultureInfo.InvariantCulture, $"{name}: bot_fail publicado {r.Relief.BotFail:0.000} y recalculado {r.Bot:0.000}"));
            }

            if (!Relief.MeetsCriterion(name, lv.BotFail, lv.Par, r.Relief.BotFail, r.Relief.Par))
            {
                yield return Fail("QA-RELIEF-BOT", string.Create(CultureInfo.InvariantCulture, $"{name}: no alivia (base {lv.BotFail:0.000}, variante {r.Relief.BotFail:0.000}, par {r.Relief.Par?.ToString(CultureInfo.InvariantCulture) ?? "null"} frente a {lv.Par})"));
            }

            if (r.Tinaja is { Reject: string why } check)
            {
                yield return Fail("QA-RELIEF-SAFE", AsciiText.Of(string.Create(CultureInfo.InvariantCulture, $"{name}: {why} [{check.Kind}]")));
            }
        }
    }

    /// <summary>
    /// <paramref name="variant"/> es <paramref name="board"/> con una cuia vacía de 4 sin mecánica de más: al final o, con
    /// <paramref name="anySlot"/> (tinaja), en cualquier ranura; caps, cuias y attrs del resto, iguales. Las ocultas no se
    /// comparan aquí (QA-RELIEF-MASK).
    /// </summary>
    public static bool IsPlusEmptyOf(BoardData board, BoardData variant, bool anySlot)
    {
        int n = board.Caps.Length;
        if (variant.Caps.Length != n + 1 || variant.Cuias.Length != n + 1 || variant.Attrs.Length != n + 1
            || board.Cuias.Length != n || board.Attrs.Length != n)
        {
            return false;
        }

        for (int at = anySlot ? 0 : n; at <= n; at++)
        {
            if (variant.Caps[at] != Cuia.DefaultCapacity || variant.Cuias[at].Length != 0 || !variant.Attrs[at].IsNone)
            {
                continue;
            }

            bool same = true;
            for (int i = 0; i < n && same; i++)
            {
                int j = i < at ? i : i + 1;
                same = board.Caps[i] == variant.Caps[j] && board.Cuias[i].AsSpan().SequenceEqual(variant.Cuias[j]) && board.Attrs[i].Equals(variant.Attrs[j]);
            }

            if (same)
            {
                return true;
            }
        }

        return false;
    }
}
