// Tableros de la curva 2 con las mecánicas de la 1.1 (D-068, fase F3b; 02 §5 y §9.1 M8 y M11; 03 §17 #17 y #18; 08 §4.1):
//   filas      con un grupo de Aguapé y 11-14 cuias, el nivel lleva sus filas en rows [P]: las 3 filas de D-006 (de arriba
//              abajo ⌈restantes / filas⌉: 4/4/3, 4/4/4, 5/4/4 y 5/5/4), las que el cliente elige para 11-15 cuias en el
//              lienzo de referencia (02 §5, ejemplos a 360 × 780); con 10 cuias o menos, la regla fija de 02 §5 (1 fila con ≤ 5,
//              ⌈T/2⌉ / ⌊T/2⌋ con 6-10), sin rows. Ningún grupo se parte entre filas (lo decide LevelValidation.ValidateRows).
//   alivio     plus_empty pone su vacío en la última ranura que deja cada grupo entero en una fila, y la lancha conserva
//              k >= vacíos + 1 (k sube lo justo) [P]; minus_color no mueve cuias.
//   solución   la del par (A*), repetida con PourRules: cada grupo del Aguapé se suelta y la lancha atraca (QA-AGUAPE y
//              QA-LANCHA; el generador rechaza el candidato que no lo cumple).
// Lo comparten el generador y level-qa (enlazado): una sola lectura de las filas y de la solución.
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal static class CurveBoards
{
    /// <summary>Cuias desde las que un nivel con un grupo de Aguapé lleva rows (02 §5; 08 §4.1).</summary>
    public const int RowsFrom = 11;

    /// <summary>k máximo de la Lancha (3 bits del núcleo; 02 §9.1 M11).</summary>
    public const int MaxLanchaK = 7;

    /// <summary>El tablero lleva algún grupo de Aguapé.</summary>
    public static bool HasAguape(IReadOnlyList<MechanicSpec> attrs) => attrs.Any(a => a.Type == MechanicType.Aguape);

    /// <summary>Las 3 filas de D-006 para <paramref name="count"/> cuias, de arriba abajo (⌈restantes / filas⌉).</summary>
    public static int[] ThreeRows(int count)
    {
        var rows = new int[3];
        int remaining = count;
        for (int r = 0; r < 3; r++)
        {
            rows[r] = (remaining + (3 - r) - 1) / (3 - r);
            remaining -= rows[r];
        }

        return rows;
    }

    /// <summary>
    /// Filas del tablero para comprobar los grupos: las de rows si el nivel las escribe (<see cref="RowsToWrite"/>), si no la
    /// regla fija de 02 §5 (1 fila con ≤ 5 cuias, ⌈T/2⌉ / ⌊T/2⌋ con 6-10). Null con 11 cuias o más sin grupo (el cliente elige).
    /// </summary>
    public static int[]? LayoutRows(int count, bool aguape) =>
        count <= 5 ? [count]
        : count <= 10 ? [(count + 1) / 2, count / 2]
        : aguape ? ThreeRows(count) : null;

    /// <summary>
    /// rows que escribe el pack: las que fijó el tablero (<see cref="BoardData.Rows"/>, las del alivio) o, con un grupo de Aguapé y
    /// 11 cuias o más (08 §4.1), las 3 filas de D-006; si no, null.
    /// </summary>
    public static int[]? RowsToWrite(BoardData board) =>
        board.Rows ?? (board.Caps.Length >= RowsFrom && HasAguape(board.Attrs) ? ThreeRows(board.Caps.Length) : null);

    /// <summary>
    /// Filas para un tablero de 11 o más cuias con grupos de Aguapé que no parten ningún grupo, en orden de preferencia [P]: las 3
    /// filas de D-006, con 11-12 cuias las 2 de ⌈T/2⌉ / ⌊T/2⌋, y después cualquier reparto dentro de los topes de 02 §5 (3 filas de
    /// ≤ 5, de arriba abajo de la más larga a la más corta; 2 filas de ≤ 7 solo con 12 cuias o menos: con 13, 3 filas, D-030).
    /// Null si ninguno vale.
    /// </summary>
    public static int[]? RowsKeepingGroups(IReadOnlyList<MechanicSpec> attrs, int count)
    {
        var options = new List<int[]> { ThreeRows(count) };
        if (count <= 12)
        {
            options.Add([(count + 1) / 2, count / 2]);
        }

        for (int a = 5; a >= 1; a--)
        {
            for (int b = a; b >= 1; b--)
            {
                int c = count - a - b;
                if (c >= 1 && c <= b)
                {
                    options.Add([a, b, c]);
                }
            }
        }

        if (count <= 12)
        {
            for (int a = 7; a >= 1; a--)
            {
                int b = count - a;
                if (b >= 1 && b <= 7)
                {
                    options.Add([a, b]);
                }
            }
        }

        return options.FirstOrDefault(rows => GroupsInRows(attrs, rows));
    }

    /// <summary>Ningún grupo de Aguapé cruza el límite entre dos filas de <paramref name="rows"/> (02 §9.1 M8, regla 6).</summary>
    public static bool GroupsInRows(IReadOnlyList<MechanicSpec> attrs, int[] rows)
    {
        int end = 0;
        for (int r = 0; r + 1 < rows.Length; r++)
        {
            end += rows[r];
            if (end <= 0 || end >= attrs.Count)
            {
                continue;
            }

            MechanicSpec left = attrs[end - 1];
            MechanicSpec right = attrs[end];
            if (left.Type == MechanicType.Aguape && right.Type == MechanicType.Aguape && left.Group == right.Group)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Orden visual al azar de un tablero con Aguapé (sustituye a <see cref="GenBoard.Shuffled"/> solo en sus slots): cada grupo
    /// es un bloque (sus cuias barajadas dentro), los bloques y las demás cuias se barajan con Fisher-Yates y se repite hasta
    /// que ningún grupo cruza una fila (<see cref="LayoutRows"/>). Null si en <paramref name="tries"/> intentos no sale.
    /// </summary>
    public static GenBoard? ShuffledWithGroups(GenBoard b, Prng rng, int tries = 64)
    {
        var groups = new SortedDictionary<int, List<int>>();
        var units = new List<List<int>>();
        for (int i = 0; i < b.Count; i++)
        {
            MechanicSpec a = b.Attrs[i];
            if (a.Type != MechanicType.Aguape)
            {
                units.Add([i]);
                continue;
            }

            if (!groups.TryGetValue(a.Group, out List<int>? members))
            {
                members = [];
                groups[a.Group] = members;
                units.Add(members);
            }

            members.Add(i);
        }

        int[] rows = LayoutRows(b.Count, aguape: true)!;
        for (int t = 0; t < tries; t++)
        {
            foreach (List<int> g in groups.Values)
            {
                Shuffle(g, rng);
            }

            Shuffle(units, rng);
            int[] order = [.. units.SelectMany(u => u)];
            GenBoard shuffled = b.Permuted(order);
            if (GroupsInRows(shuffled.Attrs, rows))
            {
                return shuffled;
            }
        }

        return null;
    }

    private static void Shuffle<T>(List<T> list, Prng rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// La lancha del tablero con <c>k</c> ≥ vacíos + 1 (02 §9.1 M11, regla 1): si un alivio deja más vacías que el nivel, k sube lo
    /// justo [P], y queda igual si ya cumple. El mismo tablero si no hay lancha; null si k pasaría de 7 o de T − 1.
    /// </summary>
    public static BoardData? WithLanchaK(BoardData board)
    {
        int lancha = Array.FindIndex(board.Attrs, a => a.Type == MechanicType.Lancha);
        if (lancha < 0)
        {
            return board;
        }

        int empties = Enumerable.Range(0, board.Cuias.Length).Count(i => i != lancha && board.Cuias[i].Length == 0);
        MechanicSpec a = board.Attrs[lancha];
        int k = Math.Max(a.K, empties + 1);
        if (k > MaxLanchaK || k > board.Caps.Length - 1)
        {
            return null;
        }

        if (k == a.K)
        {
            return board;
        }

        MechanicSpec[] attrs = [.. board.Attrs];
        attrs[lancha] = new MechanicSpec(MechanicType.Lancha, k: k);
        return board with { Attrs = attrs };
    }

    /// <summary>
    /// plus_empty de un tablero con Aguapé (03 §13): el vacío de más en la última ranura (de atrás adelante) que no parte ningún
    /// grupo ni lo deja cruzando una fila del tablero de T + 1 cuias (con 11 o más, con las rows de <see cref="RowsKeepingGroups"/>,
    /// que la variante publica) [P], con la lancha ajustada (<see cref="WithLanchaK"/>).
    /// Sin Aguapé, al final, como siempre. Null si ninguna ranura lo consigue.
    /// </summary>
    public static BoardData? PlusEmpty(BoardData board)
    {
        int n = board.Caps.Length;
        bool aguape = HasAguape(board.Attrs);
        for (int at = n; at >= 0; at--)
        {
            if (!aguape && at < n)
            {
                break;
            }

            MechanicSpec[] attrs = Insert(board.Attrs, at, MechanicSpec.None);
            int[]? rows = null;
            if (aguape)
            {
                // Con 10 cuias o menos, las filas fijas de 02 §5; con 11 o más, unas rows que no partan ningún grupo.
                rows = n + 1 >= RowsFrom ? RowsKeepingGroups(attrs, n + 1) : null;
                if (InsideGroup(board.Attrs, at) || (n + 1 >= RowsFrom ? rows is null : !GroupsInRows(attrs, LayoutRows(n + 1, aguape: true)!)))
                {
                    continue;
                }
            }

            var data = new BoardData(
                Insert(board.Caps, at, Cuia.DefaultCapacity),
                Insert(board.Cuias, at, []),
                attrs,
                [.. board.Hidden.Select(h => new[] { h[0] >= at ? h[0] + 1 : h[0], h[1] })])
            {
                Rows = rows,
            };
            return WithLanchaK(data);
        }

        return null;
    }

    /// <summary>La ranura <paramref name="at"/> cae entre dos cuias del mismo grupo de Aguapé.</summary>
    private static bool InsideGroup(MechanicSpec[] attrs, int at) =>
        at > 0 && at < attrs.Length && attrs[at - 1].Type == MechanicType.Aguape && attrs[at].Type == MechanicType.Aguape
        && attrs[at - 1].Group == attrs[at].Group;

    private static T[] Insert<T>(T[] a, int at, T value) => [.. a.Take(at), value, .. a.Skip(at)];

    /// <summary>
    /// Repite la solución <paramref name="solution"/> desde <paramref name="start"/> (con <see cref="PourRules.Settle"/> antes,
    /// como LevelSession) y dice qué grupos del Aguapé no se soltaron y si alguna lancha no atracó. Null si la solución no es
    /// legal o no gana (lo dice <paramref name="error"/>).
    /// </summary>
    public static (int[] Unreleased, bool LanchaDocked)? Replay(BoardState start, Pour[] solution, out string? error)
    {
        error = null;
        int[] groups = [.. Enumerable.Range(0, start.Count).Where(i => start[i].Mechanic == MechanicType.Aguape)
            .Select(i => start[i].MechanicPairId).Distinct().Order()];
        bool lancha = Enumerable.Range(0, start.Count).Any(i => start[i].Mechanic == MechanicType.Lancha);
        var released = new HashSet<int>();
        bool docked = !lancha;
        BoardState state = PourRules.Settle(start, out PourEvents settled);
        docked |= settled.LanchaDockedMask != 0;
        foreach (Pour p in solution)
        {
            // Grupos que aún llevan la mata antes del vertido (los ids no cambian en el estado: solo se pierden al soltarse).
            var before = new HashSet<int>(Enumerable.Range(0, state.Count).Where(i => state[i].Mechanic == MechanicType.Aguape)
                .Select(i => state[i].MechanicPairId));
            try
            {
                state = PourRules.Apply(state, p.Source, p.Target, PourMode.Run, out PourEvents events);
                docked |= events.LanchaDockedMask != 0;
            }
            catch (InvalidOperationException e)
            {
                error = "la solucion del par no es legal: " + e.Message;
                return null;
            }

            var after = new HashSet<int>(Enumerable.Range(0, state.Count).Where(i => state[i].Mechanic == MechanicType.Aguape)
                .Select(i => state[i].MechanicPairId));
            released.UnionWith(before.Where(g => !after.Contains(g)));
        }

        if (!state.IsWon())
        {
            error = "la solucion del par no gana";
            return null;
        }

        return ([.. groups.Where(g => !released.Contains(g))], docked);
    }

    /// <summary>
    /// Motivo de rechazo de un tablero con Aguapé o Lancha según la solución del par (03 §17 #17 y #18): un grupo que no se
    /// suelta o una lancha que no atraca. Null si cumple o si el tablero no lleva ninguna de las dos.
    /// </summary>
    public static string? SolutionProblem(BoardState start, Pour[] solution)
    {
        bool curve = Enumerable.Range(0, start.Count).Any(i => start[i].Mechanic is MechanicType.Aguape or MechanicType.Lancha);
        if (!curve)
        {
            return null;
        }

        if (Replay(start, solution, out string? error) is not (int[] unreleased, bool docked))
        {
            return error;
        }

        if (unreleased.Length > 0)
        {
            return "Aguape: el grupo " + string.Join(", ", unreleased) + " no se suelta en la solucion del par";
        }

        return docked ? null : "Lancha: no atraca en la solucion del par";
    }
}
