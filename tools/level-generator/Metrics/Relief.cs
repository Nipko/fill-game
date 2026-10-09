// Variantes de alivio «El río bajó» (GDD 03 §13; 08 §4.1; 03 §17 #8), con AF.Core y AF.Solver:
//   plus_empty  = el tablero con una cuia vacía de 4 más; no se resuelve (par null), sí se mide su bot_fail;
//   minus_color = el tablero sin las dosis del color que deja el par más alto entre las eliminaciones resolubles
//                 (A* w = 1 a 100.000 expandidos); a igualdad de par, el índice de motor menor (como el prototipo).
// Orden (relief_mode = auto, 03 §13): con 1 vacío libre, plus_empty primero si baja bot_fail al menos 0,20; si no, o con
// 2 vacíos, minus_color primero. Se publican las dos variantes [P] (relief[0] según la regla y relief[1], la otra), con
// el criterio de level-qa: si el base tiene bot_fail ≥ 0,20, cada variante lo baja ≥ 0,20; si no, basta par menor en
// minus_color y el vacío extra en plus_empty. Una variante que no lo cumple no se publica (lo registra el informe).
// Nunca se quita un color que sea llave de una mecánica o esté dentro de una cuia con mecánica [P]. En un nivel con
// Aguas turbias las variantes van sin ocultos (el esquema no los admite en relief [P]).
using AF.Core.Board;
using AF.Core.Mechanics;
using AF.Solver.Search;

namespace AmazonFill.Tools.LevelGenerator.Metrics;

/// <summary>Tablero en los términos del JSON (índices de motor, de abajo arriba).</summary>
internal sealed record BoardData(int[] Caps, int[][] Cuias, MechanicSpec[] Attrs, int[][] Hidden)
{
    public bool HasMechanics => Attrs.Any(a => !a.IsNone);

    public BoardBuildResult Build() => BoardBuilder.Build(Caps, Cuias, HasMechanics ? Attrs : null, Hidden.Length > 0 ? Hidden : null);

    public BoardData WithoutHidden() => this with { Hidden = [] };
}

internal sealed record ReliefBoard(string Type, BoardData Board, int? Removed, int? Par, double BotFail, bool Meets, string Note);

internal static class Relief
{
    /// <summary>Presupuesto de A* de la variante −1 color (03 §13).</summary>
    public const int MinusColorBudget = 100000;

    /// <summary>Bajada mínima de bot_fail que exige level-qa (03 §17 #8).</summary>
    public const double MinDrop = 0.20;

    /// <summary>Calcula las dos variantes en el orden de 03 §13 y devuelve las que cumplen el criterio.</summary>
    public static (List<ReliefBoard> Published, List<ReliefBoard> Rejected) Compute(
        MetricsEngine metrics, BoardData board, int basePar, double baseBot, string seedText)
    {
        BoardData visible = board.WithoutHidden();
        BoardState baseState = board.Build().State!;
        int freeEmpties = MetricsEngine.FreeEmpties(baseState);

        ReliefBoard plus = PlusEmpty(metrics, visible, baseBot, seedText);
        ReliefBoard? minus = MinusColor(metrics, visible, basePar, baseBot, seedText);

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

    private static ReliefBoard PlusEmpty(MetricsEngine metrics, BoardData board, double baseBot, string seedText)
    {
        var data = new BoardData(
            [.. board.Caps, Cuia.DefaultCapacity],
            [.. board.Cuias, []],
            [.. board.Attrs, MechanicSpec.None],
            []);
        BoardBuildResult built = data.Build();
        if (!built.IsValid || LevelValidation.Validate(built.State!) is not null)
        {
            return new ReliefBoard("plus_empty", data, null, null, 1, false, "tablero no valido: " + (built.Error ?? "validacion"));
        }

        double bot = metrics.BotFail(built.State!, seedText);
        bool meets = MeetsCriterion("plus_empty", baseBot, 0, bot, null);
        return new ReliefBoard("plus_empty", data, null, null, bot, meets, meets ? "" : "no baja bot_fail 0,20");
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

            ParResult par = metrics.Par(built.State, MinusColorBudget);
            if (par.Status == SolveStatus.Solved && (best is null || par.Par > best.Value.Par))
            {
                best = (color, par.Par);
            }
        }

        return best;
    }

    private static ReliefBoard? MinusColor(MetricsEngine metrics, BoardData board, int basePar, double baseBot, string seedText)
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

            foreach (int c in board.Cuias[i])
            {
                excluded.Add(c);
            }
        }

        int[] colors = board.Cuias.SelectMany(c => c).Where(c => !excluded.Contains(c)).Distinct().Order().ToArray();
        var candidates = new List<(int Color, int Par, BoardData Data, BoardState State)>();
        foreach (int color in colors)
        {
            var cuias = board.Cuias.Select(c => c.Where(x => x != color).ToArray()).ToArray();
            var data = board with { Cuias = cuias };
            BoardBuildResult built = data.Build();
            if (!built.IsValid || LevelValidation.Validate(built.State!) is not null)
            {
                continue;
            }

            ParResult par = metrics.Par(built.State!, MinusColorBudget);
            if (par.Status == SolveStatus.Solved)
            {
                candidates.Add((color, par.Par, data, built.State!));
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        // 03 §13: el color que deja el par más alto entre las eliminaciones resolubles; si esa no alivia lo bastante, la
        // siguiente por par (descendente) que sí lo haga [P].
        ReliefBoard? first = null;
        foreach ((int color, int par, BoardData data, BoardState state) in candidates.OrderByDescending(c => c.Par).ThenBy(c => c.Color))
        {
            double bot = metrics.BotFail(state, seedText);
            bool meets = MeetsCriterion("minus_color", baseBot, basePar, bot, par);
            var r = new ReliefBoard("minus_color", data, color, par, bot, meets, meets ? "" : "no alivia lo bastante");
            first ??= r;
            if (meets)
            {
                return r == first ? r : r with { Note = "par mas alto sin alivio suficiente; siguiente color por par" };
            }
        }

        return first;
    }
}
