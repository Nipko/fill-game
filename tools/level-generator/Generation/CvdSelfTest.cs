// --self-test cvd (Q-B9-13 bis (b) y (A)): los pares del simulador de daltonismo (cvd_pairs: 7-10 y 5-14; 07 §3, regla 3;
// glosario §9), sin proyecto de tests (las herramientas no tienen proyecto de tests en la slnx). Comprueba:
//   (a) TintAssigner.CvdAlternatives sobre tableros escritos aquí: lista vacía sin contactos del simulador; cada alternativa
//       sin contactos del simulador, con los colores fijos intactos, los mismos tintes y sin más contactos de pares de riesgo;
//       con los colores fijos que no dejan salida, lista vacía; la misma semilla da la misma lista;
//   (b) SlotGenerator.SameColors y SameRelief: aceptan una permutación pura de los tintes (también la llave de una mecánica y
//       el tinte quitado de minus_color) y rechazan otra forma, otra capacidad, el Barro cambiado, una traducción incoherente
//       (un color a dos tintes o dos colores a uno), otro par, otro bot o el tinte quitado mal traducido;
//   (c) Candidate.RevertTints: tras volver, el candidato es igual a su Fallback en todo lo medido y ya no tiene Fallback;
//   (d) invariancia: slots del mundo 2 (13, 14, 22 y 23) y días de la banda 1 del Reto del río (4, 7, 9 y 10), que cambian de
//       tintes con cvd_pairs, generados con y sin ellos: mismos candidatos (intento, ventana, rechazo, complejidad, bot, par,
//       trampas, ramificación y D con los estadísticos de esos slots), el mismo tablero salvo una biyección de tintes y, en el
//       Reto, el mismo tablero elegido, con su par; al menos un candidato y un día cambian de verdad de tintes.
// Imprime ASCII (PASS / FAIL) y sale con 0 si todo pasa, 1 si algo falla y 2 si no encuentra las especificaciones.
using System.Globalization;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Daily;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal static class CvdSelfTest
{
    public const string CampaignV1Path = "tools/level-generator/specs/campaign_v1.json";
    public const string DailyPath = "tools/level-generator/specs/daily_v1.json";

    /// <summary>Slots del mundo 2 que cambian de tintes con cvd_pairs (el pack publicado).</summary>
    public static readonly int[] Levels = [13, 14, 22, 23];

    /// <summary>Días de la banda 1 del Reto del río que cambian de tintes con cvd_pairs (el pool publicado).</summary>
    public static readonly int[] Days = [4, 7, 9, 10];

    public static int Run()
    {
        CampaignSpec v1;
        DailySpec daily;
        CampaignSpec tintsFrom;
        try
        {
            v1 = CampaignSpec.Load(CampaignV1Path);
            daily = DailySpec.Load(DailyPath);
            tintsFrom = CampaignSpec.Load(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(DailyPath))!, daily.TintsFrom));
        }
        catch (Exception e) when (e is CampaignSpecException or DailySpecException or IOException)
        {
            Console.Error.WriteLine(AsciiText.Of("level-generator --self-test cvd: " + e.Message + " (ejecuta desde la raiz del repositorio)"));
            return 2;
        }

        int failures = 0;
        void Check(bool ok, string what)
        {
            Console.WriteLine(AsciiText.Of((ok ? "PASS " : "FAIL ") + what));
            failures += ok ? 0 : 1;
        }

        Check(v1.CvdPairs.SequenceEqual([(7, 10), (5, 14)]) && v1.WithoutCvdPairs().CvdPairs.Count == 0 && v1.CvdPairs.Count == 2,
            "cvd_pairs de campaign_v1 = 7-10 y 5-14; WithoutCvdPairs no toca el original");
        Alternatives(v1, Check);
        Comparison(Check);
        List<SlotResult> withCvd = Invariance(v1, Check);
        Revert(withCvd, Check);
        DailyInvariance(daily, tintsFrom, Check);

        Console.WriteLine(failures == 0 ? "--self-test cvd: todo PASS" : string.Create(CultureInfo.InvariantCulture, $"--self-test cvd: {failures} FAIL"));
        return failures == 0 ? 0 : 1;
    }

    // (a) ---------------------------------------------------------------------------------------------------------------

    private static GenBoard Board(params int[][] cuias)
    {
        var b = new GenBoard();
        foreach (int[] c in cuias)
        {
            b.Caps.Add(Cuia.DefaultCapacity);
            b.Cuias.Add([.. c]);
            b.Attrs.Add(MechanicSpec.None);
        }

        return b;
    }

    private static int[] Map(params (int Color, int Tint)[] pairs)
    {
        var map = new int[Cuia.MaxColor + 1];
        map[Cuia.MudColor] = Cuia.MudColor;
        foreach ((int c, int t) in pairs)
        {
            map[c] = t;
        }

        return map;
    }

    private static void Alternatives(CampaignSpec spec, Action<bool, string> check)
    {
        // Los colores 1 y 2 se tocan; el 3 no toca a nadie. Con 1 -> 7 (Bacaba) y 2 -> 10 (Agua del Tapajós), un contacto 7-10.
        GenBoard board = Board([1, 1, 2, 2], [2, 2, 1, 1], [3, 3, 3, 3], []);
        int[] map = Map((1, 7), (2, 10), (3, 4));
        check(TintAssigner.CvdAdjacent(spec, board, map) == 2, "tablero a mano: 2 contactos 7-10 con 1 -> 7, 2 -> 10");
        List<int[]> alts = TintAssigner.CvdAlternatives(spec, board, map, 3, Array.Empty<int>(), new Prng(Prng.SubSeed(42, SlotGenerator.CvdStream)));
        int risk = TintAssigner.Adjacent(spec, board, map);
        bool allOk = alts.Count > 0 && alts.All(a =>
            TintAssigner.CvdAdjacent(spec, board, a) == 0
            && TintAssigner.Adjacent(spec, board, a) <= risk
            && new[] { a[1], a[2], a[3] }.Order().SequenceEqual([4, 7, 10])
            && a[Cuia.MudColor] == Cuia.MudColor);
        check(allOk, string.Create(CultureInfo.InvariantCulture,
            $"CvdAlternatives: {alts.Count} alternativa(s), todas sin contactos del simulador, sin mas contactos de riesgo y con los mismos tintes"));
        check(alts.Select(a => string.Join(",", a)).Distinct(StringComparer.Ordinal).Count() == alts.Count, "CvdAlternatives: sin repetidas");

        List<int[]> again = TintAssigner.CvdAlternatives(spec, board, map, 3, Array.Empty<int>(), new Prng(Prng.SubSeed(42, SlotGenerator.CvdStream)));
        check(again.Count == alts.Count && again.Zip(alts).All(p => p.First.SequenceEqual(p.Second)), "CvdAlternatives: la misma semilla da la misma lista");

        // Con el 3 fijo, solo se pueden cambiar entre sí el 1 y el 2: siguen siendo 7 y 10 y se siguen tocando.
        List<int[]> fixedThree = TintAssigner.CvdAlternatives(spec, board, map, 3, [3], new Prng(Prng.SubSeed(42, SlotGenerator.CvdStream)));
        check(fixedThree.Count == 0, "CvdAlternatives: con el color fijo que deja solo 7 y 10 libres, lista vacia");

        // Con el 1 fijo (7), el 2 y el 3 se cambian: la alternativa deja el 1 en 7.
        List<int[]> fixedOne = TintAssigner.CvdAlternatives(spec, board, map, 3, [1], new Prng(Prng.SubSeed(42, SlotGenerator.CvdStream)));
        check(fixedOne.Count > 0 && fixedOne.All(a => a[1] == 7 && a[2] == 4 && a[3] == 10 && TintAssigner.CvdAdjacent(spec, board, a) == 0),
            "CvdAlternatives: el color fijo no se mueve (1 -> 7; 2 -> 4 y 3 -> 10)");

        // Sin contactos del simulador: lista vacía.
        int[] safe = Map((1, 7), (2, 4), (3, 10));
        check(TintAssigner.CvdAdjacent(spec, board, safe) == 0
              && TintAssigner.CvdAlternatives(spec, board, safe, 3, Array.Empty<int>(), new Prng(1)).Count == 0,
            "CvdAlternatives: lista vacia si no hay contactos del simulador");

        // Una alternativa nunca suma contactos de pares de riesgo: 1 y 2 tocándose, con 4 (Aguaje), 5 (Achiote) y 14 (Río Negro),
        // tres tintes que se tocan en pares de riesgo (4-5, 4-14) y del simulador (5-14).
        GenBoard risky = Board([1, 1, 2, 2], [2, 2, 1, 1], [3, 3, 3, 3], []);
        int[] riskyMap = Map((1, 5), (2, 14), (3, 4));
        int baseRisk = TintAssigner.Adjacent(spec, risky, riskyMap);
        List<int[]> riskyAlts = TintAssigner.CvdAlternatives(spec, risky, riskyMap, 3, Array.Empty<int>(), new Prng(7));
        check(baseRisk == 0 && TintAssigner.CvdAdjacent(spec, risky, riskyMap) == 2 && riskyAlts.Count == 0,
            "CvdAlternatives: con 4, 5 y 14, cualquier otra asignacion suma contactos de pares de riesgo (4-5 o 4-14): lista vacia");
    }

    // (b) ---------------------------------------------------------------------------------------------------------------

    private static BoardData Data(int[] caps, int[][] cuias, MechanicSpec[]? attrs = null) =>
        new(caps, cuias, attrs ?? caps.Select(_ => MechanicSpec.None).ToArray(), []);

    private static void Comparison(Action<bool, string> check)
    {
        int[] caps = [4, 4, 4, 4];
        BoardData a = Data(caps, [[1, 2, 1, 2], [2, 1, 2, 1], [3, 15, 3, 3], [3]],
            [new MechanicSpec(MechanicType.Hoja, key: 1), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None]);
        BoardData perm = Data(caps, [[7, 10, 7, 10], [10, 7, 10, 7], [4, 15, 4, 4], [4]],
            [new MechanicSpec(MechanicType.Hoja, key: 7), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None]);
        check(SlotGenerator.SameColors([], a, perm), "SameColors: una permutacion pura de los tintes (cuias y llave de la Hoja)");
        check(!SlotGenerator.SameColors([], a, Data(caps, [[7, 7, 10, 10], [10, 7, 10, 7], [4, 15, 4, 4], [4]],
            [new MechanicSpec(MechanicType.Hoja, key: 7), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None])), "SameColors: rechaza otra forma");
        check(!SlotGenerator.SameColors([], a, Data([4, 4, 4, 5], [[7, 10, 7, 10], [10, 7, 10, 7], [4, 15, 4, 4], [4]],
            [new MechanicSpec(MechanicType.Hoja, key: 7), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None])), "SameColors: rechaza otra capacidad");
        check(!SlotGenerator.SameColors([], a, Data(caps, [[7, 10, 7, 10], [10, 7, 10, 7], [4, 4, 4, 4], [15]],
            [new MechanicSpec(MechanicType.Hoja, key: 7), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None])), "SameColors: rechaza el Barro cambiado");
        check(!SlotGenerator.SameColors([], a, Data(caps, [[7, 10, 7, 10], [10, 7, 10, 8], [4, 15, 4, 4], [4]],
            [new MechanicSpec(MechanicType.Hoja, key: 7), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None])), "SameColors: rechaza un color traducido a dos tintes");
        check(!SlotGenerator.SameColors([], a, Data(caps, [[7, 10, 7, 10], [10, 7, 10, 7], [7, 15, 7, 7], [7]],
            [new MechanicSpec(MechanicType.Hoja, key: 7), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None])), "SameColors: rechaza dos colores traducidos a un tinte");
        check(!SlotGenerator.SameColors([], a, Data(caps, [[7, 10, 7, 10], [10, 7, 10, 7], [4, 15, 4, 4], [4]],
            [new MechanicSpec(MechanicType.Hoja, key: 10), MechanicSpec.None, MechanicSpec.None, MechanicSpec.None])), "SameColors: rechaza la llave mal traducida");

        ReliefBoard R(BoardData b, int removed, int par, double bot, string type = "minus_color") => new(type, b, removed, par, bot, true, "");
        BoardData ra = Data([4, 4, 4], [[1, 1, 1, 1], [3, 3, 3, 3], []]);
        BoardData rb = Data([4, 4, 4], [[7, 7, 7, 7], [4, 4, 4, 4], []]);
        var map = new Dictionary<int, int>();
        check(SlotGenerator.SameColors(map, a, perm) && SlotGenerator.SameRelief(map, [R(ra, 2, 10, 0.1)], [R(rb, 10, 10, 0.1)]),
            "SameRelief: la misma variante con los tintes traducidos (tablero y tinte quitado: 2 -> 10)");
        Dictionary<int, int> Fresh()
        {
            var m = new Dictionary<int, int>();
            SlotGenerator.SameColors(m, a, perm);
            return m;
        }

        check(!SlotGenerator.SameRelief(Fresh(), [R(ra, 2, 10, 0.1)], [R(rb, 7, 10, 0.1)]), "SameRelief: rechaza el tinte quitado mal traducido");
        check(!SlotGenerator.SameRelief(Fresh(), [R(ra, 2, 10, 0.1)], [R(rb, 10, 11, 0.1)]), "SameRelief: rechaza otro par");
        check(!SlotGenerator.SameRelief(Fresh(), [R(ra, 2, 10, 0.1)], [R(rb, 10, 10, 0.125)]), "SameRelief: rechaza otro bot");
        check(!SlotGenerator.SameRelief(Fresh(), [R(ra, 2, 10, 0.1)], [R(rb, 10, 10, 0.1, "plus_empty")]), "SameRelief: rechaza otro tipo");
        check(!SlotGenerator.SameRelief(Fresh(), [R(ra, 2, 10, 0.1)], []), "SameRelief: rechaza otro numero de variantes");
        check(!SlotGenerator.SameRelief(Fresh(), [R(ra, 2, 10, 0.1)], [R(Data([4, 4, 4], [[7, 7, 4, 4], [7, 7, 4, 4], []]), 10, 10, 0.1)]),
            "SameRelief: rechaza otra forma en la variante");
    }

    // (d) campaña --------------------------------------------------------------------------------------------------------

    private static List<SlotResult> Generate(CampaignSpec spec)
    {
        var results = new SlotResult[Levels.Length];
        var generators = new ThreadLocal<SlotGenerator>(() => new SlotGenerator(spec));
        Parallel.For(0, Levels.Length, i => results[i] = generators.Value!.Generate(spec.Plan(Levels[i])));
        List<SlotResult> slots = [.. results];
        Dictionary<RangeInfo, RangeStats> stats = Pool.Compute(spec, slots);
        foreach (SlotResult s in slots)
        {
            Pool.Score(s, stats[s.Plan.Range]);
        }

        return slots;
    }

    private static List<SlotResult> Invariance(CampaignSpec spec, Action<bool, string> check)
    {
        List<SlotResult> with = Generate(spec);
        List<SlotResult> without = Generate(spec.WithoutCvdPairs());
        int candidates = 0;
        int recolored = 0;
        int alternatives = 0;
        var problems = new List<string>();
        for (int i = 0; i < Levels.Length; i++)
        {
            List<Candidate> a = without[i].Candidates;
            List<Candidate> b = with[i].Candidates;
            if (a.Count != b.Count)
            {
                problems.Add(string.Create(CultureInfo.InvariantCulture, $"{Levels[i]}: {a.Count} y {b.Count} candidatos"));
                continue;
            }

            for (int j = 0; j < a.Count; j++)
            {
                Candidate x = a[j];
                Candidate y = b[j];
                candidates++;
                bool same = x.Attempt == y.Attempt && x.Bot.Equals(y.Bot) && x.InWindow == y.InWindow && x.Reject == y.Reject
                    && x.Complexity == y.Complexity && x.Measured == y.Measured && x.Par == y.Par && x.EarlyTraps == y.EarlyTraps
                    && x.Branching.Equals(y.Branching) && x.Traps.Equals(y.Traps) && x.D.Equals(y.D) && x.PoolMember == y.PoolMember
                    && x.K == y.K && SlotGenerator.SameColors([], x.Board, y.Board);
                if (!same)
                {
                    problems.Add(string.Create(CultureInfo.InvariantCulture, $"{Levels[i]} intento {x.Attempt}"));
                }

                recolored += x.Board.Cuias.Zip(y.Board.Cuias).Any(p => !p.First.SequenceEqual(p.Second)) ? 1 : 0;
                alternatives += y.Fallback is not null ? 1 : 0;
            }
        }

        check(problems.Count == 0, string.Create(CultureInfo.InvariantCulture,
            $"invariancia en los slots {string.Join(", ", Levels)} del mundo 2: {candidates} candidatos con y sin cvd_pairs con el mismo intento, bot, ventana, rechazo, par, trampas, ramificacion, D y pool, y el mismo tablero salvo los tintes{(problems.Count == 0 ? "" : "; distintos: " + string.Join("; ", problems.Take(5)))}"));
        check(recolored > 0, string.Create(CultureInfo.InvariantCulture, $"invariancia: {recolored} candidato(s) cambian de verdad de tintes con cvd_pairs"));
        check(alternatives > 0 || recolored > 0, string.Create(CultureInfo.InvariantCulture, $"invariancia: {alternatives} candidato(s) con la asignacion alternativa y su Fallback"));
        return with;
    }

    // (c) ---------------------------------------------------------------------------------------------------------------

    private static void Revert(List<SlotResult> slots, Action<bool, string> check)
    {
        Candidate? c = slots.SelectMany(s => s.Candidates).FirstOrDefault(c => c.Fallback is not null);
        if (c is null)
        {
            check(false, "RevertTints: ningun candidato con Fallback en los slots de la invariancia");
            return;
        }

        Candidate f = c.Fallback!;
        bool differs = !ReferenceEquals(c.Board, f.Board);
        c.RevertTints();
        bool same = ReferenceEquals(c.Board, f.Board) && ReferenceEquals(c.State, f.State) && ReferenceEquals(c.Tints, f.Tints)
            && c.Complexity == f.Complexity && c.MaxRealities == f.MaxRealities && c.Par == f.Par && c.Expanded == f.Expanded
            && c.EarlyTraps == f.EarlyTraps && c.Branching.Equals(f.Branching) && c.Traps.Equals(f.Traps) && c.Reject == f.Reject
            && c.Measured == f.Measured && ReferenceEquals(c.TinajaCheck, f.TinajaCheck) && c.Bot.Equals(f.Bot) && c.InWindow == f.InWindow
            && c.MeasuredLabel == f.MeasuredLabel && c.Fallback is null;
        check(differs && same, string.Create(CultureInfo.InvariantCulture,
            $"RevertTints (nivel {c.Level}, intento {c.Attempt}): el candidato queda igual a su Fallback en todo lo medido y sin Fallback"));
    }

    // (d) Reto del río ---------------------------------------------------------------------------------------------------

    private static void DailyInvariance(DailySpec spec, CampaignSpec tints, Action<bool, string> check)
    {
        DailyRun with = DailyRunner.Run(spec, new DailyTints(tints, spec), [1], Days, Environment.ProcessorCount);
        DailyRun without = DailyRunner.Run(spec, new DailyTints(tints.WithoutCvdPairs(), spec), [1], Days, Environment.ProcessorCount);
        bool ok = with.Errors.Count == 0 && without.Errors.Count == 0 && with.Boards.Count == Days.Length && without.Boards.Count == Days.Length;
        int recolored = 0;
        var problems = new List<string>();
        foreach ((DailyBoard x, DailyBoard y) in without.Boards.OrderBy(b => b.Day).Zip(with.Boards.OrderBy(b => b.Day)))
        {
            bool same = x.Day == y.Day && x.Attempt == y.Attempt && x.Tried == y.Tried && x.Bot.Equals(y.Bot) && x.Par == y.Par && x.K == y.K
                && x.Source == y.Source && x.Palette.Palette.Select(t => t.Id).SequenceEqual(y.Palette.Palette.Select(t => t.Id))
                && SlotGenerator.SameColors([], x.Board, y.Board);
            if (!same)
            {
                problems.Add(string.Create(CultureInfo.InvariantCulture, $"dia {x.Day}"));
            }

            recolored += x.Board.Cuias.Zip(y.Board.Cuias).Any(p => !p.First.SequenceEqual(p.Second)) ? 1 : 0;
        }

        check(ok && problems.Count == 0, string.Create(CultureInfo.InvariantCulture,
            $"invariancia en los dias {string.Join(", ", Days)} de la banda 1 del Reto: con y sin cvd_pairs, el mismo candidato elegido, bot, par, paleta y tablero salvo los tintes{(problems.Count == 0 ? "" : "; distintos: " + string.Join(", ", problems))}"));
        check(recolored > 0, string.Create(CultureInfo.InvariantCulture, $"invariancia del Reto: {recolored} dia(s) cambian de verdad de tintes con cvd_pairs"));
    }
}
