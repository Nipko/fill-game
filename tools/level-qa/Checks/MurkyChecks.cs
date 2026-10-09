// Aguas turbias (P-10; GDD 02 §11 T1-T6, 03 §8 RUTA_SEGURA y §17 #7; D-007 Q9 y Q14 A): como mucho 3 cuias y 4 dosis
// ocultas, T4, ruta segura en cada estado (AF.Solver.Murky.SafePath), ni Lluvia, ni Piraña, ni Liana, ni Remolino en el
// nivel, solo desde el 100 en niveles Difícil o Muy difícil, y el estreno del 100 con una cuia y 2 dosis ocultas, sin otra
// mecánica (Q9).
using System.Globalization;
using AF.Core.Mechanics;
using AF.Solver.Murky;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class MurkyChecks
{
    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-MURKY-FLAG", "murky = true si y solo si hay posiciones en hidden (08 sec. 4.1)"),
        ("QA-MURKY-T1", "solo desde el 100 y en niveles Dificil o Muy dificil (02 T1)"),
        ("QA-MURKY-LIMITS", "como mucho 3 cuias turbias y 4 dosis ocultas; T4 (02 T2-T4)"),
        ("QA-MURKY-SAFE", "ruta segura en cada estado de la ruta (03 sec. 8; 02 T5)"),
        ("QA-MURKY-COMBO", "sin Lluvia, Pirana, Liana ni Remolino en el nivel (Q14 A; [P] Remolino)"),
        ("QA-MURKY-Q9", "estreno del 100: una cuia, 2 dosis ocultas y ninguna otra mecanica (Q9)"),
    ];

    public static IEnumerable<QaIssue> Run(LevelEval e)
    {
        QaLevel lv = e.Level;
        QaIssue Fail(string id, string msg) => new(id, lv.Pack, lv.Level, msg);
        int hidden = lv.Board.Hidden.Length;
        if (lv.Murky != (hidden > 0))
        {
            yield return Fail("QA-MURKY-FLAG", string.Create(CultureInfo.InvariantCulture, $"murky = {(lv.Murky ? "true" : "false")} con {hidden} posiciones ocultas"));
        }

        if (hidden == 0)
        {
            yield break;
        }

        if (lv.Level < 100 || lv.Label == "normal")
        {
            yield return Fail("QA-MURKY-T1", $"Aguas turbias en el nivel {lv.Level} con etiqueta {lv.Label}");
        }

        int cuias = lv.Board.Hidden.Select(h => h[0]).Distinct().Count();
        if (cuias > 3 || hidden > 4)
        {
            yield return Fail("QA-MURKY-LIMITS", string.Create(CultureInfo.InvariantCulture, $"{hidden} dosis ocultas en {cuias} cuias (como mucho 4 en 3)"));
        }

        foreach (int[] h in lv.Board.Hidden)
        {
            int[] c = lv.Board.Cuias[h[0]];
            if (h[1] + 1 < c.Length && c[h[1]] == c[h[1] + 1])
            {
                yield return Fail("QA-MURKY-LIMITS", string.Create(CultureInfo.InvariantCulture, $"T4: la dosis oculta [{h[0]}, {h[1]}] repite el color de la de encima"));
            }
        }

        if (e.Safe is SafePathStatus s && s != SafePathStatus.Ok)
        {
            yield return Fail("QA-MURKY-SAFE", $"sin ruta segura: {s}");
        }

        foreach (MechanicSpec a in lv.Board.Attrs.Where(a => a.Type is MechanicType.Lluvia or MechanicType.Pirana or MechanicType.Liana or MechanicType.Remolino))
        {
            yield return Fail("QA-MURKY-COMBO", $"Aguas turbias con {MechanicTypes.ToKey(a.Type)}");
        }

        if (lv.Intro == "murky" && (cuias != 1 || hidden != 2 || MechanicChecks.Codes(lv.Board).Count != 1))
        {
            yield return Fail("QA-MURKY-Q9", string.Create(CultureInfo.InvariantCulture, $"estreno con {hidden} dosis ocultas en {cuias} cuia(s) y mecanicas {string.Join("+", MechanicChecks.Codes(lv.Board))}"));
        }
    }
}
