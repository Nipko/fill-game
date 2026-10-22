// Variantes de alivio «El río bajó» (GDD 03 §13; 08 §4.1; 03 §17 #8), con AF.Core y AF.Solver:
//   plus_empty  = el tablero con una cuia vacía de 4 más; no se resuelve (par null), sí se mide su bot_fail;
//   minus_color = el tablero sin las dosis del color que deja el par más alto entre las eliminaciones resolubles
//                 (A* w = 1 a 100.000 expandidos); a igualdad de par, el índice de motor menor (como el prototipo).
// Orden (relief_mode = auto, 03 §13): con 1 vacío libre, plus_empty primero si baja bot_fail al menos 0,20; si no, o con
// 2 vacíos, minus_color primero. Se publican las dos variantes [P] (relief[0] según la regla y relief[1], la otra), con
// el criterio de level-qa: si el base tiene bot_fail ≥ 0,20, cada variante lo baja ≥ 0,20; si no, basta par menor en
// minus_color y el vacío extra en plus_empty. Una variante que no lo cumple no se publica (lo registra el informe).
// Nunca se quita un color que sea llave de una mecánica (también la key2 de la Flor, P5) o esté dentro de una cuia con
// mecánica [P]. En un nivel con Aguas turbias las variantes van sin ocultos (el esquema no los admite en relief sin la tinaja
// [P]).
// Segunda pasada de minus_color (Q-N2 (a) de D-033; tarea B11; V3): desde el 31 el generador publica siempre una variante
// minus_color para el alivio con la Cuia extra (04 §2.2, caso b: con la Cuia extra aplicada se sirve la −1 color, nunca la de
// +1 vacío). Si la primera pasada (A* w = 1 a 100.000) no deja ninguna que cumpla el criterio, se repite la misma regla con
// A* w = 1 a MinusColorSecondBudget expandidos [P]: el par sigue siendo el óptimo exacto (w = 1), así que level-qa lo recalcula
// con ese presupuesto (MinusColorBudget) y da el mismo número; los niveles que ya tenían minus_color no cambian (una búsqueda
// que acaba dentro de 100.000 es la misma con más presupuesto). Si tampoco así hay una que cumpla, el nivel queda sin
// minus_color y el informe lo anota (pregunta Q-B11-3 de docs/dev/tutorial-b11.md).
// Tinaja (D-019; 03 §13; propuesta de la tinaja c.3 punto 4): minus_color nunca quita una fruta que esté en la tinaja (está
// dentro de una cuia con mecánica: rompería T4 y la máscara); las variantes conservan la máscara de la tinaja (todas sus capas
// menos la boca, la que exige AF.Core) y repiten la ruta segura y los paseos (TinajaWalks con TinajaWalks.ReliefOptions, las
// de Morpho: w = 2 y 40.000 nodos por realidad [P], pendiente de decisión; sin la compuerta de coste). plus_empty no se
// publica si pasa de 13 cuias (el tope de 12 del nivel, 02 §5, más su cuia vacía: decisión del dueño del 2026-10-18, D-027;
// Superposition.TinajaReliefMaxCuias). El pack escribe la máscara en el hidden de la variante (decisión del dueño del 2026-10-14, opción a de la revisión de P2a;
// 08 §4.1: relief_board admite hidden si y solo si su attrs contiene la tinaja).
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Solver.Search;
using AmazonFill.Tools.LevelGenerator.Generation;

namespace AmazonFill.Tools.LevelGenerator.Metrics;

/// <summary>Tablero en los términos del JSON (índices de motor, de abajo arriba).</summary>
internal sealed record BoardData(int[] Caps, int[][] Cuias, MechanicSpec[] Attrs, int[][] Hidden)
{
    public bool HasMechanics => Attrs.Any(a => !a.IsNone);

    public BoardBuildResult Build() => BoardBuilder.Build(Caps, Cuias, HasMechanics ? Attrs : null, Hidden.Length > 0 ? Hidden : null);

    public BoardData WithoutHidden() => this with { Hidden = [] };

    /// <summary>El tablero lleva una tinaja (M13; D-019).</summary>
    public bool HasTinaja => Attrs.Any(a => a.Type == MechanicType.Tinaja);

    /// <summary>
    /// Tablero de partida de las variantes de alivio: sin ocultos (Aguas turbias) o, con tinaja, con su máscara (todas sus
    /// capas menos la boca), que AF.Core exige.
    /// </summary>
    public BoardData ForRelief() => HasTinaja ? this : WithoutHidden();
}

internal sealed record ReliefBoard(string Type, BoardData Board, int? Removed, int? Par, double BotFail, bool Meets, string Note);

internal static class Relief
{
    /// <summary>Presupuesto de A* de la primera pasada de la variante −1 color (03 §13: «A* a 100.000»).</summary>
    public const int MinusColorFirstBudget = 100000;

    /// <summary>
    /// Presupuesto de A* (w = 1) de la segunda pasada de la variante −1 color, solo si la primera no deja ninguna que cumpla
    /// (Q-N2 (a) de D-033; B11) [P]: el menor de 1.000.000 y 2.000.000 con el que los 18 niveles de 31-700 que solo tenían
    /// plus_empty tienen al menos una eliminación resoluble (el 620 necesita 1.962.635).
    /// </summary>
    public const int MinusColorSecondBudget = 2000000;

    /// <summary>
    /// Presupuesto con el que level-qa recalcula el par de una variante −1 color publicada (QA-RELIEF-MINUS): el de la segunda
    /// pasada. Con w = 1 el resultado de una búsqueda que acaba dentro de <see cref="MinusColorFirstBudget"/> no cambia con más
    /// presupuesto, así que vale para las variantes de las dos pasadas.
    /// </summary>
    public const int MinusColorBudget = MinusColorSecondBudget;

    /// <summary>Bajada mínima de bot_fail que exige level-qa (03 §17 #8).</summary>
    public const double MinDrop = 0.20;

    /// <summary>Calcula las dos variantes en el orden de 03 §13 y devuelve las que cumplen el criterio.</summary>
    public static (List<ReliefBoard> Published, List<ReliefBoard> Rejected) Compute(
        MetricsEngine metrics, BoardData board, int basePar, double baseBot, string seedText)
    {
        BoardData visible = board.ForRelief();
        BoardState baseState = board.Build().State!;
        int freeEmpties = MetricsEngine.FreeEmpties(baseState);

        ReliefBoard plus = PlusEmpty(metrics, visible, baseBot, seedText);
        List<MinusCandidate> candidates = MinusCandidates(metrics, visible, out List<MinusCandidate> exceeded);
        var evaluated = new Dictionary<int, ReliefBoard>(); // por color: el bot ×40 de cada eliminación, una sola vez
        ReliefBoard? minus = SelectMinus(metrics, candidates, basePar, baseBot, seedText, evaluated);
        if ((minus is null || !minus.Meets) && exceeded.Count > 0)
        {
            // Q-N2 (a) (B11): sin una −1 color que cumpla, la misma regla con más presupuesto, que solo cambia algo en los colores
            // que agotaron la primera pasada (los resueltos y los irresolubles dan lo mismo con más presupuesto: A* w = 1 es
            // determinista); si tampoco así hay una que cumpla, queda la de antes.
            var all = new List<MinusCandidate>(candidates);
            foreach (MinusCandidate c in exceeded)
            {
                ParResult par = metrics.Par(c.State, MinusColorSecondBudget);
                if (par.Status == SolveStatus.Solved)
                {
                    all.Add(c with { Par = par.Par });
                }
            }

            ReliefBoard? second = SelectMinus(metrics, all, basePar, baseBot, seedText, evaluated);
            if (second is not null && (second.Meets || minus is null))
            {
                minus = second.Meets ? second with { Note = AppendNote(second.Note, "segunda pasada (Q-N2)") } : second;
            }
        }

        var ordered = new List<ReliefBoard>();
        bool plusFirst = freeEmpties <= 1 && plus.BotFail <= baseBot - MinDrop + 1e-9;
        if (plusFirst || minus is null)
        {
            ordered.Add(plus);
            if (minus is not null)
            {
                ordered.Add(minus);
            }
        }
        else
        {
            ordered.Add(minus);
            ordered.Add(plus);
        }

        return (ordered.Where(r => r.Meets).ToList(), ordered.Where(r => !r.Meets).ToList());
    }

    public static bool MeetsCriterion(string type, double baseBot, int basePar, double bot, int? par)
    {
        if (baseBot >= MinDrop - 1e-9)
        {
            return bot <= baseBot - MinDrop + 1e-9;
        }

        return type == "plus_empty" || (par is int p && p < basePar);
    }

    /// <summary>Variante +1 vacío (03 §13); interna y no privada para <c>--self-test tinaja</c> (tope de 13 cuias con tinaja, D-027).</summary>
    internal static ReliefBoard PlusEmpty(MetricsEngine metrics, BoardData board, double baseBot, string seedText)
    {
        var data = new BoardData(
            [.. board.Caps, Cuia.DefaultCapacity],
            [.. board.Cuias, []],
            [.. board.Attrs, MechanicSpec.None],
            board.Hidden);
        if (board.HasTinaja)
        {
            if (data.Caps.Length > Superposition.TinajaReliefMaxCuias)
            {
                return new ReliefBoard("plus_empty", data, null, null, 1, false,
                    "tinaja: con un vacio mas pasa del tope de 13 cuias del alivio con tinaja (D-027)");
            }

            BoardData? withPair = PlusEmptyWithTinaja(board);
            if (withPair is null)
            {
                return new ReliefBoard("plus_empty", data, null, null, 1, false, "tinaja: ninguna ranura deja la pareja en la misma fila");
            }

            data = withPair;
        }

        BoardBuildResult built = data.Build();
        if (!built.IsValid || LevelValidation.Validate(built.State!) is not null)
        {
            return new ReliefBoard("plus_empty", data, null, null, 1, false, "tablero no valido: " + (built.Error ?? "validacion"));
        }

        double bot = metrics.BotFail(built.State!, seedText);
        bool meets = MeetsCriterion("plus_empty", baseBot, 0, bot, null);
        string? tinaja = meets ? TinajaReject(data, built.State!, seedText, "plus_empty") : null;
        return new ReliefBoard("plus_empty", data, null, null, bot, meets && tinaja is null, tinaja ?? (meets ? "" : "no baja bot_fail 0,20"));
    }

    /// <summary>
    /// plus_empty con tinaja: el vacío de más va al final si la pareja sigue en la misma fila con una cuia más (las filas
    /// dependen del número de cuias, D-006); si no, en la última ranura, de atrás adelante, que la deja en la misma fila
    /// [P]. Los ocultos de la tinaja se desplazan con ella. Null si ninguna ranura lo consigue.
    /// </summary>
    private static BoardData? PlusEmptyWithTinaja(BoardData board)
    {
        int n = board.Caps.Length;
        int left = Math.Min(
            Array.FindIndex(board.Attrs, a => a.Type == MechanicType.Tinaja),
            Array.FindIndex(board.Attrs, a => a.Type == MechanicType.Cucharon));
        int at = n;
        while (at > 0 && !Superposition.SameRow(n + 1, at <= left ? left + 1 : left))
        {
            at--;
        }

        if (!Superposition.SameRow(n + 1, at <= left ? left + 1 : left))
        {
            return null;
        }

        static T[] Insert<T>(T[] a, int at, T value) => [.. a.Take(at), value, .. a.Skip(at)];
        return new BoardData(
            Insert(board.Caps, at, Cuia.DefaultCapacity),
            Insert(board.Cuias, at, []),
            Insert(board.Attrs, at, MechanicSpec.None),
            [.. board.Hidden.Select(h => new[] { h[0] >= at ? h[0] + 1 : h[0], h[1] })]);
    }

    /// <summary>
    /// Con tinaja, la variante repite la ruta segura y los paseos (03 §13; propuesta de la tinaja c.3 punto 4): null si los
    /// pasa (o si el tablero no lleva tinaja); si no, el motivo.
    /// </summary>
    private static string? TinajaReject(BoardData data, BoardState state, string seedText, string type)
    {
        if (!data.HasTinaja)
        {
            return null;
        }

        TinajaCheckResult check = new TinajaWalks().Check(state, debut: false, seedText + "/" + type, morphoGate: false, relief: true);
        return check.Reject;
    }

    /// <summary>
    /// Regla pura de 03 §13 (la que compara la paridad con el prototipo): entre las eliminaciones de un color que A* a
    /// 100.000 resuelve, la de par más alto; a igualdad, el índice menor. Sin mecánicas ni validación de nivel.
    /// </summary>
    public static (int Color, int Par)? BestMinusColor(MetricsEngine metrics, int[] caps, int[][] cuias)
    {
        (int Color, int Par)? best = null;
        foreach (int color in cuias.SelectMany(c => c).Distinct().Order())
        {
            int[][] reduced = cuias.Select(c => c.Where(x => x != color).ToArray()).ToArray();
            BoardBuildResult built = BoardBuilder.Build(caps, reduced);
            if (!built.IsValid || built.State!.IsWon())
            {
                continue;
            }

            ParResult par = metrics.Par(built.State, MinusColorFirstBudget);
            if (par.Status == SolveStatus.Solved && (best is null || par.Par > best.Value.Par))
            {
                best = (color, par.Par);
            }
        }

        return best;
    }

    private static string AppendNote(string note, string extra) => note.Length == 0 ? extra : note + "; " + extra;

    /// <summary>Una eliminación de color válida (tablero construido y validado) con su par, o −1 si aún no se resolvió.</summary>
    private sealed record MinusCandidate(int Color, int Par, BoardData Data, BoardState State);

    // Las eliminaciones de un color válidas de 03 §13, resueltas con A* w = 1 a MinusColorFirstBudget: las resueltas, en la lista
    // devuelta; las que agotaron el presupuesto, en exceeded (para la segunda pasada); las irresolubles, en ninguna.
    private static List<MinusCandidate> MinusCandidates(MetricsEngine metrics, BoardData board, out List<MinusCandidate> exceeded)
    {
        var excluded = new HashSet<int> { Cuia.MudColor };
        for (int i = 0; i < board.Attrs.Length; i++)
        {
            MechanicSpec a = board.Attrs[i];
            if (a.IsNone)
            {
                continue;
            }

            if (a.Key != 0)
            {
                excluded.Add(a.Key);
            }

            if (a.Key2 != 0)
            {
                // La segunda llave de la Flor (P5): tampoco se quita.
                excluded.Add(a.Key2);
            }

            foreach (int c in board.Cuias[i])
            {
                excluded.Add(c);
            }
        }

        int[] colors = board.Cuias.SelectMany(c => c).Where(c => !excluded.Contains(c)).Distinct().Order().ToArray();
        var candidates = new List<MinusCandidate>();
        exceeded = new List<MinusCandidate>();
        foreach (int color in colors)
        {
            var cuias = board.Cuias.Select(c => c.Where(x => x != color).ToArray()).ToArray();
            var data = board with { Cuias = cuias };
            BoardBuildResult built = data.Build();
            if (!built.IsValid || LevelValidation.Validate(built.State!) is not null)
            {
                continue;
            }

            ParResult par = metrics.Par(built.State!, MinusColorFirstBudget);
            if (par.Status == SolveStatus.Solved)
            {
                candidates.Add(new MinusCandidate(color, par.Par, data, built.State!));
            }
            else if (par.Status == SolveStatus.BudgetExceeded)
            {
                exceeded.Add(new MinusCandidate(color, -1, data, built.State!));
            }
        }

        return candidates;
    }

    // 03 §13 sobre las eliminaciones resueltas: la de par más alto y, si no alivia lo bastante, la siguiente por par [P].
    // evaluated guarda por color la evaluación ya hecha (bot ×40 y ruta segura de la tinaja), para que la segunda pasada no la
    // repita; solo se consulta por clave, nunca se recorre (el orden lo dan los candidatos).
    private static ReliefBoard? SelectMinus(MetricsEngine metrics, List<MinusCandidate> candidates, int basePar, double baseBot, string seedText,
        Dictionary<int, ReliefBoard> evaluated)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        // 03 §13: el color que deja el par más alto entre las eliminaciones resolubles; si esa no alivia lo bastante, la
        // siguiente por par (descendente) que sí lo haga [P].
        ReliefBoard? first = null;
        foreach ((int color, int par, BoardData data, BoardState state) in candidates.OrderByDescending(c => c.Par).ThenBy(c => c.Color))
        {
            if (!evaluated.TryGetValue(color, out ReliefBoard? r))
            {
                double bot = metrics.BotFail(state, seedText);
                bool meets = MeetsCriterion("minus_color", baseBot, basePar, bot, par);
                string? tinaja = meets ? TinajaReject(data, state, seedText, "minus_color") : null;
                meets &= tinaja is null;
                r = new ReliefBoard("minus_color", data, color, par, bot, meets, tinaja ?? (meets ? "" : "no alivia lo bastante"));
                evaluated[color] = r;
            }

            bool met = r.Meets;
            first ??= r;
            if (met)
            {
                return r == first ? r : r with { Note = "par mas alto sin alivio suficiente; siguiente color por par" };
            }
        }

        return first;
    }
}
