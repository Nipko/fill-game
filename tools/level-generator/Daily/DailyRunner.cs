// Generación del pool del Reto del río (R3; 03 §16; nota docs/dev/reto-del-rio.md §13). Para cada (banda, día) pedido:
//   - semilla: Prng.SeedFrom("amazonfill:v1:daily:b{b}:d{ddd}") (DailyIds); el tablero se reproduce desde su seed (03 §17 #10);
//   - K: el de k_by_day (día par el menor de la banda, impar el mayor) [P];
//   - candidatos: hasta max_candidates (30, 03 §4.4) con la fuente de la regla de ese K y, si ninguno entra en la ventana y
//     la banda tiene respaldos ("fallback", uno o una lista), otros max_candidates con cada uno, en orden. El candidato n (0, 1, ... contando
//     también los del respaldo) sale de la sub-semilla Prng.SubSeed(semilla, CandidateStream + n), y de ella, como en
//     Generation/SlotGenerator: la fuente con esa sub-semilla, los tintes con SubSeed(.., 2) y el orden visual con
//     SubSeed(.., 3). Si los tintes dejan un par del simulador de daltonismo junto en una cuia (Q-B9-13 bis (A)), las
//     asignaciones alternativas salen de SubSeed(.., 5) (CvdStream, el mismo que SlotGenerator) y solo se acepta una con el
//     mismo bot, la misma ventana y el mismo par. CandidateStream es el mismo valor que el de SlotGenerator ("GEN\0");
//   - cada candidato: tablero base de la fuente (InverseWalk, InverseDeep o FillSource de AF.Solver / Generation, sin
//     cambiarlos) con K cuias de 4 llenas y los vacíos de la banda, sin mecánicas ni ocultas; tintes (DailyTints); orden
//     visual al azar (GenBoard.Shuffled); BoardBuilder y LevelValidation de AF.Core; bot x40 (MetricsEngine.BotFail con la
//     semilla del tablero, como la campaña con la suya);
//   - aceptación: el primer candidato en la ventana bot_fail de la banda que A* resuelve (presupuesto del pipeline,
//     MetricsEngine.Par) con par >= 2K (03 §17 #2);
//   - si ningún candidato entra en la ventana, el día es un error y no se escribe el pack: la ventana es compuerta (R-Q11 (a),
//     decidida por el dueño el 2026-10-19); se ajusta la especificación (fuentes o K, [P]) y se vuelve a generar.
//     level-qa --daily (QA-DAILY-BOT) lo sigue comprobando;
//   - si la paleta de la banda no admite algún K con su tope de pares, DailyTints da error antes de generar (R-Q1b (a)).
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

    /// <summary>Pares de riesgo por encima del tope de la banda: nunca, porque DailyTints no lo admite (R-Q1b (a)).</summary>
    public bool OverRiskCap => Palette.RiskPairs > RiskCap;
}

internal sealed record DailyRun(IReadOnlyList<DailyBoard> Boards, IReadOnlyList<string> Errors, IReadOnlyDictionary<int, long> BandMilliseconds, long TotalMilliseconds, int Threads);

internal static class DailyRunner
{
    /// <summary>Flujo de sub-semillas de los candidatos: el mismo valor que <see cref="SlotGenerator.CandidateStream"/>.</summary>
    public const ulong CandidateStream = SlotGenerator.CandidateStream;

    public const ulong TintStream = 2;
    public const ulong ShuffleStream = 3;

    /// <summary>Subsemilla de las asignaciones alternativas sin contactos del simulador de daltonismo (Q-B9-13 bis (A)).</summary>
    public const ulong CvdStream = SlotGenerator.CvdStream;

    /// <summary>Tolerancia de los extremos de la ventana del bot, la misma que Generation/SlotGenerator (1e-9).</summary>
    public const double WindowTolerance = 1e-9;

    /// <summary>bot_fail dentro de la ventana [min, max] de la banda, con <see cref="WindowTolerance"/> en los extremos.</summary>
    public static bool InWindow(DailyBand band, double bot) =>
        bot >= band.BotFailMin - WindowTolerance && bot <= band.BotFailMax + WindowTolerance;

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
        public required BoardData Board { get; set; }
        public required BoardState State { get; set; }
        public required DailyPalette Palette { get; set; }
        public required double Bot { get; init; }
        public required bool InWindow { get; init; }
        public required double Distance { get; init; }
        public required bool Fallback { get; init; }
        public int Par { get; set; } = -1;
        public bool ParChecked { get; set; }

        /// <summary>
        /// El mismo candidato con la asignación de tintes de siempre, cuando este lleva otra sin contactos del simulador de
        /// daltonismo con el mismo bot (Q-B9-13 bis (A)); si el par no da lo mismo, se vuelve a él (<see cref="RevertTints"/>).
        /// </summary>
        public Built? Usual { get; set; }

        public void RevertTints()
        {
            Built u = Usual!;
            Board = u.Board;
            State = u.State;
            Palette = u.Palette;
            Par = u.Par;
            ParChecked = u.ParChecked;
            Usual = null;
        }
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
            foreach (DailySource fallback in band.Fallbacks)
            {
                stages.Add((fallback, true));
            }

            int attempt = 0;
            int tried = 0;
            foreach ((DailySource source, bool fallback) in stages)
            {
                for (int n = 0; n < band.MaxCandidates && chosen is null; n++, attempt++)
                {
                    tried++;
                    Built? c = Build(band, k, source, attempt, fallback, Prng.SubSeed(daySeed, CandidateStream + (ulong)attempt), seedText, out string? why);
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
                    if (c.Usual is Built usual)
                    {
                        string? usualReject = CheckPar(usual, k);
                        if (usualReject != parReject || usual.Par != c.Par)
                        {
                            c.RevertTints();
                            parReject = usualReject;
                        }
                    }

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

            sw.Stop();
            if (chosen is null)
            {
                // R-Q11 (a): la ventana es compuerta; sin candidato en ella, el día no se publica.
                double closest = built.Count == 0 ? double.NaN : built.Min(c => c.Distance);
                error = string.Create(CultureInfo.InvariantCulture,
                    $"{DailyIds.Id(band.Band, day)}: ningun candidato dentro de la ventana {band.BotFailMin:0.00}-{band.BotFailMax:0.00} resoluble con par >= 2K en {tried} intentos (R-Q11 (a): la ventana es compuerta; el mas cercano queda a {closest:0.000} de ella; {string.Join("; ", rejects.Select(r => r.Key + " x" + r.Value))})");
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

        private Built? Build(DailyBand band, int k, DailySource source, int attempt, bool fallback, ulong seed, string seedText, out string? why)
        {
            why = null;
            string gen = source.Gen;
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
                        InverseResult r = _deep.Generate(new InverseSpec(caps, units, band.Empties, -1, -1, PourMode.Run), seed, source.Nodes ?? InverseDeep.DefaultMaxNodes);
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
            // Pares del simulador de daltonismo (Q-B9-13 bis (A)): el candidato de siempre y, si deja un par del simulador junto
            // en una cuia, la primera asignación alternativa (subsemilla CvdStream) con el mismo bot y la misma ventana.
            GenBoard abstractBoard = board.Clone();
            int cvd = tints.CvdAdjacent(abstractBoard, palette.Map);
            Built? usual = Finish(board, palette with { CvdAdjacent = cvd }, out why);
            if (usual is null || cvd == 0)
            {
                return usual;
            }

            foreach (DailyPalette alt in tints.Alternatives(palette, k, abstractBoard, new Prng(Prng.SubSeed(seed, CvdStream))))
            {
                Built? c = Finish(abstractBoard.Clone(), alt, out _);
                if (c is not null && c.Bot.Equals(usual.Bot) && c.InWindow == usual.InWindow)
                {
                    c.Usual = usual;
                    return c;
                }
            }

            return usual;

            Built? Finish(GenBoard b, DailyPalette p, out string? w) => FinishBuilt(b, p, attempt, gen, depth, fallback, seed, seedText, band, out w);
        }

        private Built? FinishBuilt(GenBoard board, DailyPalette palette, int attempt, string gen, int depth, bool fallback, ulong seed, string seedText,
            DailyBand band, out string? why)
        {
            why = null;
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
            bool inWindow = InWindow(band, bot);
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
