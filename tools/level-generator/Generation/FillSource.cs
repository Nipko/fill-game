// Relleno aleatorio + rechazo (FILL de GDD 03 §4.3) con el PRNG del núcleo (xoshiro256**, 09 R26): las U_c dosis de
// cada color se barajan con Fisher-Yates y se reparten en las cuias «casa» (cap = U_c, en el orden de caps); el resto de
// cuias (vacíos, Cuia chica) empiezan vacías. Se rechaza el candidato con una cuia sellada, atascado al inicio o
// irresoluble; se acepta el primero que A* (w = 1, run) resuelve con par ≥ 2K o que agota el presupuesto de rechazo (lo
// decide la verificación del slot). Reglas, sellado y bloqueo: AF.Core.
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Rules;
using AF.Solver.Search;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal sealed record FillResult(BoardState? State, int Candidates, int Par, int Expanded);

internal static class FillSource
{
    /// <summary>Candidatos por llamada (03 §4.3: <c>max_cand</c>; con 1 vacío la aceptación cae al 3-7 % con K = 7-8).</summary>
    public const int DefaultMaxCandidates = 200;

    /// <summary>
    /// Presupuesto de A* para el rechazo de fill [P]: 20.000 expandidos. Basta para probar la irresolubilidad de los tableros
    /// de 1 vacío (1-8 ms, 03 §1 hecho 5) y para resolver los fáciles; si se agota, el candidato pasa y su par exacto lo
    /// calcula después la verificación del slot (A* a 300.000, 03 §4.3), que también exige par ≥ 2K.
    /// </summary>
    public const int AcceptBudget = 20000;

    public static FillResult Generate(int[] caps, int[] units, int colors, ulong seed, AStarSolver solver, int maxCandidates = DefaultMaxCandidates)
    {
        var rng = new Prng(seed);
        var pool = new List<int>();
        for (int c = 1; c <= colors; c++)
        {
            for (int u = 0; u < units[c - 1]; u++)
            {
                pool.Add(c);
            }
        }

        // Casas: en el orden de caps, la primera cuia libre con cap = U_c de cada color (como InverseSpec).
        var home = new bool[caps.Length];
        for (int c = 1; c <= colors; c++)
        {
            int i = Array.FindIndex(caps, x => x == units[c - 1]);
            while (i >= 0 && home[i])
            {
                i = Array.FindIndex(caps, i + 1, x => x == units[c - 1]);
            }

            if (i < 0)
            {
                throw new InvalidOperationException("FILL: las capacidades no alojan los colores (02 §8)");
            }

            home[i] = true;
        }

        var table = new int[CuiaRules.UnitsTableLength];
        for (int c = 1; c <= colors; c++)
        {
            table[c] = units[c - 1];
        }

        int[] shuffled = pool.ToArray();
        var cuias = new Cuia[caps.Length];
        var options = new SolverOptions(AcceptBudget, 1, true, PourMode.Run);
        for (int candidate = 1; candidate <= maxCandidates; candidate++)
        {
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            int next = 0;
            for (int i = 0; i < caps.Length; i++)
            {
                if (!home[i])
                {
                    cuias[i] = Cuia.Empty(caps[i]);
                    continue;
                }

                cuias[i] = Cuia.Create(caps[i], shuffled[next..(next + caps[i])]);
                next += caps[i];
            }

            bool sealedAny = false;
            for (int i = 0; i < cuias.Length && !sealedAny; i++)
            {
                sealedAny = CuiaRules.IsSealed(cuias[i], table);
            }

            if (sealedAny || BoardRules.IsStuck(cuias, 0, cuias.Length, table, true))
            {
                continue;
            }

            var state = new BoardState(cuias);
            SolveResult r = solver.Solve(state, options);
            if ((r.IsSolved && r.MoveCount >= 2 * colors) || r.Status == SolveStatus.BudgetExceeded)
            {
                return new FillResult(state, candidate, r.IsSolved ? r.MoveCount : -1, r.Expanded);
            }
        }

        return new FillResult(null, maxCandidates, -1, 0);
    }
}
