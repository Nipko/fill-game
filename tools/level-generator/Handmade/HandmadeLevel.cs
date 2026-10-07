// Comprobaciones de un nivel del tutorial hecho a mano (docs/dev/prototipo-v0.md b; 02 §13; glosario §9; 03 §4.4-§5).
// Las reglas se evalúan con AF.Core (PourRules, BoardState) y el par se contrasta con AF.Solver (A*, 03 §5).
using System.Globalization;
using AF.Core.Board;
using AF.Core.Rules;
using AF.Solver.Search;

namespace AmazonFill.Tools.LevelGenerator.Handmade;

internal sealed record Check(string Name, bool Ok, string Detail);

/// <summary>Un nivel de la especificación (tintes por ID del glosario §9, dosis de abajo arriba).</summary>
internal sealed record LevelSpec(int Level, string[] Palette, string[][] Cuias, string? Note);

internal sealed class HandmadeLevel
{
    private readonly List<Check> _checks = new();

    public HandmadeLevel(LevelSpec spec)
    {
        Spec = spec;
        Row = HandmadeCanon.Row(spec.Level) ?? throw new SpecException($"nivel {spec.Level}: no está en la tabla de 02 §13 (niveles 1-10)");
        Doses = new int[spec.Cuias.Length][];
        for (int i = 0; i < spec.Cuias.Length; i++)
        {
            Doses[i] = spec.Cuias[i].Select(id => HandmadeCanon.FindTint(id)!.Engine).ToArray();
        }

        var cuias = new Cuia[Doses.Length];
        for (int i = 0; i < cuias.Length; i++)
        {
            if (Doses[i].Length > HandmadeCanon.Capacity)
            {
                throw new SpecException($"nivel {spec.Level}: la cuia {i} tiene más de {HandmadeCanon.Capacity} dosis");
            }

            cuias[i] = Cuia.Create(HandmadeCanon.Capacity, Doses[i]);
        }

        Board = new BoardState(cuias);
        Colors = Doses.SelectMany(d => d).Distinct().Order().ToArray();
    }

    public LevelSpec Spec { get; }

    public TutorialRow Row { get; }

    public int Number => Spec.Level;

    /// <summary>Dosis de abajo arriba con el índice de motor (glosario §9).</summary>
    public int[][] Doses { get; }

    public BoardState Board { get; }

    public int[] Colors { get; }

    public int Empties => Doses.Count(d => d.Length == 0);

    public IReadOnlyList<Check> Checks => _checks;

    public bool Ok => _checks.All(c => c.Ok);

    public StateGraph? Graph { get; private set; }

    public int? Par { get; private set; }

    public List<Pour>? Solution { get; private set; }

    public List<Pour>? StuckPath { get; private set; }

    public string Teaching { get; private set; } = "";

    public void Run()
    {
        TableRow();
        InitialState();
        Solvability();
        TeachingChecks();
    }

    private void Add(string name, bool ok, string detail) => _checks.Add(new Check(name, ok, detail));

    private int Spread(int color) => Doses.Count(d => d.Contains(color));

    private void TableRow()
    {
        TutorialRow row = Row;
        int full = Doses.Count(d => d.Length == HandmadeCanon.Capacity);
        Add("02 §13: cuias = colores + vacíos",
            Doses.Length == row.Cuias && Colors.Length == row.Colors && Empties == row.Empties && full == row.Colors,
            $"{Doses.Length} = {Colors.Length} + {Empties} (tabla: {row.Cuias} = {row.Colors} + {row.Empties}); {full} cuias llenas y {Empties} vacías");

        var rowEngines = row.Tints.Select(t => HandmadeCanon.FindTint(t)!.Engine).Order().ToArray();
        Add("02 §13: tintes de la fila con su ID del glosario §9",
            Spec.Palette.SequenceEqual(row.Tints) && rowEngines.SequenceEqual(Colors),
            string.Join(", ", Spec.Palette.Select(id => { Tint t = HandmadeCanon.FindTint(id)!; return $"{t.NameEs} ({t.Id} = {t.Engine})"; })));

        string[] lateDebuts = Spec.Palette.Where(id => HandmadeCanon.FindTint(id)!.DebutLevel > Number).ToArray();
        Add("debut de cada tinte ≤ nivel (glosario §9)", lateDebuts.Length == 0, lateDebuts.Length == 0 ? "ok" : string.Join(", ", lateDebuts));

        bool unitsOk = Colors.All(c => Board.UnitsOf(c) == HandmadeCanon.UnitsPerColor)
            && Enumerable.Range(0, Board.Count).All(i => Board[i].Capacity == HandmadeCanon.Capacity);
        Add("U_c = 4 por tinte y cap 4 (02 R1, R3)", unitsOk,
            string.Join(" ", Colors.Select(c => $"{HandmadeCanon.TintByEngine(c).Id}={Board.UnitsOf(c)}")));

        int risk = HandmadeCanon.RiskPairs.Count(p => Colors.Contains(p.A) && Colors.Contains(p.B));
        Add("0 pares de riesgo (max_pares_riesgo = 0 en 1-40, glosario §9)", risk <= HandmadeCanon.MaxRiskPairs,
            risk.ToString(CultureInfo.InvariantCulture));

        if (Number == 1)
        {
            bool literal = Doses.Length == HandmadeCanon.Level1Literal.Length
                && Doses.Zip(HandmadeCanon.Level1Literal).All(p => p.First.SequenceEqual(p.Second));
            Add("nivel 1: estado inicial literal de 02 §13", literal, "[8,8,8,4] [4,4,4,8] []");
        }

        Add($"BFS exhaustivo: ≤ {HandmadeCanon.MaxBfsCuias} cuias (prototipo-v0 b)", Board.Count <= HandmadeCanon.MaxBfsCuias,
            $"{Board.Count} cuias");
    }

    private void InitialState()
    {
        int sealedCount = Enumerable.Range(0, Board.Count).Count(i => Board.IsSealed(i));
        Add("no está resuelto al empezar (02 R12)", !Board.IsWon(), $"{sealedCount} cuias selladas");
        int useful = PourRules.CollectUsefulMoves(Board, true, new Pour[PourRules.MaxPours]);
        Add("no está en Aguas estancadas al empezar (02 R13; 02 §9.2 regla 1)", !PourRules.IsStuck(Board, true),
            $"{useful} vertidos útiles");
    }

    private void Solvability()
    {
        if (Board.Count > HandmadeCanon.MaxBfsCuias)
        {
            return;
        }

        var g = new StateGraph(Board);
        Graph = g;
        Par = g.Distance(Board);
        Add("resoluble (BFS exhaustivo con AF.Core.PourRules)", Par is not null, $"{g.StateCount} estados alcanzables");
        Add("par dentro del rango [P] de 02 §13", Par is not null && Par >= Row.ParMin && Par <= Row.ParMax,
            $"par {Fmt(Par)}; rango {Row.ParMin}-{Row.ParMax}");

        SolveResult astar = new AStarSolver().Solve(Board, SolverOptions.Pipeline);
        Add("par = A* de AF.Solver (03 §5, w = 1)", astar.IsSolved && astar.MoveCount == Par,
            $"A* {astar.Status}, par {(astar.IsSolved ? astar.MoveCount.ToString(CultureInfo.InvariantCulture) : "-")}, {astar.Expanded} expandidos");

        if (Par is not null)
        {
            Solution = g.OptimalSolution();
            BoardState s = Board;
            bool legal = true;
            foreach (Pour m in Solution!)
            {
                legal &= PourRules.Amount(s, m.Source, m.Target) == m.Amount;
                if (!legal)
                {
                    break;
                }

                s = PourRules.Apply(s, m.Source, m.Target);
            }

            Add("la solución del informe gana con par vertidos legales", legal && s.IsWon() && Solution.Count == Par,
                $"{Solution.Count} vertidos");
        }

        StuckPath = g.ShortestStuckPath();
    }

    private void TeachingChecks()
    {
        int maxSpread = Colors.Max(Spread);
        if (Number < 5)
        {
            Add("ningún color en 3 cuias antes del 5 (02 §13: el 5 trae el primer color problema)", maxSpread <= 2,
                $"máximo {maxSpread} cuias por color");
        }

        switch (Number)
        {
            case 1:
                Teaching = "estado literal de 02 §13";
                break;
            case 2:
                PartialThreeOnTwo();
                break;
            case 3:
                DetoursAtStart();
                break;
            case 4:
                Teaching = "debut de Guanábana";
                break;
            case 5:
                int[] threes = Colors.Where(c => Spread(c) == 3).ToArray();
                Add("nivel 5: un color repartido en 3 cuias (02 §13)", threes.Length == 1 && maxSpread == 3,
                    threes.Length == 0 ? "ninguno" : string.Join(", ", threes.Select(c => HandmadeCanon.TintByEngine(c).Id)));
                Teaching = threes.Length == 1 ? $"{HandmadeCanon.TintByEngine(threes[0]).NameEs} en 3 cuias" : "";
                break;
            case 7:
                Teaching = "debut de Bacaba";
                break;
            case 9:
            case 10:
                Teaching = "sin enseñanza de tablero";
                break;
        }

        if (Number == 6)
        {
            Add("nivel 6: un solo vacío (excepción tutorial, 02 §13)", Empties == 1 && Row.Empties == 1, $"{Empties} vacío(s)");
        }

        if (Number is 6 or 8)
        {
            string why = Number == 6 ? "primer modal" : "reinicio probable con 2 vacíos";
            Add($"nivel {Number}: Aguas estancadas alcanzable (02 §13: {why})", StuckPath is not null,
                StuckPath is null ? "no" : $"en {StuckPath.Count} vertido(s)");
            if (StuckPath is not null)
            {
                Teaching = Number == 6 ? $"1 vacío; estancadas en {StuckPath.Count}" : $"estancadas en {StuckPath.Count} con 2 vacíos";
            }
        }
    }

    /// <summary>
    /// 02 §13, nivel 2: vertido parcial de una racha de 3 sobre un hueco de 2 (R9: el resto queda en A), disponible
    /// en S0, S1 o S2 de la solución del informe y sin perder la resolubilidad.
    /// </summary>
    private void PartialThreeOnTwo()
    {
        const string name = "nivel 2: vertido parcial de una racha de 3 sobre un hueco de 2 (02 §13)";
        if (Graph is null || Solution is null)
        {
            Add(name, false, "sin solución");
            return;
        }

        (int Step, Pour Move, int Extra)? found = null;
        BoardState s = Board;
        for (int k = 0; k < 3 && found is null; k++)
        {
            foreach (Pour m in Graph.LegalPours(s))
            {
                if (s[m.Source].TopRunLength == 3 && s[m.Target].Space == 2 && m.Amount == 2)
                {
                    int? after = Graph.Distance(PourRules.Apply(s, m.Source, m.Target));
                    if (after is not null)
                    {
                        found = (k, m, after.Value - Graph.Distance(s)!.Value + 1);
                        break;
                    }
                }
            }

            if (k < Solution.Count)
            {
                s = PourRules.Apply(s, Solution[k].Source, Solution[k].Target);
            }
        }

        Add(name, found is not null, found is null
            ? "ninguno en S0-S2"
            : $"tras {found.Value.Step} vertido(s) de la solución: {Fmt(found.Value.Move)} (queda 1 en origen); sigue resoluble y cuesta {found.Value.Extra} vertido(s) sobre el par");
        if (found is not null)
        {
            Teaching = $"parcial 3 sobre 2 tras {found.Value.Step} vertido (+{found.Value.Extra})";
        }
    }

    /// <summary>02 §13, nivel 3: vertidos útiles iniciales que no acercan a la solución (ocasión para el tutorial de Marea).</summary>
    private void DetoursAtStart()
    {
        const string name = "nivel 3: hay vertidos iniciales que alejan de la solución (tutorial de Marea, 02 §13)";
        if (Graph is null || Par is null)
        {
            Add(name, false, "sin solución");
            return;
        }

        Pour[] useful = Graph.UsefulPours(Board);
        int worse = useful.Count(m =>
        {
            int? d = Graph.Distance(PourRules.Apply(Board, m.Source, m.Target));
            return d is null || d >= Par;
        });
        Add(name, worse > 0, $"{worse} de {useful.Length} vertidos útiles iniciales no acercan (Δ ≥ 0)");
        Teaching = $"{worse}/{useful.Length} vertidos iniciales no acercan";
    }

    public static string Fmt(int? value) => value is null ? "-" : value.Value.ToString(CultureInfo.InvariantCulture);

    public static string Fmt(Pour m) =>
        string.Create(CultureInfo.InvariantCulture, $"{m.Source}→{m.Target} ×{m.Amount}");
}

internal sealed class SpecException(string message) : Exception(message);
