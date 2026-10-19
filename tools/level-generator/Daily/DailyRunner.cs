// Generación del pool del Reto del río (R3; 03 §16; nota docs/dev/reto-del-rio.md §13). Para cada (banda, día) pedido:
//   - semilla: Prng.SeedFrom("amazonfill:v1:daily:b{b}:d{ddd}") (DailyIds); el tablero se reproduce desde su seed (03 §17 #10);
//   - K: el de k_by_day (día par el menor de la banda, impar el mayor) [P];
//   - candidatos: hasta max_candidates (30, 03 §4.4) con la fuente de la regla de ese K y, si ninguno entra en la ventana y
//     la banda tiene "fallback" (B1: fill E=2), otros max_candidates con esa fuente. El candidato n (0, 1, ... contando
//     también los del respaldo) sale de la sub-semilla Prng.SubSeed(semilla, CandidateStream + n), y de ella, como en
//     Generation/SlotGenerator: la fuente con esa sub-semilla, los tintes con SubSeed(.., 2) y el orden visual con
//     SubSeed(.., 3). CandidateStream es el mismo valor que el de SlotGenerator ("GEN\0");
//   - cada candidato: tablero base de la fuente (InverseWalk, InverseDeep o FillSource de AF.Solver / Generation, sin
//     cambiarlos) con K cuias de 4 llenas y los vacíos de la banda, sin mecánicas ni ocultas; tintes (DailyTints); orden
//     visual al azar (GenBoard.Shuffled); BoardBuilder y LevelValidation de AF.Core; bot x40 (MetricsEngine.BotFail con la
//     semilla del tablero, como la campaña con la suya);
//   - aceptación: el primer candidato en la ventana bot_fail de la banda que A* resuelve (presupuesto del pipeline,
//     MetricsEngine.Par) con par >= 2K (03 §17 #2);
//   - si ninguno entra en la ventana (R-Q11, nota §1.6) [P]: se guarda el más cercano a la ventana (distancia de bot_fail a
//     la ventana, desempate por número de candidato) que A* resuelve con par >= 2K, y se marca. level-qa --daily (R4) lo da
//     como fallo, así que R5 no publica un pool así;
//   - si la paleta de la banda no admite ese K con su tope de pares (B2 con K = 9 y B3, nota §1.1, R-Q1b) [P]: el tablero
//     sale con el mínimo de pares posible (DailyTints) y se marca; ni K ni la paleta cambian.
// Las bandas y los días son independientes: se generan en paralelo, con un generador por hilo, y el resultado no depende
// del número de hilos (lo comprueba --self-test daily).
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Rules;
using AF.Solver.Generation;
using AF.Solver.Search;
using AmazonFill.Tools.LevelGenerator.Generation;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Daily;

/// <summary>Un tablero del pool con lo que se escribe en el pack y lo que solo va al informe.</summary>
internal sealed class DailyBoard
{
    public required int Band { get; init; }
    public required int Day { get; init; }
    public required int K { get; init; }
    public required int Empties { get; init; }
    public required DailyWorld World { get; init; }
    public required string Source { get; init; }
    public required int Depth { get; init; }

    /// <summary>Número del candidato elegido (0 = el primero); en el pack, gen.candidates = Attempt + 1.</summary>
    public required int Attempt { get; init; }

    /// <summary>Candidatos construidos para este día (incluidos los del respaldo).</summary>
    public required int Tried { get; init; }
    public required BoardData Board { get; init; }
    public required BoardState State { get; init; }
    public required DailyPalette Palette { get; init; }
    public required double Bot { get; init; }
    public required int Par { get; init; }
    public required bool InWindow { get; init; }
    public required bool UsedFallback { get; init; }
    public required int RiskCap { get; init; }
    public required int EffectiveRiskCap { get; init; }

    /// <summary>Motivo -> candidatos rechazados (orden ordinal al leerlo).</summary>
    public required SortedDictionary<string, int> Rejects { get; init; }
    public required long Milliseconds { get; init; }

    public string Id => DailyIds.Id(Band, Day);

    public string SeedText => DailyIds.Seed(Band, Day);

    /// <summary>Pares de riesgo por encima del tope de la banda (R-Q1b) [P].</summary>
    public bool OverRiskCap => Palette.RiskPairs > RiskCap;
}

internal sealed record DailyRun(IReadOnlyList<DailyBoard> Boards, IReadOnlyList<string> Errors, IReadOnlyDictionary<int, long> BandMilliseconds, long TotalMilliseconds, int Threads);

internal static class DailyRunner
{
    /// <summary>Flujo de sub-semillas de los candidatos: el mismo valor que <see cref="SlotGenerator.CandidateStream"/>.</summary>
    public const ulong CandidateStream = SlotGenerator.CandidateStream;

    public const ulong TintStream = 2;
    public const ulong ShuffleStream = 3;

    public static DailyRun Run(DailySpec spec, DailyTints tints, IReadOnlyList<int> bands, IReadOnlyList<int> days, int threads)
    {
        var jobs = new List<(int Band, int Day)>();
        foreach (int b in bands)
        {
            foreach (int d in days)
            {
                jobs.Add((b, d));
            }
        }

        var results = new DailyBoard?[jobs.Count];
        var errors = new string?[jobs.Count];
        var total = Stopwatch.StartNew();
        using var generators = new ThreadLocal<DayGenerator>(() => new DayGenerator(spec, tints), trackAllValues: false);
        Parallel.For(0, jobs.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, threads) }, i =>
        {
            (int b, int d) = jobs[i];
            results[i] = generators.Value!.Generate(spec.Band(b), d, out errors[i]);
        });
        total.Stop();

        var boards = results.Where(r => r is not null).Select(r => r!).OrderBy(r => r.Band).ThenBy(r => r.Day).ToList();
        var bandMs = new SortedDictionary<int, long>();
        foreach (DailyBoard r in boards)
        {
            bandMs[r.Band] = (bandMs.TryGetValue(r.Band, out long ms) ? ms : 0) + r.Milliseconds;
        }

        return new DailyRun(boards, errors.Where(e => e is not null).Select(e => e!).ToList(), bandMs, total.ElapsedMilliseconds, Math.Max(1, threads));
    }

    private sealed class Built
    {
        public required int Attempt { get; init; }
        public required string Source { get; init; }
        public required int Depth { get; init; }
        public required BoardData Board { get; init; }
        public required BoardState State { get; init; }
        public required DailyPalette Palette { get; init; }
        public required double Bot { get; init; }
        public required bool InWindow { get; init; }
        public required double Distance { get; init; }
        public required bool Fallback { get; init; }
        public int Par { get; set; } = -1;
        public bool ParChecked { get; set; }
    }

    private sealed class DayGenerator(DailySpec spec, DailyTints tints)
    {
        private readonly MetricsEngine _metrics = new();
        private readonly InverseWalk _walk = new();
        private readonly InverseDeep _deep = new();
        private readonly AStarSolver _fillSolver = new();

        public DailyBoard? Generate(DailyBand band, int day, out string? error)
        {
            error = null;
            var sw = Stopwatch.StartNew();
            string seedText = DailyIds.Seed(band.Band, day);
            ulong daySeed = Prng.SeedFrom(seedText);
            int k = band.KOfDay(day);
            var rejects = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var built = new List<Built>();
            Built? chosen = null;
            var stages = new List<(DailySource Source, bool Fallback)> { (band.SourceFor(k), false) };
            if (band.Fallback is not null)
            {
                stages.Add((band.Fallback, true));
            }

            int attempt = 0;
            int tried = 0;
            foreach ((DailySource source, bool fallback) in stages)
            {
                for (int n = 0; n < band.MaxCandidates && chosen is null; n++, attempt++)
                {
                    tried++;
                    Built? c = Build(band, k, source.Gen, attempt, fallback, Prng.SubSeed(daySeed, CandidateStream + (ulong)attempt), seedText, out string? why);
                    if (c is null)
                    {
                        Count(rejects, why!);
                        continue;
                    }

                    built.Add(c);
                    if (!c.InWindow)
                    {
                        Count(rejects, "fuera de la ventana del bot");
                        continue;
                    }

                    string? parReject = CheckPar(c, k);
                    if (parReject is not null)
                    {
                        Count(rejects, parReject);
                        continue;
                    }

                    chosen = c;
                }

                if (chosen is not null)
                {
                    break;
                }
            }

            if (chosen is null)
            {
                // R-Q11 [P]: el más cercano a la ventana que A* resuelve con par >= 2K.
                foreach (Built c in built.Where(c => !c.ParChecked || c.Par >= 0).OrderBy(c => c.Distance).ThenBy(c => c.Attempt))
                {
                    if (CheckPar(c, k) is null)
                    {
                        chosen = c;
                        break;
                    }
                }
            }

            sw.Stop();
            if (chosen is null)
            {
                error = string.Create(CultureInfo.InvariantCulture,
                    $"{DailyIds.Id(band.Band, day)}: ningun candidato resoluble con par >= 2K en {tried} intentos ({string.Join("; ", rejects.Select(r => r.Key + " x" + r.Value))})");
                return null;
            }

            return new DailyBoard
            {
                Band = band.Band,
                Day = day,
                K = k,
                Empties = band.Empties,
                World = spec.WorldOfDay(day),
                Source = chosen.Source,
                Depth = chosen.Depth,
                Attempt = chosen.Attempt,
                Tried = tried,
                Board = chosen.Board,
                State = chosen.State,
                Palette = chosen.Palette,
                Bot = chosen.Bot,
                Par = chosen.Par,
                InWindow = chosen.InWindow,
                UsedFallback = chosen.Fallback,
                RiskCap = band.MaxRiskPairs,
                EffectiveRiskCap = tints.EffectiveCap(band.Band, k),
                Rejects = rejects,
                Milliseconds = sw.ElapsedMilliseconds,
            };
        }

        /// <summary>Null si A* resuelve con par >= 2K; si no, el motivo. Mide una sola vez por candidato.</summary>
        private string? CheckPar(Built c, int k)
        {
            if (!c.ParChecked)
            {
                c.ParChecked = true;
                ParResult par = _metrics.Par(c.State);
                c.Par = par.Status == SolveStatus.Solved ? par.Par : -1;
                if (par.Status != SolveStatus.Solved)
                {
                    return par.Status == SolveStatus.Unsolvable ? "irresoluble" : "A* agoto el presupuesto";
                }
            }

            if (c.Par < 0)
            {
                return "sin par (irresoluble o A* agotado)";
            }

            return c.Par < 2 * k ? "par < 2K" : null;
        }

        private Built? Build(DailyBand band, int k, string gen, int attempt, bool fallback, ulong seed, string seedText, out string? why)
        {
            why = null;
            var caps = new int[k + band.Empties];
            var units = new int[k];
            Array.Fill(caps, Cuia.DefaultCapacity);
            Array.Fill(units, Cuia.DefaultCapacity);
            BoardState? baseState;
            int depth;
            try
            {
                switch (gen)
                {
                    case "fill":
                    {
                        FillResult fill = FillSource.Generate(caps, units, k, seed, _fillSolver);
                        baseState = fill.State;
                        depth = fill.Candidates;
                        break;
                    }

                    case "deep":
                    {
                        InverseResult r = _deep.Generate(new InverseSpec(caps, units, band.Empties, -1, -1, PourMode.Run), seed);
                        baseState = r.State;
                        depth = r.Depth;
                        break;
                    }

                    default:
                    {
                        InverseResult r = _walk.Generate(new InverseSpec(caps, units, band.Empties, -1, -1, PourMode.Run), seed);
                        baseState = r.State;
                        depth = r.Depth;
                        break;
                    }
                }
            }
            catch (ArgumentException e)
            {
                why = "fuente: " + e.Message;
                return null;
            }

            if (baseState is null)
            {
                why = $"la fuente {gen} no produjo tablero";
                return null;
            }

            GenBoard board = GenBoard.From(baseState);
            DailyPalette palette = tints.Assign(band, k, board, new Prng(Prng.SubSeed(seed, TintStream)));
            board.MapColors(palette.Map);
            board = board.Shuffled(new Prng(Prng.SubSeed(seed, ShuffleStream)));
            BoardData data = board.ToData();
            BoardBuildResult result = data.Build();
            if (!result.IsValid)
            {
                why = "BoardBuilder: " + result.Error;
                return null;
            }

            BoardState state = result.State!;
            string? invalid = LevelValidation.Validate(state);
            if (invalid is not null)
            {
                why = "LevelValidation";
                return null;
            }

            double bot = _metrics.BotFail(state, seedText);
            bool inWindow = bot >= band.BotFailMin && bot <= band.BotFailMax;
            double distance = inWindow ? 0 : Math.Min(Math.Abs(bot - band.BotFailMin), Math.Abs(bot - band.BotFailMax));
            return new Built
            {
                Attempt = attempt,
                Source = gen,
                Depth = depth,
                Board = data,
                State = state,
                Palette = palette,
                Bot = bot,
                InWindow = inWindow,
                Distance = distance,
                Fallback = fallback,
            };
        }

        private static void Count(SortedDictionary<string, int> rejects, string why) =>
            rejects[why] = rejects.TryGetValue(why, out int n) ? n + 1 : 1;
    }
}
