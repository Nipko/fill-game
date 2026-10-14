// --self-test tinaja (P1 de la tanda 4): pruebas del paso «Ti» y de sus filtros sin proyecto de tests (las herramientas no
// tienen proyecto de tests en la slnx; ARQUITECTURA-T4 §6). Usa la especificación de muestra specs/sample_tinaja.json y
// semillas fijas. Comprueba:
//   - el tope de dosis por tipo de nivel (D-019, Q-Ti1 A) y que la tinaja nunca va en p1, p2 ni p19;
//   - el paso «Ti» sobre un tablero base fijo: T4 en la columna, la boca a la vista y el resto oculto (hidden), su cuia vacía
//     de 2 contigua, las dosis conservadas, ningún vacío nuevo y el mismo tablero con la misma semilla;
//   - el orden visual: la pareja contigua y en la misma fila con cualquier número de filas del cliente (D-006);
//   - el slot 323 entero (estreno): bot < 0,20, ruta segura, 20 paseos seguros ganados, ningún estado de adivinar, S0 sin
//     trampas y el JSON del pack (attrs, murky, hidden, intro_mechanic y pack_schema 3);
//   - el alivio: minus_color nunca quita una fruta de la tinaja, las variantes conservan la máscara y la pareja, y su JSON
//     lleva la máscara en hidden (08 §4.1: relief_board admite hidden si y solo si su attrs contiene la tinaja);
//   - el tope de 12 cuias con tinaja (02 §5): la etapa baja K y plus_empty no pasa de 12 cuias;
//   - «S0 sin trampas» detecta un vertido útil inseguro (el tablero perdido de b.2) y los filtros pasan en el S0 de b.2;
//   - la calibración de PcNodesPerMs (INFO): Morpho desde cero en el estado más pesado de los retos 329 y 332 y del hito 340
//     del pack de muestra (tools/out/sample_tinaja), las consultas que deciden la compuerta.
// Imprime ASCII (PASS / FAIL / INFO) y sale con 0 si todo pasa, 1 si algo falla y 2 si no encuentra la especificación.
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AF.Core.Rules;
using AF.Solver.Generation;
using AF.Solver.Murky;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal static class TinajaSelfTest
{
    public const string SpecPath = "tools/level-generator/specs/sample_tinaja.json";

    public static int Run()
    {
        CampaignSpec spec;
        try
        {
            spec = CampaignSpec.Load(SpecPath);
        }
        catch (CampaignSpecException e)
        {
            Console.Error.WriteLine(AsciiText.Of("level-generator --self-test: " + e.Message + " (ejecuta desde la raiz del repositorio)"));
            return 2;
        }

        int failures = 0;
        void Check(bool ok, string what)
        {
            Console.WriteLine(AsciiText.Of((ok ? "PASS " : "FAIL ") + what));
            failures += ok ? 0 : 1;
        }

        void Info(string what) => Console.WriteLine(AsciiText.Of("INFO " + what));

        Tiers(spec, Check);
        Rows(Check);
        StepOnFixedBase(spec, Check);
        Slot323(spec, Check, Info);
        CuiaCap(spec, Check);
        B2Boards(Check, Info);
        MorphoCalibration(spec, Check, Info);

        Console.WriteLine(failures == 0 ? "self-test tinaja: todo PASS" : $"self-test tinaja: {failures} FAIL");
        return failures == 0 ? 0 : 1;
    }

    private static void Tiers(CampaignSpec spec, Action<bool, string> check)
    {
        (int Level, (int, int)? Doses, int Empties)[] expected =
        [
            (321, null, 1), (322, null, 1), (323, (3, 3), 2), (324, (3, 3), 2), (327, (3, 3), 1), (329, (4, 5), 1),
            (338, (4, 5), 1), (339, null, 1), (340, (5, 5), 1),
        ];
        foreach ((int level, (int, int)? doses, int empties) in expected)
        {
            LevelPlan plan = spec.Plan(level);
            (int, int)? got = Superposition.TinajaDoses(plan);
            check(got == doses, F($"dosis de la tinaja en el {level} ({plan.RoleName}, p{plan.Position}): {Show(got)} (se espera {Show(doses)}; D-019, Q-Ti1 A)"));
            if (doses is not null)
            {
                check(Superposition.TinajaEmpties(plan) == empties, F($"vacios sin la pareja en el {level}: {Superposition.TinajaEmpties(plan)} (se espera {empties})"));
            }
        }

        // Una comodidad p3-p5 fuera del ciclo de estreno (el 324 sin estrella): 1 vacío, como la práctica (03 §12; 2 solo en los
        // tableros de estreno, d.3) [P], pendiente de decisión.
        LevelPlan comfort = WithPlan(spec.Plan(324), debut: false);
        check(Superposition.TinajaDoses(comfort) == (3, 3) && Superposition.TinajaEmpties(comfort) == 1,
            F($"comodidad p{comfort.Position} sin estreno: 3 dosis y {Superposition.TinajaEmpties(comfort)} vacio sin la pareja (se espera 1; 2 solo en el estreno)"));

        static string Show((int, int)? d) => d is (int a, int b) ? F($"{a}-{b}") : "sin tinaja";
    }

    /// <summary>Copia de <paramref name="p"/> con otro K o sin la estrella de estreno (para probar las reglas por tipo de nivel).</summary>
    private static LevelPlan WithPlan(LevelPlan p, int? k = null, bool? debut = null) => new()
    {
        Level = p.Level,
        World = p.World,
        Range = p.Range,
        Position = p.Position,
        Ramp = p.Ramp,
        Role = p.Role,
        Cell = p.Cell,
        K = k ?? p.K,
        Empties = p.Empties,
        Source = p.Source,
        Window = p.Window,
        AllowedLabel = p.AllowedLabel,
        TargetLabel = p.TargetLabel,
        DebutBoard = debut ?? p.DebutBoard,
        Row = p.Row,
        Intro = debut == false ? null : p.Intro,
        MaxRiskPairs = p.MaxRiskPairs,
        Season = p.Season,
    };

    /// <summary>
    /// Tope de 12 cuias con tinaja (02 §5, fila «Tinaja y su cuia»; Superposition.TinajaMaxCuias): con K = 10 u 11 en un reto
    /// (el rango 9-11 de 03 §11 para 301-400), la etapa baja K hasta 12 cuias con la pareja y el vacío, y la relajación
    /// (K − 1) queda por debajo; plus_empty no publica una variante de 13 cuias.
    /// </summary>
    private static void CuiaCap(CampaignSpec spec, Action<bool, string> check)
    {
        var gen = new SlotGenerator(spec);
        foreach (int k in new[] { 10, 11 })
        {
            LevelPlan plan = WithPlan(spec.Plan(329), k: k);
            StageConfig? s0 = gen.Config(plan, 0, dropMurky: false);
            StageConfig? s1 = gen.Config(plan, 1, dropMurky: false);
            int Cuias(StageConfig c) => c.K + c.Empties + c.Chicas.Length + (c.Slack ? 1 : 0) + 2;
            check(s0 is not null && s0.K == 9 && Cuias(s0) == Superposition.TinajaMaxCuias && s1 is not null && s1.K == 8,
                F($"reto con tinaja y K = {k}: la etapa 0 baja a K = {s0?.K} ({(s0 is null ? 0 : Cuias(s0))} cuias con la pareja) y la relajacion a K = {s1?.K} (tope de 12 cuias, 02 sec. 5)"));
        }

        // Un tablero de 12 cuias con tinaja (K = 9, 1 vacío y la pareja): plus_empty no pasa del tope.
        LevelPlan p = WithPlan(spec.Plan(329), k: 9);
        int[] caps = [.. Enumerable.Repeat(Cuia.DefaultCapacity, 10)];
        int[] units = [.. Enumerable.Repeat(Cuia.DefaultCapacity, 9)];
        BoardState baseState = new InverseWalk().Generate(new InverseSpec(caps, units, 1, -1, -1, PourMode.Run), 0x5EED0329UL).State!;
        GenBoard board = Step(spec, p, baseState, 7);
        ReliefBoard plus = Relief.PlusEmpty(gen.Metrics, board.ToData(), 1.0, p.SeedText);
        check(board.Count == Superposition.TinajaMaxCuias && !plus.Meets && plus.Board.Caps.Length == Superposition.TinajaMaxCuias + 1,
            F($"alivio de un tablero de {board.Count} cuias con tinaja: plus_empty no se publica ({plus.Note})"));
    }

    private static void Rows(Action<bool, string> check)
    {
        bool all = true;
        for (int n = 3; n <= 16; n++)
        {
            int slots = Enumerable.Range(0, n - 1).Count(left => Superposition.SameRow(n, left));
            all &= slots > 0;
        }

        // Nunca en el borde de una fila: con 12 cuias el cliente elige 6 + 6 o 4 + 4 + 4 (D-006).
        check(!Superposition.SameRow(12, 5) && !Superposition.SameRow(12, 3) && !Superposition.SameRow(12, 7) && Superposition.SameRow(12, 4),
            "con 12 cuias la pareja no cae entre las filas de 6 + 6 ni entre las de 4 + 4 + 4 (D-006)");

        check(all, "con 3-16 cuias siempre hay una ranura de la misma fila para la pareja");
        check(Superposition.T4Orders([1, 1, 2]).Count == 1 && Superposition.T4Orders([1, 1, 2])[0].SequenceEqual([1, 2, 1])
            && Superposition.T4Orders([1, 1, 1]).Count == 0 && Superposition.T4Orders([1, 2, 3]).Count == 6,
            "T4Orders: [1,1,2] -> solo 1,2,1; [1,1,1] -> ninguno; [1,2,3] -> 6");
    }

    private static void StepOnFixedBase(CampaignSpec spec, Action<bool, string> check)
    {
        foreach (int level in new[] { 323, 327, 329, 340 })
        {
            LevelPlan plan = spec.Plan(level);
            int k = level == 323 ? 6 : level == 327 ? 7 : 8;
            int empties = Superposition.TinajaEmpties(plan);
            int[] caps = [.. Enumerable.Repeat(Cuia.DefaultCapacity, k + empties)];
            int[] units = [.. Enumerable.Repeat(Cuia.DefaultCapacity, k)];
            BoardState baseState = new InverseWalk().Generate(new InverseSpec(caps, units, empties, -1, -1, PourMode.Run), 0x5EED0000UL + (ulong)level).State!;
            GenBoard first = Step(spec, plan, baseState, 7);
            GenBoard again = Step(spec, plan, baseState, 7);
            string name = F($"paso Ti en el {level} ({plan.RoleName})");
            check(Same(first, again), name + ": la misma semilla da el mismo tablero");

            int ti = first.Attrs.FindIndex(a => a.Type == MechanicType.Tinaja);
            int cu = first.Attrs.FindIndex(a => a.Type == MechanicType.Cucharon);
            List<int> column = first.Cuias[ti];
            (int min, int max) = Superposition.TinajaDoses(plan)!.Value;
            check(first.Caps[ti] == LevelValidation.TinajaCapacity && column.Count >= min && column.Count <= max,
                F($"{name}: tinaja de 5 con {column.Count} dosis ({min}-{max})"));
            check(Enumerable.Range(0, column.Count - 1).All(i => column[i] != column[i + 1]), F($"{name}: T4 en la columna [{string.Join(",", column)}]"));
            check(first.Hidden.Order().SequenceEqual(Enumerable.Range(0, column.Count - 1).Select(l => (ti, l))) && !first.IsHidden(ti, column.Count - 1),
                F($"{name}: hidden = todas las capas menos la boca"));
            check(first.Caps[cu] == LevelValidation.CucharonCapacity && first.Cuias[cu].Count == 0 && Math.Abs(ti - cu) == 1,
                F($"{name}: su cuia chica de 2, vacia y contigua"));
            var before = Enumerable.Range(0, baseState.Count).SelectMany(i => Enumerable.Range(0, baseState[i].Count).Select(l => baseState[i].ColorAt(l))).Order().ToList();
            var after = first.Cuias.SelectMany(c => c).Order().ToList();
            check(before.SequenceEqual(after), name + ": las dosis se conservan (U_c)");
            int baseEmpties = Enumerable.Range(0, baseState.Count).Count(i => baseState[i].IsEmpty);
            int newEmpties = Enumerable.Range(0, first.Count).Count(i => first.Cuias[i].Count == 0 && first.Attrs[i].IsNone);
            check(baseEmpties == newEmpties, F($"{name}: ningun vacio nuevo ({newEmpties})"));
            BoardBuildResult built = first.ToData().Build();
            check(built.IsValid && LevelValidation.Validate(built.State!) is null,
                name + ": BoardBuilder y LevelValidation (" + (built.Error ?? (built.IsValid ? LevelValidation.Validate(built.State!) : null) ?? "ok") + ")");

            bool rows = true;
            for (ulong s = 0; s < 64; s++)
            {
                GenBoard shuffled = Superposition.ShuffledWithTinaja(first, new Prng(s));
                int a = shuffled.Attrs.FindIndex(x => x.Type == MechanicType.Tinaja);
                int b = shuffled.Attrs.FindIndex(x => x.Type == MechanicType.Cucharon);
                rows &= Math.Abs(a - b) == 1 && Superposition.SameRow(shuffled.Count, Math.Min(a, b))
                    && shuffled.Hidden.All(h => h.Cuia == a) && shuffled.Cuias[a].SequenceEqual(column);
            }

            check(rows, name + ": el orden visual deja la pareja contigua y en la misma fila (64 semillas)");
        }

        foreach (int level in new[] { 321, 339 })
        {
            LevelPlan plan = spec.Plan(level);
            BoardState baseState = new InverseWalk().Generate(new InverseSpec([4, 4, 4, 4, 4, 4, 4, 4], [4, 4, 4, 4, 4, 4], 2, -1, -1, PourMode.Run), 1UL).State!;
            bool threw = false;
            try
            {
                Step(spec, plan, baseState, 7);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            check(threw, F($"el paso Ti se niega en el {level} ({plan.RoleName}): sin tinaja en p1, p2 ni p19"));
        }
    }

    private static GenBoard Step(CampaignSpec spec, LevelPlan plan, BoardState baseState, ulong seed)
    {
        GenBoard board = GenBoard.From(baseState);
        int colors = board.Units().Count;
        var ctx = new SuperpositionContext { Plan = plan, Spec = spec, Colors = colors, Units = [.. Enumerable.Repeat(Cuia.DefaultCapacity, colors)] };
        string? why = Superposition.Apply(board, spec.Mechanic("Ti"), ctx, new Prng(seed));
        return why is null ? board : throw new InvalidOperationException("paso Ti: " + why);
    }

    private static bool Same(GenBoard a, GenBoard b) =>
        a.Caps.SequenceEqual(b.Caps) && a.Attrs.SequenceEqual(b.Attrs) && a.Hidden.SequenceEqual(b.Hidden)
        && a.Cuias.Count == b.Cuias.Count && a.Cuias.Zip(b.Cuias).All(p => p.First.SequenceEqual(p.Second));

    private static void Slot323(CampaignSpec spec, Action<bool, string> check, Action<string> info)
    {
        LevelPlan plan = spec.Plan(323);
        var gen = new SlotGenerator(spec);
        SlotResult slot = gen.Generate(plan);
        Candidate? c = slot.Candidates.Where(x => x.PoolMember).OrderBy(x => x.Attempt).FirstOrDefault();
        check(c is not null, F($"slot 323 (estreno): {slot.Candidates.Count(x => x.PoolMember)} candidato(s) en ventana de {slot.Attempts} intentos"));
        if (c is null)
        {
            return;
        }

        TinajaCheckResult t = c.TinajaCheck!;
        check(c.IsTinaja && c.Bot < 0.20, F($"slot 323: tinaja y bot_fail {c.Bot:0.000} < 0,20 (estreno)"));
        check(t.Reject is null && t.SafeWalksWon == TinajaWalks.SafeWalkCount && t.CasualGuess == 0 && t.S0Traps == 0,
            F($"slot 323: ruta segura de {t.RoutePours}, paseos seguros {t.SafeWalksWon}/{t.SafeWalks}, casuales {t.CasualWon}/{t.CasualLost}/{t.CasualGuess}/{t.CasualOpen}, S0 sin trampas ({t.S0Traps})"));
        check(t.MaxRealities <= 2 && t.Doses == 3, F($"slot 323: 3 dosis y {t.MaxRealities} realidades (<= 2 en el estreno)"));
        info(F($"slot 323: Morpho en {t.MorphoStates} estados, {t.MorphoMaxNodes} nodos como maximo (tope {TinajaWalks.MorphoNodeCap})"));

        JsonObject level = JsonNode.Parse(PackWriter.Min(PackWriter.Level(spec, new PackLevel(plan, c, []))))!.AsObject();
        JsonArray attrs = level["attrs"]!.AsArray();
        int ti = attrs.Select((a, i) => (a, i)).Single(x => x.a?["type"]?.GetValue<string>() == "tinaja").i;
        int cu = attrs.Select((a, i) => (a, i)).Single(x => x.a?["type"]?.GetValue<string>() == "cucharon").i;
        int doses = level["cuias"]![ti]!.AsArray().Count;
        var hidden = level["hidden"]!.AsArray().Select(h => (h![0]!.GetValue<int>(), h[1]!.GetValue<int>())).ToList();
        check(attrs[ti]!.AsObject().Count == 1 && attrs[cu]!.AsObject().Count == 1 && Math.Abs(ti - cu) == 1,
            "JSON: attrs {\"type\":\"tinaja\"} y {\"type\":\"cucharon\"} sin campos, contiguas");
        check(level["murky"]!.GetValue<bool>() && hidden.SequenceEqual(Enumerable.Range(0, doses - 1).Select(l => (ti, l))),
            "JSON: murky = true y hidden = las capas de la tinaja menos la boca");
        check(level["intro_mechanic"]?.GetValue<string>() == "tinaja" && !level["skippable"]!.GetValue<bool>(), "JSON: intro_mechanic tinaja en el estreno (323)");
        check(PackWriter.PackSchemaFor([new PackLevel(plan, c, [])]) == PackWriter.TinajaPackSchema
            && JsonNode.Parse(PackWriter.Write(spec, plan.World, [new PackLevel(plan, c, [])]))!["pack_schema"]!.GetValue<int>() == 3,
            "JSON: pack_schema 3 en un pack con la tinaja");

        // Alivio (03 §13): minus_color nunca quita una fruta de la tinaja; las variantes conservan la máscara y la pareja.
        (List<ReliefBoard> published, List<ReliefBoard> rejected) = Relief.Compute(gen.Metrics, c.Board, c.Par, c.Bot, plan.SeedText);
        int[] inTinaja = c.Board.Cuias[Array.FindIndex(c.Board.Attrs, a => a.Type == MechanicType.Tinaja)];
        List<ReliefBoard> minus = [.. published.Concat(rejected).Where(r => r.Type == "minus_color" && r.Removed is not null)];
        check(minus.Count > 0 && minus.All(r => !inTinaja.Contains(r.Removed!.Value)),
            F($"alivio: minus_color no quita ninguna fruta de la tinaja ({string.Join(", ", minus.Select(r => r.Removed))} frente a [{string.Join(",", inTinaja)}])"));
        foreach (ReliefBoard r in published)
        {
            BoardBuildResult built = r.Board.Build();
            int a = Array.FindIndex(r.Board.Attrs, x => x.Type == MechanicType.Tinaja);
            int b = Array.FindIndex(r.Board.Attrs, x => x.Type == MechanicType.Cucharon);
            check(built.IsValid && LevelValidation.Validate(built.State!) is null && Math.Abs(a - b) == 1
                && Superposition.SameRow(r.Board.Caps.Length, Math.Min(a, b)) && r.Board.Hidden.All(h => h[0] == a) && r.Board.Hidden.Length == r.Board.Cuias[a].Length - 1,
                F($"alivio {r.Type}: valido, con la mascara de la tinaja y la pareja contigua en la misma fila"));
        }

        // JSON de las variantes (decisión del dueño del 2026-10-14; 08 §4.1): hidden = la máscara de la tinaja, con los índices
        // de la variante, tras attrs; y el pack entero sigue con pack_schema 3.
        JsonObject withRelief = JsonNode.Parse(PackWriter.Min(PackWriter.Level(spec, new PackLevel(plan, c, published))))!.AsObject();
        JsonArray reliefJson = withRelief["relief"]!.AsArray();
        bool masks = reliefJson.Count == published.Count && reliefJson.Count > 0;
        foreach (JsonNode? r in reliefJson)
        {
            JsonObject o = r!.AsObject();
            JsonArray ra = o["attrs"]!.AsArray();
            int rt = ra.Select((a, i) => (a, i)).Single(x => x.a?["type"]?.GetValue<string>() == "tinaja").i;
            int rDoses = o["cuias"]![rt]!.AsArray().Count;
            List<string> keys = [.. o.Select(kv => kv.Key)];
            masks &= o["hidden"] is JsonArray rh
                && rh.Select(h => (h![0]!.GetValue<int>(), h[1]!.GetValue<int>())).SequenceEqual(Enumerable.Range(0, rDoses - 1).Select(l => (rt, l)))
                && keys.IndexOf("hidden") == keys.IndexOf("attrs") + 1;
        }

        check(masks, F($"JSON del alivio: cada variante ({reliefJson.Count}) lleva en hidden la mascara de la tinaja, tras attrs"));
        info(F($"alivio del 323: publicadas {string.Join(" ", published.Select(r => r.Type))}; no publicadas {string.Join("; ", rejected.Select(r => r.Type + " (" + r.Note + ")"))}"));
    }

    private static void B2Boards(Action<bool, string> check, Action<string> info)
    {
        // Notación de b.2 (propuesta de la tinaja, «Morpho»): N de 4, de fondo a boca; T la tinaja (cap 5) y (o) oculta; Ch su cuia.
        const int a = 1, b = 2, c = 3;
        int[] caps = [4, 4, 4, LevelValidation.TinajaCapacity, LevelValidation.CucharonCapacity];
        MechanicSpec[] attrs = [MechanicSpec.None, MechanicSpec.None, MechanicSpec.None, new MechanicSpec(MechanicType.Tinaja), new MechanicSpec(MechanicType.Cucharon)];

        // S0 = N[c,c,a] N[a,b,a] N[b,b,b] T[c(o),a(o),c] Ch[]: tiene ruta segura (b.2).
        BoardBuildResult s0 = BoardBuilder.Build(caps, [[c, c, a], [a, b, a], [b, b, b], [c, a, c], []], attrs, [[3, 0], [3, 1]]);
        check(s0.IsValid && LevelValidation.Validate(s0.State!) is null, "b.2: S0 de la ficha valido");
        if (s0.IsValid)
        {
            var walks = new TinajaWalks();
            TinajaCheckResult r = walks.Check(s0.State!, debut: false, "amazonfill:v1:selftest:b2", morphoGate: true);
            check(r.Reject is null && r.SafeWalksWon == TinajaWalks.SafeWalkCount && r.CasualGuess == 0,
                F($"b.2: S0 con ruta segura ({r.RoutePours} vertidos), {r.SafeWalksWon}/{r.SafeWalks} paseos seguros y 0 de adivinar (casuales {r.CasualWon}/{r.CasualLost}/{r.CasualGuess}/{r.CasualOpen}) {r.Reject}"));
            int traps = walks.S0Traps(s0.State!, SafeMoves.ForPipeline(PourMode.Run, s0.State!));
            info(F($"b.2: vertidos utiles de S0 que no son seguros: {traps}"));
            info(F($"Morpho en S0 de b.2: {walks.MorphoQuery(s0.State!, s0.State!).Nodes} nodos (consulta pequena: mide sobre todo el coste fijo; la calibracion va al final)"));
        }

        // Estado perdido de b.2: N[c,c,c] N[a,b,a,a] N[b,b,b] T[c(o),a] Ch[]; el único vertido útil (sacar) no es seguro.
        BoardBuildResult lost = BoardBuilder.Build(caps, [[c, c, c], [a, b, a, a], [b, b, b], [c, a], []], attrs, [[3, 0]]);
        check(lost.IsValid, "b.2: tablero perdido construible " + (lost.Error ?? ""));
        if (lost.IsValid)
        {
            int traps = new TinajaWalks().S0Traps(lost.State!, SafeMoves.ForPipeline(PourMode.Run, lost.State!));
            check(traps == 1, F($"S0 sin trampas detecta el vertido util inseguro del tablero perdido de b.2 ({traps} de 1)"));
        }
    }

    /// <summary>Pack de muestra (lo regenera <c>--spec specs/sample_tinaja.json</c>; README del generador).</summary>
    public const string SamplePack = "tools/out/sample_tinaja/mundo_07_v1.json";

    /// <summary>Repeticiones de cada consulta en la calibración (tras una de calentamiento).</summary>
    private const int CalibrationRuns = 10;

    /// <summary>
    /// Calibración de <see cref="TinajaWalks.PcNodesPerMs"/> (revisión de P1): Morpho desde cero (w = 2, 40.000 nodos por
    /// realidad) en el estado en el que más expande de los filtros de los retos 329 y 332 y del hito 340 del pack de muestra
    /// (8.000-10.600 nodos: las consultas que deciden la compuerta), no en consultas de pocas decenas de nodos, que miden sobre
    /// todo el coste fijo. Los nodos son deterministas (PASS); los milisegundos dependen del equipo y de la carga (INFO).
    /// </summary>
    private static void MorphoCalibration(CampaignSpec spec, Action<bool, string> check, Action<string> info)
    {
        if (!File.Exists(SamplePack))
        {
            info("calibracion de Morpho: falta " + SamplePack + " (regenera la muestra; README del generador)");
            return;
        }

        JsonArray levels = JsonNode.Parse(File.ReadAllText(SamplePack))!["levels"]!.AsArray();
        long allNodes = 0;
        double allMs = 0;
        double slowest = double.MaxValue;
        foreach (int level in new[] { 329, 332, 340 })
        {
            JsonObject json = levels.Single(l => l!["level"]!.GetValue<int>() == level)!.AsObject();
            BoardBuildResult built = FromJson(json);
            LevelPlan plan = spec.Plan(level);
            var walks = new TinajaWalks();
            TinajaCheckResult r = built.IsValid ? walks.Check(built.State!, plan.DebutBoard, plan.SeedText) : new TinajaCheckResult();
            check(built.IsValid && r.Reject is null && r.MorphoMaxState is not null,
                F($"calibracion: el {level} del pack de muestra pasa los filtros de la tinaja ({r.MorphoStates} estados de Morpho, {r.MorphoMaxNodes} nodos como maximo) {built.Error}{r.Reject}"));
            if (r.MorphoMaxState is not BoardState heavy)
            {
                continue;
            }

            BoardState s0 = built.State!;
            long warm = walks.MorphoQuery(heavy, s0).Nodes;
            long nodes = 0;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < CalibrationRuns; i++)
            {
                nodes += walks.MorphoQuery(heavy, s0).Nodes;
            }

            double ms = Math.Max(1e-3, sw.Elapsed.TotalMilliseconds);
            check(warm == r.MorphoMaxNodes && nodes == CalibrationRuns * warm, F($"calibracion: Morpho desde cero en el estado mas pesado del {level} expande siempre {warm} nodos"));
            allNodes += nodes;
            allMs += ms;
            slowest = Math.Min(slowest, nodes / ms);
            info(F($"calibracion del {level}: {warm} nodos por consulta en {ms / CalibrationRuns:0.00} ms, {nodes / ms:0} nodos/ms en este PC"));
        }

        if (allMs > 0)
        {
            double rate = Math.Min(slowest, allNodes / allMs);
            info(F($"calibracion de Morpho: {allNodes / allMs:0} nodos/ms de media y {slowest:0} en el nivel mas lento (constante [E] PcNodesPerMs = {TinajaWalks.PcNodesPerMs}")
                + (rate < TinajaWalks.PcNodesPerMs ? ": ESTE PC VA POR DEBAJO; ver README" : "") + F($"; tope {TinajaWalks.MorphoNodeCap} nodos por consulta)"));
        }
    }

    /// <summary>Tablero inicial de un nivel del pack (caps, cuias, attrs y hidden; 08 §4.1).</summary>
    private static BoardBuildResult FromJson(JsonObject level)
    {
        int[] caps = [.. level["caps"]!.AsArray().Select(c => c!.GetValue<int>())];
        int[][] cuias = [.. level["cuias"]!.AsArray().Select(c => c!.AsArray().Select(x => x!.GetValue<int>()).ToArray())];
        MechanicSpec[] attrs = [.. level["attrs"]!.AsArray().Select(a => a is null ? MechanicSpec.None : Attr(a.AsObject()))];
        int[][] hidden = [.. level["hidden"]!.AsArray().Select(h => h!.AsArray().Select(x => x!.GetValue<int>()).ToArray())];
        return BoardBuilder.Build(caps, cuias, attrs.Any(a => !a.IsNone) ? attrs : null, hidden.Length > 0 ? hidden : null);

        static MechanicSpec Attr(JsonObject a)
        {
            if (!MechanicTypes.TryParse(a["type"]!.GetValue<string>(), out MechanicType type))
            {
                throw new InvalidOperationException("tipo de mecanica desconocido: " + a["type"]);
            }

            int Int(string name, int none) => a[name] is JsonNode n ? n.GetValue<int>() : none;
            return new MechanicSpec(type, Int("key", 0), Int("partner", -1), Int("n", 0), Int("k", 0));
        }
    }

    private static string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
}
