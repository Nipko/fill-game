// Variantes de la 1.0 (P6 de la tanda 4): Liana de cuenta (Ln, 243), Hoja atada (Ha, 283), Brote de dos hojas (B2, 343) y Flor de
// vitória-régia (Fl, 403). Fuentes: 02 §9.1 (M3 y «Variantes»), 02 §9.2 reglas 1 y 2, propuesta de D-007 b.3 y la firma de
// pack_schema 3 (D-023, punto 5: hoja_atada {key}, brote con n ∈ {1, 2}, flor {key, key2} con key ≠ key2, liana con n).
// Controles por variante, cada uno con su id y su fixture (fixtures/QA-VAR-*):
//   QA-VAR-HA     la Hoja atada tiene su fruta llave en el tablero y el nivel no empieza en Aguas estancadas;
//   QA-VAR-B2     el Brote de dos hojas empieza con espacio >= 2 (b.3: «el validador exige espacio >= 2: empieza con 2 dosis como
//                 mucho»), con 1-2 dosis;
//   QA-VAR-FLOR   la Flor lleva dos frutas distintas (c1 ≠ c2), las dos en el tablero, sin dosis suyas dentro de ella y con casa
//                 fuera de ella a la vez (b.3: «como la Liana, regla 4»), contando las casas como D-024: una Hoja atada, un Matapi
//                 con dosis o la Lluvia solo son casa de su fruta, y la Liana, otra Flor o la Tinaja, de ninguna;
//   QA-VAR-LN     la Liana de cuenta lleva n de 1 a 2 (02 §9.1 M3);
//   QA-VAR-DEBUT  el tablero de estreno de cada variante la lleva sola (ninguna otra mecánica ni capacidad) y con un ejemplar
//                 (02 §9.2 regla 2).
// Se miran sobre los datos publicados (BoardData), también si BoardBuilder rechaza el tablero, y en cada variante de alivio
// (relief), que conserva las mecánicas del nivel. LevelValidation de AF.Core (QA-VALID y QA-RELIEF-VALID) sigue siendo la regla
// completa de cada mecánica (capacidad = U_c de la llave y una Hoja atada por fruta de D-024, el fondo de la Hoja atada, los n
// colores de la Liana de cuenta, el bloqueo C1-9…): aquí solo se repite, con un motivo propio, lo que pide P6.
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class VariantChecks
{
    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-VAR-HA", "Hoja atada: su fruta llave esta en el tablero y el nivel no empieza en Aguas estancadas (propuesta de D-007 b.3; 03 sec. 17 #6; 02 sec. 9.2 regla 1)"),
        ("QA-VAR-B2", "Brote de dos hojas: espacio >= 2 al empezar, con 1-2 dosis (propuesta de D-007 b.3; 02 R10 b)"),
        ("QA-VAR-FLOR", "Flor de vitoria-regia: key != key2, las dos frutas en el tablero, sin dosis suyas dentro y con casa fuera de ella a la vez; una Hoja atada, un Matapi con dosis o la Lluvia solo son casa de su fruta, y la Liana, otra Flor o la Tinaja, de ninguna (b.3; D-023 punto 5; D-024)"),
        ("QA-VAR-LN", "Liana de cuenta: n de 1 a 2 (02 sec. 9.1 M3; b.3)"),
        ("QA-VAR-DEBUT", "estreno de una variante de la 1.0: sola (sin otra mecanica ni capacidad) y un ejemplar (02 sec. 9.2 regla 2)"),
    ];

    /// <summary>Hojas del Brote de dos hojas (b.3; firma de pack_schema 3: brote con n = 2).</summary>
    private const int TwoLeaves = MechanicCodes.BroteTwoLeaves;

    /// <summary>n de la Liana de cuenta: 1 o 2 cuias selladas (02 §9.1 M3).</summary>
    private const int LianaCountMin = 1;

    private const int LianaCountMax = 2;

    public static IEnumerable<QaIssue> Run(LevelEval e)
    {
        QaLevel lv = e.Level;
        QaIssue Fail(string id, string msg) => new(id, lv.Pack, lv.Level, msg);

        bool stuck = e.State is BoardState s0 && PourRules.IsStuck(s0, true);
        foreach ((string id, string msg) in Board(lv.Board, stuck, ""))
        {
            yield return Fail(id, msg);
        }

        foreach (ReliefEval r in e.Relief)
        {
            bool reliefStuck = r.State is BoardState rs && PourRules.IsStuck(rs, true);
            foreach ((string id, string msg) in Board(r.Relief.Board, reliefStuck, $"relief {r.Relief.Type}: "))
            {
                yield return Fail(id, msg);
            }
        }

        // Estreno (02 §9.2 regla 2): el tablero de estreno de una variante la lleva sola, sin capacidades, y con un ejemplar. Lo de
        // los 2 vacíos y la ventana < 0,20 ya lo dicen QA-DEBUT y QA-WINDOW para toda mecánica.
        if (e.Plan is { DebutBoard: true } plan && plan.Role == SlotRole.Comfort)
        {
            foreach (MechanicInfo m in plan.Mechanics.Where(m => MechanicCodes.Variants.Contains(m.Code)))
            {
                SortedSet<string> codes = MechanicChecks.Codes(lv.Board);
                int[] caps = MechanicChecks.CapacityCaps(lv.Board).ToArray();
                int instances = MechanicChecks.Instances(lv.Board);
                var problems = new List<string>();
                if (codes.Count != 1 || !codes.Contains(m.Code))
                {
                    problems.Add($"mecanicas {(codes.Count == 0 ? "ninguna" : string.Join("+", codes))}");
                }

                if (caps.Length > 0)
                {
                    problems.Add("capacidades " + string.Join(", ", caps.Select(c => c.ToString(CultureInfo.InvariantCulture))));
                }

                if (instances != 1)
                {
                    problems.Add(string.Create(CultureInfo.InvariantCulture, $"{instances} ejemplares"));
                }

                if (problems.Count > 0)
                {
                    yield return Fail("QA-VAR-DEBUT", $"estreno de {m.Id} ({m.Code}) con " + string.Join("; ", problems) + ": va sola y con un ejemplar");
                }
            }
        }
    }

    /// <summary>Controles de un tablero (el del nivel o el de una variante de alivio, con <paramref name="prefix"/>).</summary>
    private static IEnumerable<(string Id, string Message)> Board(BoardData b, bool stuck, string prefix)
    {
        int[] units = Units(b);
        bool IsTintOnBoard(int key) => key >= Cuia.MinColor && key <= Cuia.MaxTintColor && units[key] > 0;
        bool hojaAtada = false;
        for (int i = 0; i < b.Attrs.Length && i < b.Cuias.Length && i < b.Caps.Length; i++)
        {
            MechanicSpec a = b.Attrs[i];
            int[] doses = b.Cuias[i];
            switch (a.Type)
            {
                case MechanicType.HojaAtada:
                    hojaAtada = true;
                    if (!IsTintOnBoard(a.Key))
                    {
                        yield return ("QA-VAR-HA", prefix + Num($"cuia {i}: la llave {a.Key} de la Hoja atada no esta en el tablero"));
                    }

                    break;

                case MechanicType.Brote when a.N == TwoLeaves:
                {
                    int space = b.Caps[i] - doses.Length;
                    if (space < TwoLeaves || doses.Length < 1 || doses.Length > TwoLeaves)
                    {
                        yield return ("QA-VAR-B2", prefix + Num(
                            $"cuia {i}: Brote de dos hojas con {doses.Length} dosis en una cuia de {b.Caps[i]} (espacio {space}); empieza con 1-2 dosis y espacio >= 2"));
                    }

                    break;
                }

                case MechanicType.Liana when a.N != 0:
                    if (a.N < LianaCountMin || a.N > LianaCountMax)
                    {
                        yield return ("QA-VAR-LN", prefix + Num($"cuia {i}: Liana de cuenta con n = {a.N}; va de {LianaCountMin} a {LianaCountMax}"));
                    }

                    break;

                case MechanicType.Flor:
                    foreach (string problem in FlorProblems(b, i, units))
                    {
                        yield return ("QA-VAR-FLOR", prefix + Num($"cuia {i}: ") + problem);
                    }

                    break;
            }
        }

        // 02 §9.2 regla 1 (y la regla 4 de la Liana que la Hoja atada repite en b.3): el nivel con la Hoja atada no empieza en
        // Aguas estancadas. Solo con un tablero que se construye (si no, ya falla QA-STRUCT o QA-RELIEF-VALID).
        if (hojaAtada && stuck)
        {
            yield return ("QA-VAR-HA", prefix + "el nivel con la Hoja atada empieza en Aguas estancadas");
        }
    }

    /// <summary>
    /// Unidades de cada color (índice = color): las dosis de las cuias publicadas más las gotas pendientes de la Lluvia, que son
    /// de su agua (02 R3: U_agua = dosis + gotas = 4).
    /// </summary>
    internal static int[] Units(BoardData b)
    {
        var units = new int[Cuia.MaxColor + 1];
        foreach (int c in b.Cuias.SelectMany(x => x))
        {
            if (c >= 0 && c < units.Length)
            {
                units[c]++;
            }
        }

        foreach (MechanicSpec a in b.Attrs.Where(a => a.Type == MechanicType.Lluvia && a.Key >= 0 && a.Key < units.Length))
        {
            units[a.Key] += a.N;
        }

        return units;
    }

    /// <summary>
    /// Motivos por los que la Flor de la cuia <paramref name="i"/> no cumple b.3 y la firma: dos frutas distintas, las dos en el
    /// tablero, ninguna dosis suya dentro de la flor y casa fuera de ella para las dos a la vez (<see cref="CanSealOutside"/>).
    /// Lo prueba <c>--self-test reglas</c> (<see cref="RulesSelfTest"/>) además de los fixtures QA-VAR-FLOR.
    /// </summary>
    internal static IEnumerable<string> FlorProblems(BoardData b, int i, int[] units)
    {
        int key = b.Attrs[i].Key;
        int key2 = b.Attrs[i].Key2;
        if (key == key2)
        {
            yield return Num($"las dos llaves son la misma fruta ({key}); la Flor lleva c1 != c2");
            yield break;
        }

        var missing = new[] { key, key2 }.Where(k => k < Cuia.MinColor || k > Cuia.MaxTintColor || units[k] == 0).ToArray();
        if (missing.Length > 0)
        {
            yield return "la llave " + string.Join(" y la ", missing.Select(k => k.ToString(CultureInfo.InvariantCulture))) + " no esta en el tablero";
            yield break;
        }

        int[] inside = b.Cuias[i].Where(c => c == key || c == key2).Distinct().Order().ToArray();
        if (inside.Length > 0)
        {
            yield return "la fruta " + string.Join(" y la ", inside.Select(k => k.ToString(CultureInfo.InvariantCulture)))
                + " tiene dosis dentro de la Flor: no podria sellar fuera de ella";
        }

        if (!CanSealOutside(b, i, key, key2, units))
        {
            yield return Num($"las frutas {key} (U_c = {units[key]}) y {key2} (U_c = {units[key2]}) no tienen casa fuera de la Flor a la vez")
                + " (una Hoja atada, un Matapi con dosis o la Lluvia solo son casa de su fruta; la Liana, otra Flor, la Tinaja y su cuia, de ninguna)";
        }
    }

    /// <summary>
    /// Las dos frutas de la Flor de la cuia <paramref name="flor"/> pueden sellar fuera de ella a la vez: una casa para cada
    /// una, distintas (b.3, «como la Liana, regla 4»). Una casa de la fruta f es otra cuia de capacidad U_f que puede recibirla y
    /// sellarla (D-024 y LevelValidation: «una Hoja atada o un Matapi con dosis solo es casa de su fruta»):
    /// <list type="bullet">
    /// <item><description>la Hoja atada, solo de su llave; el Matapi con dosis, solo de la fruta de sus dosis (vacío, de
    /// cualquiera); la cuia de la Lluvia, solo de su agua (solo sella con agua, M6);</description></item>
    /// <item><description>la Liana (con fruta o de cuenta) y otra Flor, de ninguna: están cerradas hasta que sellen otras frutas,
    /// y se cuentan como si no se abrieran (cota conservadora); la Tinaja y su cuia, de ninguna (nunca reciben, M13);</description></item>
    /// <item><description>el resto (sin mecánica, Hoja, Brote, Remolino, con Barro…), de cualquier fruta de su capacidad.</description></item>
    /// </list>
    /// LevelValidation de AF.Core solo cuenta las capacidades en esta regla de la Flor; aquí se cuentan las casas de verdad.
    /// </summary>
    internal static bool CanSealOutside(BoardData b, int flor, int key, int key2, int[] units)
    {
        bool own1 = false;
        bool own2 = false;
        int open1 = 0;
        int open2 = 0;
        for (int j = 0; j < b.Caps.Length && j < b.Attrs.Length && j < b.Cuias.Length; j++)
        {
            if (j == flor)
            {
                continue;
            }

            int only = HomeOnlyOf(b, j);
            if (only == NoHome)
            {
                continue;
            }

            int cap = b.Caps[j];
            if (only == AnyFruit)
            {
                open1 += cap == units[key] ? 1 : 0;
                open2 += cap == units[key2] ? 1 : 0;
            }
            else
            {
                own1 |= only == key && cap == units[key];
                own2 |= only == key2 && cap == units[key2];
            }
        }

        int need1 = own1 ? 0 : 1;
        int need2 = own2 ? 0 : 1;
        return units[key] == units[key2] ? open1 >= need1 + need2 : open1 >= need1 && open2 >= need2;
    }

    private const int AnyFruit = -1;

    private const int NoHome = -2;

    /// <summary>De qué fruta puede ser casa la cuia <paramref name="j"/>: <see cref="AnyFruit"/>, <see cref="NoHome"/> o una fruta.</summary>
    private static int HomeOnlyOf(BoardData b, int j)
    {
        MechanicSpec a = b.Attrs[j];
        int[] doses = b.Cuias[j];
        switch (a.Type)
        {
            case MechanicType.HojaAtada:
                return a.Key;
            case MechanicType.Matapi:
                return doses.Length == 0 ? AnyFruit : doses.Distinct().Count() == 1 ? doses[0] : NoHome;
            case MechanicType.Lluvia:
                return a.Key;
            case MechanicType.Liana:
            case MechanicType.Flor:
            case MechanicType.Tinaja:
            case MechanicType.Cucharon:
                return NoHome;
            default:
                return AnyFruit;
        }
    }

    private static string Num(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
