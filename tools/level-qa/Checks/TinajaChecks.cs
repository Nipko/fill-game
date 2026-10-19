// Tinaja (M13; D-019, Q-Ti1 A; 02 §9.1 M13; propuesta de la tinaja b.2, c.3 y c.5; P3 de la tanda 4): lo que el esquema
// no puede expresar (c.4) y los filtros del generador, repetidos con el mismo código (TinajaWalks) y las mismas semillas.
//   QA-TINAJA-ONE    una sola tinaja y una sola cuia suya (b.2 regla 1; «dos tinajas» es una variante posterior, d.4);
//   QA-TINAJA-PAIR   su cuia en la ranura contigua y en la misma fila con cualquiera de las filas del cliente (b.2 regla 1;
//                    Superposition.SameRow, la misma regla que el orden visual del generador);
//   QA-TINAJA-CUIAS  como mucho 12 cuias en un nivel con tinaja, contando la pareja (02 §5, fila «Tinaja y su cuia» [P];
//                    Superposition.TinajaMaxCuias); en sus variantes de alivio, 13 (la cuia vacía de plus_empty; decisión del
//                    dueño del 2026-10-18, D-027; Superposition.TinajaReliefMaxCuias), que ReliefChecks da como QA-RELIEF-VALID;
//   QA-TINAJA-MASK   hidden = exactamente todas las capas de la tinaja menos la boca, y nada oculto fuera de ella (b.2
//                    regla 1: es la única fuente de ocultas; c.4);
//   QA-TINAJA-T4     T4 en toda la columna: dos dosis vecinas nunca son de la misma fruta (b.2 regla 1);
//   QA-TINAJA-COMBO  sin Liana, Remolino, Lluvia ni Piraña en el nivel, y sin Barro dentro de la tinaja o de su cuia (02 §9.1
//                    M13, «Combinaciones»: el Barro fuera de la pareja está permitido; las de la 1.1 no existen en AF.Core;
//                    las otras cuias turbias las detiene QA-TINAJA-MASK);
//   QA-TINAJA-SLOT   ninguna tinaja en recuperación (p1, p2), remanso (p19) ni en la rampa (D-019, Q-Ti1 A;
//                    Superposition.TinajaDoses);
//   QA-TINAJA-DEBUT  tablero de estreno con tinaja: sola (ninguna otra mecánica ni capacidad, un ejemplar), 2 vacíos sin contar
//                    la pareja, bot_fail < 0,20 y S0 sin trampas (todo vertido útil de S0 es seguro; c.3 punto 3; d.3): el
//                    motivo separa los demostrados inseguros (trampa en S0) de los sin seguridad demostrada (presupuesto
//                    agotado; FIXG), y QA-TINAJA-SAFE no repite ese rechazo;
//   QA-TINAJA-SAFE   ruta segura (03 §8), 20 paseos seguros y 30 casuales [P] (TinajaWalks.Check): el motivo separa «sin
//                    vertido seguro» de «presupuesto agotado» también en los paseos casuales (FIXG: un SafeMoves sin
//                    presupuesto es «presupuesto agotado», sin consultar la realidad verdadera, nunca «adivinar»);
//   QA-TINAJA-MORPHO compuerta de coste de Morpho (w = 2, 40.000 nodos por realidad, tope de nodos con el factor
//                    MorphoDeviceFactor, 8× [P] hasta PF1) en S0, en la ruta y en los paseos: el mismo tope y factor que P1.
// El tope de dosis por tipo de nivel es QA-TINAJA-TIER (MurkyChecks); las variantes de alivio, ReliefChecks (QA-RELIEF-MASK y
// QA-RELIEF-SAFE). Las piezas de la tinaja que ya rechaza AF.Core (capacidades 5 y 2, 3-5 dosis, su cuia vacía) dan
// QA-VALID; aquí se repiten las que tienen control propio en c.5, para que un fallo diga qué regla de la tinaja rompe.
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class TinajaChecks
{
    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-TINAJA-ONE", "una sola tinaja y una sola cuia suya por nivel (02 sec. 9.1 M13 regla 1)"),
        ("QA-TINAJA-PAIR", "su cuia en la ranura contigua y en la misma fila con cualquiera de las filas del cliente (02 sec. 9.1 M13 regla 1; D-006)"),
        ("QA-TINAJA-CUIAS", "como mucho 12 cuias en un nivel con tinaja, contando la pareja (02 sec. 5 [P])"),
        ("QA-TINAJA-MASK", "hidden = todas las capas de la tinaja menos la boca, y nada oculto fuera de ella (02 sec. 9.1 M13 regla 1; propuesta de la tinaja c.4)"),
        ("QA-TINAJA-T4", "T4 en toda la columna de la tinaja: dos dosis vecinas nunca son de la misma fruta (02 sec. 9.1 M13 regla 1)"),
        ("QA-TINAJA-COMBO", "sin Liana, Remolino, Lluvia ni Pirana en el nivel y sin Barro en la tinaja o su cuia (02 sec. 9.1 M13, combinaciones)"),
        ("QA-TINAJA-SLOT", "ninguna tinaja en p1, p2, p19 ni en la rampa (D-019, Q-Ti1 A)"),
        ("QA-TINAJA-DEBUT", "estreno con tinaja: sola y un ejemplar, 2 vacios sin contar la pareja, bot_fail < 0,20 y S0 sin trampas (02 sec. 9.2 regla 2; propuesta de la tinaja c.3 y d.3)"),
        ("QA-TINAJA-SAFE", "ruta segura, 20 paseos seguros y 30 casuales [P] (03 sec. 8; D-019, Q-Ti1 A; propuesta de la tinaja c.3 punto 3)"),
        ("QA-TINAJA-MORPHO", "coste de Morpho (w = 2, 40.000 nodos por realidad) en S0, la ruta y los paseos dentro de hint_timeout_ms con el factor del telefono [P] (propuesta de la tinaja c.3 punto 3; 08 sec. 9)"),
    ];

    /// <summary>Bot del estreno: por debajo de 0,20 (02 §9.2 regla 2).</summary>
    public const double DebutBotMax = 0.20;

    public static IEnumerable<QaIssue> Run(LevelEval e)
    {
        QaLevel lv = e.Level;
        BoardData b = lv.Board;
        if (!b.Attrs.Any(a => MechanicChecks.IsTinajaPair(a.Type)))
        {
            yield break;
        }

        QaIssue Fail(string id, string msg) => new(id, lv.Pack, lv.Level, msg);
        foreach ((string id, string msg) in Structure(b))
        {
            yield return Fail(id, msg);
        }

        LevelPlan? plan = e.Plan;
        if (plan is not null && Superposition.TinajaDoses(plan) is null)
        {
            string where = plan.Ramp ? "la rampa" : string.Create(CultureInfo.InvariantCulture, $"p{plan.Position} ({plan.RoleName})");
            yield return Fail("QA-TINAJA-SLOT", $"tinaja en {where}: la recuperacion (p1, p2), el remanso (p19) y la rampa son niveles puros");
        }

        if (plan is { DebutBoard: true })
        {
            SortedSet<string> codes = MechanicChecks.Codes(b);
            int[] caps = MechanicChecks.CapacityCaps(b).ToArray();
            if (codes.Count != 1 || !codes.Contains("Ti") || caps.Length > 0)
            {
                string others = string.Join("+", codes.Where(c => c != "Ti").Concat(caps.Select(c => string.Create(CultureInfo.InvariantCulture, $"cuia de {c}"))));
                yield return Fail("QA-TINAJA-DEBUT", $"el tablero de estreno con tinaja va solo; lleva ademas {others}");
            }

            int instances = MechanicChecks.Instances(b);
            if (instances != 1)
            {
                yield return Fail("QA-TINAJA-DEBUT", string.Create(CultureInfo.InvariantCulture, $"estreno con {instances} ejemplares de mecanica; la tinaja con su cuia es uno"));
            }

            int empties = MechanicChecks.FreeEmpties(b);
            if (empties < 2)
            {
                yield return Fail("QA-TINAJA-DEBUT", string.Create(CultureInfo.InvariantCulture, $"estreno con {empties} vacio(s) sin contar la pareja; lleva 2"));
            }

            if (lv.BotFail >= DebutBotMax - 1e-9)
            {
                yield return Fail("QA-TINAJA-DEBUT", string.Create(CultureInfo.InvariantCulture, $"estreno con bot_fail {lv.BotFail:0.000}; va por debajo de 0,20"));
            }

            if (e.TinajaS0Traps > 0)
            {
                // Separa la trampa demostrada del vertido que solo no se pudo demostrar seguro (03 §8: «El informe separa
                // «sin vertido seguro» de «presupuesto agotado»»; TinajaWalks.S0Reject; FIXG).
                int unproven = Math.Max(0, e.TinajaS0Unproven);
                yield return Fail("QA-TINAJA-DEBUT", e.TinajaS0Traps == int.MaxValue
                    ? "S0 sin vertido seguro: no se puede comprobar que S0 no tenga trampas"
                    : string.Create(CultureInfo.InvariantCulture,
                        $"{e.TinajaS0Traps} vertido(s) util(es) de S0 no son seguros (estreno sin trampas): {e.TinajaS0Traps - unproven} demostrado(s) inseguro(s) (trampa en S0); {unproven} sin seguridad demostrada (presupuesto agotado)"));
            }
        }

        // Filtros del generador (los mismos que SlotGenerator.Measure). Un rechazo en «S0 sin trampas» (S0Traps > 0: trampa en
        // S0 o presupuesto agotado en S0) ya lo dice QA-TINAJA-DEBUT, con los dos recuentos; aquí no se repite.
        if (e.Tinaja is { Reject: string why } check && check.S0Traps <= 0)
        {
            yield return Fail(check.Kind == TinajaWalks.KindMorpho ? "QA-TINAJA-MORPHO" : "QA-TINAJA-SAFE",
                AsciiText.Of(string.Create(CultureInfo.InvariantCulture, $"{why} [{check.Kind}]")));
        }
    }

    /// <summary>
    /// Controles de forma de la tinaja sobre un tablero (el del nivel o el de una variante de alivio): ONE, PAIR, CUIAS, MASK,
    /// T4 y COMBO, como pares (id, motivo). Vacío si el tablero no lleva la tinaja ni su cuia. <paramref name="maxCuias"/>: el
    /// tope de cuias con la pareja, el del nivel (<see cref="Superposition.TinajaMaxCuias"/>, 12) o, en una variante de alivio,
    /// el suyo (<see cref="Superposition.TinajaReliefMaxCuias"/>, 13; D-027).
    /// </summary>
    public static IEnumerable<(string Id, string Message)> Structure(BoardData b, int maxCuias = Superposition.TinajaMaxCuias)
    {
        int n = b.Cuias.Length;
        int[] tinajas = Enumerable.Range(0, b.Attrs.Length).Where(i => b.Attrs[i].Type == MechanicType.Tinaja).ToArray();
        int[] cuias = Enumerable.Range(0, b.Attrs.Length).Where(i => b.Attrs[i].Type == MechanicType.Cucharon).ToArray();
        if (tinajas.Length == 0 && cuias.Length == 0)
        {
            yield break;
        }

        if (tinajas.Length != 1 || cuias.Length != 1)
        {
            yield return ("QA-TINAJA-ONE", string.Create(CultureInfo.InvariantCulture,
                $"{tinajas.Length} tinaja(s) (cuias {List(tinajas)}) y {cuias.Length} cuia(s) suya(s) (cuias {List(cuias)}); va una de cada"));
        }

        if (n > maxCuias)
        {
            yield return ("QA-TINAJA-CUIAS", string.Create(CultureInfo.InvariantCulture,
                $"{n} cuias con la tinaja; como mucho {maxCuias}, contando la pareja"));
        }

        if (tinajas.Length == 1 && cuias.Length == 1)
        {
            int t = tinajas[0];
            int c = cuias[0];
            int left = Math.Min(t, c);
            if (Math.Abs(t - c) != 1)
            {
                yield return ("QA-TINAJA-PAIR", string.Create(CultureInfo.InvariantCulture, $"la tinaja (cuia {t}) y su cuia (cuia {c}) no son contiguas"));
            }
            else if (!Superposition.SameRow(n, left))
            {
                string rows = string.Join(" o ", Superposition.RowOptions(n).Select(r => r.ToString(CultureInfo.InvariantCulture)));
                yield return ("QA-TINAJA-PAIR", string.Create(CultureInfo.InvariantCulture,
                    $"la tinaja (cuia {t}) y su cuia (cuia {c}) no quedan en la misma fila con {n} cuias en {rows} filas"));
            }
        }

        // Máscara (con una sola tinaja): todas sus capas menos la boca, y ninguna oculta fuera de ella.
        if (tinajas.Length == 1)
        {
            int t = tinajas[0];
            int doses = b.Cuias[t].Length;
            var expected = Enumerable.Range(0, Math.Max(0, doses - 1)).Select(k => (t, k)).ToList();
            var actual = b.Hidden.Select(h => h.Length == 2 ? (h[0], h[1]) : (-1, -1)).ToList();
            bool same = actual.Count == expected.Count && actual.Distinct().Count() == actual.Count && expected.All(actual.Contains);
            if (!same)
            {
                yield return ("QA-TINAJA-MASK", string.Create(CultureInfo.InvariantCulture,
                    $"hidden {Pairs(actual)}; la mascara de la tinaja (cuia {t}, {doses} dosis) es {Pairs(expected)}: todas sus capas menos la boca"));
            }

            int[] outside = actual.Where(p => p.Item1 != t).Select(p => p.Item1).Distinct().Order().ToArray();
            if (outside.Length > 0)
            {
                yield return ("QA-TINAJA-MASK", $"dosis ocultas fuera de la tinaja (cuias {List(outside)}): la tinaja es la unica fuente de ocultas (sin Aguas turbias)");
            }
        }

        foreach (int t in tinajas)
        {
            int[] column = b.Cuias[t];
            for (int k = 0; k + 1 < column.Length; k++)
            {
                if (column[k] == column[k + 1])
                {
                    yield return ("QA-TINAJA-T4", string.Create(CultureInfo.InvariantCulture,
                        $"T4: las dosis {k} y {k + 1} de la tinaja (cuia {t}) son de la misma fruta ({column[k]})"));
                }
            }
        }

        for (int i = 0; i < b.Attrs.Length; i++)
        {
            MechanicType type = b.Attrs[i].Type;
            if (type is MechanicType.Liana or MechanicType.Remolino or MechanicType.Lluvia or MechanicType.Pirana)
            {
                yield return ("QA-TINAJA-COMBO", string.Create(CultureInfo.InvariantCulture, $"la tinaja no se combina con {MechanicTypes.ToKey(type)} (cuia {i})"));
            }

            if (MechanicChecks.IsTinajaPair(type) && b.Cuias[i].Contains(Cuia.MudColor))
            {
                string piece = type == MechanicType.Tinaja ? "la tinaja" : "su cuia";
                yield return ("QA-TINAJA-COMBO", string.Create(CultureInfo.InvariantCulture, $"Barro dentro de {piece} (cuia {i}): solo va fuera de la pareja"));
            }
        }
    }

    private static string List(IEnumerable<int> xs) => string.Join(", ", xs.Select(x => x.ToString(CultureInfo.InvariantCulture)));

    private static string Pairs(IEnumerable<(int, int)> pairs) =>
        "[" + string.Join(", ", pairs.Select(p => string.Create(CultureInfo.InvariantCulture, $"[{p.Item1}, {p.Item2}]"))) + "]";
}
