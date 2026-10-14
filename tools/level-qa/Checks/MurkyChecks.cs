// Aguas turbias (P-10; GDD 02 §11 T1-T6, 03 §8 RUTA_SEGURA y §17 #7; D-007 Q9 y Q14 A): como mucho 3 cuias y 4 dosis
// ocultas, T4, ruta segura en cada estado (AF.Solver.Murky.SafePath), ni Lluvia, ni Piraña, ni Liana, ni Remolino en el
// nivel, solo desde el 100 en niveles Difícil o Muy difícil, y el estreno del 100 con una cuia y 2 dosis ocultas, sin otra
// mecánica (Q9).
// Tinaja (M13; D-019, Q-Ti1 A; 02 §9.1 M13 y §11 T1; propuesta de la tinaja c.5): sus ocultas no son Aguas turbias. murky
// sigue valiendo true con ellas (08 §4.1: «dosis ocultas, Aguas turbias o tinaja»), pero T1 (QA-MURKY-T1), los topes de
// Aguas turbias, su ruta segura y sus combinaciones solo miran las ocultas fuera de la tinaja (MechanicChecks.MurkyHidden):
// la excepción de D-019 deja la tinaja en niveles Normales desde su estreno. Su tope propio por tipo de nivel es
// QA-TINAJA-TIER, y el resto de sus controles (máscara, T4, combinaciones, ruta segura, paseos y Morpho) está en TinajaChecks.
using System.Globalization;
using AF.Core.Mechanics;
using AF.Solver.Murky;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class MurkyChecks
{
    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-MURKY-FLAG", "murky = true si y solo si hay posiciones en hidden (08 sec. 4.1)"),
        ("QA-MURKY-T1", "Aguas turbias solo desde el 100 y en niveles Dificil o Muy dificil (02 T1); la tinaja queda fuera (excepcion de D-019, Q-Ti1 A)"),
        ("QA-MURKY-LIMITS", "como mucho 3 cuias turbias y 4 dosis ocultas; T4 (02 T2-T4)"),
        ("QA-MURKY-SAFE", "ruta segura en cada estado de la ruta (03 sec. 8; 02 T5)"),
        ("QA-MURKY-COMBO", "sin Lluvia, Pirana, Liana ni Remolino en el nivel (Q14 A; [P] Remolino)"),
        ("QA-MURKY-Q9", "estreno del 100: una cuia, 2 dosis ocultas y ninguna otra mecanica (Q9)"),
        ("QA-TINAJA-TIER", "dosis de la tinaja por tipo de nivel: estreno, comodidad y practica 3 (2 ocultas); retos 4-5 (3-4); hito 5 (4) (D-019, Q-Ti1 A; 02 sec. 9.1 M13)"),
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

        // Tope de la tinaja por tipo de nivel (D-019, Q-Ti1 A; la tabla de Superposition.TinajaDoses que usa el generador).
        // Sin tinaja en el slot (p1, p2, p19 y la rampa) lo dice QA-TINAJA-SLOT.
        int[] tinajas = Enumerable.Range(0, lv.Board.Attrs.Length).Where(i => lv.Board.Attrs[i].Type == MechanicType.Tinaja).ToArray();
        if (tinajas.Length > 0 && e.Plan is LevelPlan plan && Superposition.TinajaDoses(plan) is (int min, int max))
        {
            foreach (int t in tinajas)
            {
                int doses = lv.Board.Cuias[t].Length;
                if (doses < min || doses > max)
                {
                    string range = min == max
                        ? string.Create(CultureInfo.InvariantCulture, $"{min} ({min - 1} ocultas)")
                        : string.Create(CultureInfo.InvariantCulture, $"{min}-{max} ({min - 1}-{max - 1} ocultas)");
                    yield return Fail("QA-TINAJA-TIER", string.Create(CultureInfo.InvariantCulture,
                        $"tinaja (cuia {t}) con {doses} dosis en p{plan.Position} ({plan.RoleName}); lleva {range}"));
                }
            }
        }

        // Aguas turbias: las ocultas fuera de la tinaja.
        int[][] murkyHidden = MechanicChecks.MurkyHidden(lv.Board);
        int murky = murkyHidden.Length;
        if (murky == 0)
        {
            yield break;
        }

        if (lv.Level < 100 || lv.Label == "normal")
        {
            yield return Fail("QA-MURKY-T1", $"Aguas turbias en el nivel {lv.Level} con etiqueta {lv.Label}");
        }

        int cuias = murkyHidden.Select(h => h.Length > 0 ? h[0] : -1).Distinct().Count();
        if (cuias > 3 || murky > 4)
        {
            yield return Fail("QA-MURKY-LIMITS", string.Create(CultureInfo.InvariantCulture, $"{murky} dosis ocultas en {cuias} cuias (como mucho 4 en 3)"));
        }

        foreach (int[] h in murkyHidden.Where(h => h.Length == 2 && h[0] >= 0 && h[0] < lv.Board.Cuias.Length))
        {
            int[] c = lv.Board.Cuias[h[0]];
            if (h[1] >= 0 && h[1] + 1 < c.Length && c[h[1]] == c[h[1] + 1])
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

        if (lv.Intro == "murky" && (cuias != 1 || murky != 2 || MechanicChecks.Codes(lv.Board).Count != 1))
        {
            yield return Fail("QA-MURKY-Q9", string.Create(CultureInfo.InvariantCulture, $"estreno con {murky} dosis ocultas en {cuias} cuia(s) y mecanicas {string.Join("+", MechanicChecks.Codes(lv.Board))}"));
        }
    }
}
