// --self-test variants (P5 de la tanda 4): pruebas de las variantes de la 1.0 en el generador sin proyecto de tests (las
// herramientas no tienen proyecto de tests en la slnx; ARQUITECTURA-T4 §6). Variantes de 02 §9.1 (propuesta de D-007 b.3;
// firma de pack_schema 3, D-023 punto 5): Liana de cuenta (Ln), Hoja atada (Ha), Brote de dos hojas (B2) y Flor de
// vitória-régia (Fl). Usa la especificación de muestra specs/sample_variants.json, campaign_v1.json y semillas fijas.
// Comprueba:
//   - los códigos del calendario (MechanicCodes.CodeOf: Li frente a Ln, Br frente a B2, Ha, Fl y Ti) y que las tres
//     especificaciones solo usan códigos conocidos; una tabla mechanics con un código desconocido (copia temporal de la
//     muestra con una fila Xx) falla al cargar con CampaignSpecException «codigo de mecanica desconocido»;
//   - que la superposición conserva el orden de antes para las demás mecánicas (Superposition.OrderBeforeVariants) y que un
//     tablero sin variantes consume los mismos números aleatorios con el orden nuevo que con el de antes (los packs 11-200 no
//     cambian; el --check lo confirma con los packs);
//   - cada paso (Ln, Ha, B2 y Fl) sobre un tablero base fijo con 40 semillas: la colocación es válida según BoardBuilder y
//     LevelValidation, con los campos de b.3 (n de la Liana 1 en el estreno y 1-2 fuera de él; Hoja atada sobre su fruta en
//     el fondo con cap = U_c; Brote con n = 2 y espacio ≥ 2; Flor con key ≠ key2 fuera de la cuia), y la misma semilla da el
//     mismo tablero;
//   - la key2 de la Flor al cambiar los colores (GenBoard.MapColors) y al reordenar (Permuted);
//   - el estreno de cada variante en la muestra (243, 263, 283 y 303): un candidato en ventana con un solo ejemplar, 2 vacíos y
//     bot_fail < 0,20 (02 §9.2 regla 2), válido, y su JSON (attrs de la firma, intro_mechanic, skippable = false y
//     pack_schema 3) leído de vuelta con el lector del generador da el mismo tablero y el mismo hash (el PackReader de level-qa
//     lo comprueba RunPackReadBack, desde el --self-test key2 de level-qa); la Flor sin key2 no se construye; el alivio no quita
//     las llaves (también la key2) y sus variantes son válidas;
//   - los fixtures negativos de las variantes (specs/tests: LIANA-CAPS con la Liana de cuenta y con la Flor, BEFORE-DEBUT del
//     Brote de dos hojas) y la muestra limpia en --validate-spec.
// Imprime ASCII (PASS / FAIL / INFO) y sale con 0 si todo pasa, 1 si algo falla y 2 si no encuentra la especificación.
using System.Globalization;
using System.Text.Json.Nodes;
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal static class VariantsSelfTest
{
    public const string SpecPath = "tools/level-generator/specs/sample_variants.json";
    public const string CampaignPath = "tools/level-generator/specs/campaign_v1.json";

    /// <summary>Fixtures negativos de las variantes (specs/tests) y la regla que tiene que fallar en cada uno.</summary>
    public static readonly (string Path, string Rule)[] Fixtures =
    [
        ("tools/level-generator/specs/tests/liana_n_with_capacity.json", "LIANA-CAPS"),
        ("tools/level-generator/specs/tests/flor_with_pirana.json", "LIANA-CAPS"),
        ("tools/level-generator/specs/tests/brote_n2_before_debut.json", "BEFORE-DEBUT"),
    ];

    /// <summary>Semillas de las pruebas de los pasos sobre el tablero base fijo.</summary>
    private const int Seeds = 40;

    public static int Run()
    {
        CampaignSpec spec;
        CampaignSpec campaign;
        try
        {
            spec = CampaignSpec.Load(SpecPath);
            campaign = CampaignSpec.Load(CampaignPath);
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

        Codes(spec, campaign, Check);
        Order(campaign, Check);
        StepsOnFixedBase(spec, Check);
        KeysThroughColorsAndOrder(Check);
        Debuts(spec, Check, Info);
        SpecFixtures(spec, Check);

        Console.WriteLine(failures == 0 ? "self-test variants: todo PASS" : $"self-test variants: {failures} FAIL");
        return failures == 0 ? 0 : 1;
    }

    private static void Codes(CampaignSpec spec, CampaignSpec campaign, Action<bool, string> check)
    {
        (MechanicSpec Attr, string? Code)[] cases =
        [
            (MechanicSpec.None, null),
            (new MechanicSpec(MechanicType.Liana, key: 3), "Li"),
            (new MechanicSpec(MechanicType.Liana, n: 1), "Ln"),
            (new MechanicSpec(MechanicType.Liana, n: 2), "Ln"),
            (new MechanicSpec(MechanicType.Brote), "Br"),
            (new MechanicSpec(MechanicType.Brote, n: 2), "B2"),
            (new MechanicSpec(MechanicType.HojaAtada, key: 4), "Ha"),
            (new MechanicSpec(MechanicType.Flor, key: 3, key2: 7), "Fl"),
            (new MechanicSpec(MechanicType.Hoja, key: 4), "Ho"),
            (new MechanicSpec(MechanicType.Tinaja), "Ti"),
            (new MechanicSpec(MechanicType.Cucharon), "Ti"),
        ];
        bool ok = cases.All(c => MechanicCodes.CodeOf(c.Attr) == c.Code);
        check(ok, "codigos: liana con key = Li y con n = Ln; brote = Br y con n = 2 = B2; hoja_atada = Ha; flor = Fl; tinaja y cucharon = Ti");
        check(MechanicCodes.Variants.SequenceEqual(["Ln", "Ha", "B2", "Fl"]) && MechanicCodes.Variants.All(MechanicCodes.IsKnown),
            "codigos de las variantes: Ln, Ha, B2 y Fl (los [P] de P5, iguales a los de d.5.1 de D2)");
        CampaignSpec tinaja = CampaignSpec.Load(TinajaSelfTest.SpecPath);
        check(new[] { spec, campaign, tinaja }.All(s => s.MechanicTable.All(m => MechanicCodes.IsKnown(m.Code))),
            "las especificaciones (muestra de variantes, campaign_v1 y muestra de la tinaja) solo usan codigos conocidos");
        check(spec.MechanicTable.Select(m => (m.Code, m.Id)).SequenceEqual([("Ln", "liana_n"), ("Ha", "hoja_atada"), ("B2", "brote_n2"), ("Fl", "flor")])
            && spec.MechanicTable.All(m => PackWriter.Pack3Intros.Contains(m.Id)),
            "muestra: el id de cada variante es su intro_mechanic del pack_schema 3 (liana_n, hoja_atada, brote_n2 y flor; D-023, punto 5)");
        bool unknown;
        try
        {
            _ = CampaignSpec.Load(SpecPath).ParseCell(250, "Xx");
            unknown = false;
        }
        catch (CampaignSpecException)
        {
            unknown = true;
        }

        check(unknown, "un codigo que no esta en la tabla de la especificacion se rechaza al leer la celda");
        UnknownMechanicTable(check);
    }

    /// <summary>
    /// Una tabla <c>mechanics</c> con un código que no está en <see cref="MechanicCodes.All"/> se rechaza al cargar la
    /// especificación (CampaignSpec.Build), no como «mecánica sin superposición» en cada slot. Copia la muestra de variantes en
    /// un directorio temporal (fuera del bucle de fixtures de --validate-spec, que esperan exit 1 con una regla): la copia tal
    /// cual se carga, y con una fila más de código «Xx» lanza CampaignSpecException con «codigo de mecanica desconocido».
    /// </summary>
    private static void UnknownMechanicTable(Action<bool, string> check)
    {
        string dir = Path.Combine(Path.GetTempPath(), "af-mechanics-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            JsonObject root = JsonNode.Parse(File.ReadAllText(SpecPath, System.Text.Encoding.UTF8))!.AsObject();
            string copy = Path.Combine(dir, "sample_variants_copy.json");
            File.WriteAllText(copy, root.ToJsonString());
            bool copyLoads;
            try
            {
                copyLoads = CampaignSpec.Load(copy).MechanicTable.Count == root["mechanics"]!.AsArray().Count;
            }
            catch (CampaignSpecException)
            {
                copyLoads = false;
            }

            root["mechanics"]!.AsArray().Add(new JsonObject
            {
                ["code"] = "Xx",
                ["id"] = "xx",
                ["kind"] = "filter",
                ["debut"] = 250,
                ["season"] = "all",
            });
            string bad = Path.Combine(dir, "sample_variants_unknown_code.json");
            File.WriteAllText(bad, root.ToJsonString());
            string? message = null;
            try
            {
                _ = CampaignSpec.Load(bad);
            }
            catch (CampaignSpecException e)
            {
                message = e.Message;
            }

            check(copyLoads && message is not null && message.Contains("codigo de mecanica desconocido 'Xx'", StringComparison.Ordinal),
                "una tabla mechanics con un codigo desconocido (Xx) se rechaza al cargar la especificacion: "
                + (message ?? (copyLoads ? "se cargo sin error" : "la copia sin cambios no se carga")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static void Order(CampaignSpec campaign, Action<bool, string> check)
    {
        check(Superposition.Order.Where(c => !MechanicCodes.Variants.Contains(c)).SequenceEqual(Superposition.OrderBeforeVariants),
            "superposicion: sin las variantes, el orden de siempre (Re Ho Ma Ll Li Br Ba Pi Ti Tu)");
        check(Superposition.Order.Order(StringComparer.Ordinal).SequenceEqual(MechanicCodes.All),
            "superposicion: un paso por cada codigo conocido, y ninguno de mas");

        // Un tablero sin variantes consume los mismos números con el orden nuevo y con el de antes (03 §2: el slot se reproduce
        // desde su semilla): Hoja (43), Brote (163) y Barro (123) de campaign_v1, cada uno con 10 semillas.
        bool same = true;
        foreach (int level in new[] { 43, 123, 163 })
        {
            LevelPlan plan = campaign.Plan(level);
            for (ulong seed = 1; seed <= 10; seed++)
            {
                (GenBoard a, ulong na, string? wa) = ApplyAll(Superposition.Order, Base(), plan, campaign, seed);
                (GenBoard b, ulong nb, string? wb) = ApplyAll(Superposition.OrderBeforeVariants, Base(), plan, campaign, seed);
                same &= wa == wb && na == nb && Same(a, b);
            }
        }

        check(same, "un tablero sin variantes (43, 123 y 163 de campaign_v1) sale igual y gasta los mismos numeros con el orden nuevo");
    }

    private static (GenBoard Board, ulong Next, string? Why) ApplyAll(string[] order, GenBoard board, LevelPlan plan, CampaignSpec spec, ulong seed)
    {
        var ctx = new SuperpositionContext { Plan = plan, Spec = spec, Colors = BaseColors, Units = BaseUnits };
        var rng = new Prng(seed);
        string? why = null;
        foreach (string code in order)
        {
            foreach (MechanicInfo m in plan.Mechanics.Where(m => m.Code == code))
            {
                why ??= Superposition.Apply(board, m, ctx, rng);
            }
        }

        return (board, rng.NextULong(), why);
    }

    /// <summary>Colores del tablero base fijo (abstractos 1-6, todos de 4 dosis).</summary>
    private const int BaseColors = 6;

    private static int[] BaseUnits => [4, 4, 4, 4, 4, 4];

    /// <summary>
    /// Tablero base fijo de 6 colores de 4 dosis (colores abstractos 1-6, de abajo arriba): cinco cuias llenas y mezcladas, una
    /// de 3 dosis, una de 1 y dos vacíos.
    /// </summary>
    private static GenBoard Base()
    {
        int[] caps = [4, 4, 4, 4, 4, 4, 4, 4, 4];
        int[][] cuias = [[1, 2, 3, 4], [2, 3, 4, 5], [3, 4, 5, 6], [4, 5, 6, 1], [5, 6, 1, 2], [6, 1, 2], [3], [], []];
        BoardBuildResult built = BoardBuilder.Build(caps, cuias, null, null);
        return GenBoard.From(built.State!);
    }

    private static bool Same(GenBoard a, GenBoard b) =>
        a.Count == b.Count && a.Caps.SequenceEqual(b.Caps) && a.Attrs.SequenceEqual(b.Attrs) && a.Hidden.SequenceEqual(b.Hidden)
        && Enumerable.Range(0, a.Count).All(i => a.Cuias[i].SequenceEqual(b.Cuias[i]));

    private static void StepsOnFixedBase(CampaignSpec spec, Action<bool, string> check)
    {
        foreach (MechanicInfo m in spec.MechanicTable)
        {
            foreach (bool debut in new[] { true, false })
            {
                // Estreno (el primer tablero) y un nivel del ciclo que la lleva fuera del estreno (p11).
                LevelPlan plan = spec.Plan(debut ? m.Debut : m.Debut + 8);
                int placed = 0;
                int valid = 0;
                bool fields = true;
                bool deterministic = true;
                var ns = new SortedSet<int>();
                var notes = new SortedSet<string>(StringComparer.Ordinal);
                for (ulong seed = 1; seed <= Seeds; seed++)
                {
                    GenBoard b = Base();
                    var ctx = new SuperpositionContext { Plan = plan, Spec = spec, Colors = BaseColors, Units = BaseUnits };
                    string? why = Superposition.Apply(b, m, ctx, new Prng(seed));
                    if (why is not null)
                    {
                        notes.Add(why);
                        continue;
                    }

                    placed++;
                    GenBoard again = Base();
                    _ = Superposition.Apply(again, m, new SuperpositionContext { Plan = plan, Spec = spec, Colors = BaseColors, Units = BaseUnits }, new Prng(seed));
                    deterministic &= Same(b, again);
                    int[] at = [.. Enumerable.Range(0, b.Count).Where(i => !b.Attrs[i].IsNone)];
                    fields &= at.Length == 1 && MechanicCodes.CodeOf(b.Attrs[at[0]]) == m.Code;
                    if (at.Length == 1)
                    {
                        MechanicSpec a = b.Attrs[at[0]];
                        List<int> c = b.Cuias[at[0]];
                        fields &= m.Code switch
                        {
                            "Ln" => c.Count > 0 && (debut ? a.N == 1 : a.N is 1 or 2),
                            "Ha" => c.Count > 0 && a.Key == c[0] && b.Units()[a.Key] == b.Caps[at[0]],
                            "B2" => a.N == 2 && c.Count is 1 or 2 && b.Caps[at[0]] - c.Count >= 2,
                            "Fl" => a.Key != a.Key2 && a.Key is >= 1 and <= BaseColors && a.Key2 is >= 1 and <= BaseColors && !c.Contains(a.Key) && !c.Contains(a.Key2),
                            _ => false,
                        };
                        ns.Add(a.N);
                    }

                    BoardBuildResult built = b.ToData().Build();
                    if (built.IsValid && LevelValidation.Validate(built.State!) is null)
                    {
                        valid++;
                    }
                }

                string where = F($"{m.Code} sobre el tablero fijo ({(debut ? "estreno" : "fuera del estreno")}, nivel {plan.Level})");
                check(placed == Seeds && valid == placed, F($"{where}: colocada con {placed} de {Seeds} semillas, valida segun LevelValidation en {valid}{(notes.Count == 0 ? "" : " (" + string.Join("; ", notes) + ")")}"));
                check(fields, F($"{where}: campos de b.3 en la cuia ({m.Code switch { "Ln" => "n " + string.Join("/", ns), "Ha" => "llave = fruta del fondo y cap = U_c", "B2" => "n = 2, 1-2 dosis", _ => "key != key2, fuera de la cuia" }})"));
                check(deterministic, F($"{where}: la misma semilla da el mismo tablero"));
                if (m.Code == "Ln" && !debut)
                {
                    check(ns.SetEquals([1, 2]), F($"Ln fuera del estreno: n = 1 y n = 2 salen con las {Seeds} semillas ({string.Join("/", ns)})"));
                }
            }
        }

        // Sin cuia donde ponerla, la variante se rechaza con su motivo (no se pone en otra parte).
        var empty = new GenBoard();
        empty.AddEmpty(4, MechanicSpec.None);
        empty.AddEmpty(4, MechanicSpec.None);
        bool reasons = spec.MechanicTable.All(m => Superposition.Apply(empty.Clone(), m,
            new SuperpositionContext { Plan = spec.Plan(m.Debut), Spec = spec, Colors = BaseColors, Units = BaseUnits }, new Prng(1)) is not null);
        check(reasons, "sin cuia con contenido, cada variante devuelve su motivo (el candidato se rechaza)");

        // Brote de dos hojas en una Cuia alta (d.5.1 no prohíbe B2 con capacidades): con 3 dosis en una cuia de 6 hay espacio
        // ≥ 2, pero b.3 dice «empieza con 2 dosis como mucho»; con 2 dosis, sí.
        MechanicInfo b2 = spec.MechanicTable.Single(m => m.Code == MechanicCodes.BroteTwo);
        string? Tall(int doses)
        {
            var tall = new GenBoard();
            tall.AddEmpty(6, MechanicSpec.None);
            tall.Cuias[0].AddRange(Enumerable.Range(1, doses));
            tall.AddEmpty(4, MechanicSpec.None);
            tall.AddEmpty(4, MechanicSpec.None);
            return Superposition.Apply(tall, b2, new SuperpositionContext { Plan = spec.Plan(b2.Debut + 8), Spec = spec, Colors = BaseColors, Units = BaseUnits }, new Prng(1));
        }

        check(Tall(3) is not null && Tall(4) is not null && Tall(2) is null && Tall(1) is null,
            "B2 en una Cuia alta de 6: con 3 o 4 dosis no se pone (b.3: 2 dosis como mucho) y con 1 o 2 si");
    }

    private static void KeysThroughColorsAndOrder(Action<bool, string> check)
    {
        var b = new GenBoard();
        b.AddEmpty(4, new MechanicSpec(MechanicType.Flor, key: 1, key2: 2));
        b.Cuias[0].AddRange([3, 4]);
        b.AddEmpty(4, new MechanicSpec(MechanicType.HojaAtada, key: 3));
        b.Cuias[1].Add(3);
        int[] map = [0, 9, 5, 7, 2];
        b.MapColors(map);
        MechanicSpec flor = b.Attrs[0];
        check(flor.Key == 9 && flor.Key2 == 5 && b.Attrs[1].Key == 7, F($"MapColors cambia key y key2 de la Flor ({flor}) y la llave de la Hoja atada ({b.Attrs[1]})"));
        GenBoard p = b.Permuted([1, 0]);
        check(p.Attrs[1].Equals(flor), F($"Permuted conserva la key2 de la Flor ({p.Attrs[1]})"));
    }

    private static void Debuts(CampaignSpec spec, Action<bool, string> check, Action<string> info)
    {
        foreach (MechanicInfo m in spec.MechanicTable)
        {
            LevelPlan plan = spec.Plan(m.Debut);
            var gen = new SlotGenerator(spec);
            SlotResult slot = gen.Generate(plan);
            Candidate? c = slot.Candidates.Where(x => x.PoolMember).OrderBy(x => x.Attempt).FirstOrDefault();
            check(c is not null, F($"estreno {m.Code} ({m.Debut}): {slot.Candidates.Count(x => x.PoolMember)} candidato(s) en ventana de {slot.Attempts} intentos"));
            if (c is null)
            {
                continue;
            }

            BoardData b = c.Board;
            int[] at = [.. Enumerable.Range(0, b.Attrs.Length).Where(i => !b.Attrs[i].IsNone)];
            check(CampaignReport.DebutOk(c) && at.Length == 1 && MechanicCodes.CodeOf(b.Attrs[at[0]]) == m.Code && b.Hidden.Length == 0
                && !b.Cuias.Any(x => x.Contains(Cuia.MudColor)),
                F($"estreno {m.Code} ({m.Debut}): {CampaignReport.Instances(b)} ejemplar, {CampaignReport.Empties(b)} vacios y bot_fail {c.Bot:0.000} < 0,20 (02 sec. 9.2 regla 2)"));
            check(LevelValidation.Validate(c.State) is null, F($"estreno {m.Code} ({m.Debut}): valido segun LevelValidation"));

            JsonObject level = JsonNode.Parse(PackWriter.Min(PackWriter.Level(spec, new PackLevel(plan, c, []))))!.AsObject();
            JsonObject attr = level["attrs"]!.AsArray().OfType<JsonObject>().Single();
            string[] keys = [.. attr.Select(kv => kv.Key)];
            string shape = m.Code switch
            {
                "Ln" => "type,n",
                "Ha" => "type,key",
                "B2" => "type,n",
                _ => "type,key,key2",
            };
            string type = attr["type"]!.GetValue<string>();
            bool attrOk = string.Join(",", keys) == shape && type == m.Code switch { "Ln" => "liana", "Ha" => "hoja_atada", "B2" => "brote", _ => "flor" }
                && (m.Code != "B2" || attr["n"]!.GetValue<int>() == 2) && (m.Code != "Ln" || attr["n"]!.GetValue<int>() == 1)
                && (m.Code != "Fl" || attr["key"]!.GetValue<int>() != attr["key2"]!.GetValue<int>());
            check(attrOk, F($"JSON del {m.Debut}: attrs {attr.ToJsonString()} (firma de pack_schema 3, D-023 punto 5)"));
            check(level["intro_mechanic"]?.GetValue<string>() == m.Id && !level["skippable"]!.GetValue<bool>(),
                F($"JSON del {m.Debut}: intro_mechanic {m.Id} y skippable = false"));
            check(PackWriter.PackSchemaFor([new PackLevel(plan, c, [])]) == PackWriter.TinajaPackSchema
                && JsonNode.Parse(PackWriter.Write(spec, plan.World, [new PackLevel(plan, c, [])]))!["pack_schema"]!.GetValue<int>() == 3,
                F($"JSON del {m.Debut}: pack_schema 3"));

            // Lectura de vuelta con el lector del generador (TinajaSelfTest.FromJson): el mismo tablero y el mismo hash. La de
            // level-qa (su PackReader, sobre el pack escrito en disco) la comprueba RunPackReadBack, desde su --self-test key2.
            BoardBuildResult back = TinajaSelfTest.FromJson(level);
            check(back.IsValid && back.State!.Equals(c.State) && back.State.CanonicalHash() == c.State.CanonicalHash(),
                F($"JSON del {m.Debut} leido de vuelta: mismo tablero y mismo hash ({c.State.CanonicalHash():x16})"));
            if (m.Code == "Fl")
            {
                JsonObject noKey2 = level.DeepClone().AsObject();
                noKey2["attrs"]!.AsArray().OfType<JsonObject>().Single().Remove("key2");
                check(!TinajaSelfTest.FromJson(noKey2).IsValid, "la Flor leida sin key2 no se construye (la lectura tiene que llevar key2)");
            }

            // Alivio (03 §13): minus_color no quita ninguna llave (tampoco la key2 de la Flor) y las variantes son válidas.
            (List<ReliefBoard> published, List<ReliefBoard> rejected) = Relief.Compute(gen.Metrics, b, c.Par, c.Bot, plan.SeedText);
            MechanicSpec a = b.Attrs[at[0]];
            int[] locked = [.. new[] { a.Key, a.Key2 }.Where(k => k != 0).Concat(b.Cuias[at[0]])];
            bool removedOk = published.Concat(rejected).Where(r => r.Removed is not null).All(r => !locked.Contains(r.Removed!.Value));
            bool validRelief = published.All(r => r.Board.Build() is { IsValid: true } rb && LevelValidation.Validate(rb.State!) is null);
            check(removedOk && validRelief && published.Count > 0,
                F($"alivio del {m.Debut}: {published.Count} variante(s) valida(s) ({string.Join(" ", published.Select(r => r.Type))}) sin quitar las llaves ni el contenido de la variante"));
            info(F($"estreno {m.Code} ({m.Debut}): {attr.ToJsonString()}, K {c.K}, {b.Caps.Length} cuias, par {c.Par}, bot_fail {c.Bot:0.000}, candidato {c.Attempt + 1}"));
        }
    }

    /// <summary>
    /// Lectura de vuelta de los estrenos de las variantes con el lector de level-qa (criterio de P5: «level-qa lee las Flores con
    /// key2 y el hash coincide»). level-qa enlaza este archivo y lo llama desde su <c>--self-test key2</c> con su PackReader:
    /// <paramref name="read"/> recibe la ruta de un pack y devuelve el tablero de su primer nivel y los de sus variantes de
    /// relief, en orden. El estreno de cada variante de la muestra (Ln, Ha, B2 y Fl) se escribe con PackWriter, con su alivio, a
    /// un pack temporal, y lo leído tiene que dar el mismo tablero, las mismas mecánicas (también la key2) y el mismo hash que el
    /// del generador, en el nivel y en cada variante de relief; la Flor del pack sin su key2 no se construye.
    /// </summary>
    /// <returns>0 si todo pasa, 1 si algo falla y 2 si no encuentra la especificación.</returns>
    public static int RunPackReadBack(string reader, Func<string, (BoardData Level, BoardData[] Relief)> read)
    {
        CampaignSpec spec;
        try
        {
            spec = CampaignSpec.Load(SpecPath);
        }
        catch (CampaignSpecException e)
        {
            Console.Error.WriteLine(AsciiText.Of("--self-test key2: " + e.Message + " (ejecuta desde la raiz del repositorio)"));
            return 2;
        }

        int failures = 0;
        void Check(bool ok, string what)
        {
            Console.WriteLine(AsciiText.Of((ok ? "PASS " : "FAIL ") + what));
            failures += ok ? 0 : 1;
        }

        string dir = Path.Combine(Path.GetTempPath(), "af-key2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            foreach (MechanicInfo m in spec.MechanicTable)
            {
                LevelPlan plan = spec.Plan(m.Debut);
                var gen = new SlotGenerator(spec);
                Candidate? c = gen.Generate(plan).Candidates.Where(x => x.PoolMember).OrderBy(x => x.Attempt).FirstOrDefault();
                if (c is null)
                {
                    Check(false, F($"estreno {m.Code} ({m.Debut}): sin candidato en ventana"));
                    continue;
                }

                List<ReliefBoard> relief = Relief.Compute(gen.Metrics, c.Board, c.Par, c.Bot, plan.SeedText).Published;
                string json = PackWriter.Write(spec, plan.World, [new PackLevel(plan, c, relief)]);
                string path = Path.Combine(dir, m.Code + ".json");
                File.WriteAllText(path, json);
                (BoardData? level, BoardData[] reliefRead) = Read(read, path, out string? error);
                BoardBuildResult back = level is null ? default : level.Build();
                bool same = level is not null && back.IsValid && back.State!.Equals(c.State) && level.Attrs.SequenceEqual(c.Board.Attrs)
                    && back.State.CanonicalHash() == c.State.CanonicalHash();
                Check(same, F($"{reader}: estreno {m.Code} ({m.Debut}) leido del pack con el mismo tablero, attrs {string.Join(" ", c.Board.Attrs.Where(a => !a.IsNone))} y hash {c.State.CanonicalHash():x16}{(error is null ? "" : " (" + error + ")")}"));

                bool reliefSame = level is not null && reliefRead.Length == relief.Count && relief.Count > 0;
                for (int i = 0; reliefSame && i < relief.Count; i++)
                {
                    BoardBuildResult want = relief[i].Board.Build();
                    BoardBuildResult got = reliefRead[i].Build();
                    reliefSame = want.IsValid && got.IsValid && got.State!.Equals(want.State) && reliefRead[i].Attrs.SequenceEqual(relief[i].Board.Attrs)
                        && got.State.CanonicalHash() == want.State!.CanonicalHash();
                }

                Check(reliefSame, F($"{reader}: las {relief.Count} variante(s) de relief del {m.Debut} ({string.Join(" ", relief.Select(r => r.Type))}) leidas con el mismo tablero, attrs y hash"));

                if (m.Code == MechanicCodes.Flor)
                {
                    JsonNode pack = JsonNode.Parse(json)!;
                    pack["levels"]![0]!["attrs"]!.AsArray().OfType<JsonObject>().Single().Remove("key2");
                    string noKey2 = Path.Combine(dir, m.Code + "-sin-key2.json");
                    File.WriteAllText(noKey2, pack.ToJsonString());
                    (BoardData? without, _) = Read(read, noKey2, out string? why);
                    Check(without is null || !without.Build().IsValid,
                        F($"{reader}: la Flor del {m.Debut} sin key2 no se construye ({why ?? (without is null ? "" : without.Build().Error)})"));
                }
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }

        Console.WriteLine(failures == 0 ? "self-test key2: todo PASS" : $"self-test key2: {failures} FAIL");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>Lee un pack con el lector dado; si el lector lanza, devuelve el tablero nulo y el motivo.</summary>
    private static (BoardData? Level, BoardData[] Relief) Read(Func<string, (BoardData Level, BoardData[] Relief)> read, string path, out string? error)
    {
        try
        {
            error = null;
            return read(path);
        }
        catch (Exception e)
        {
            error = e.GetType().Name + ": " + e.Message;
            return (null, []);
        }
    }

    private static void SpecFixtures(CampaignSpec spec, Action<bool, string> check)
    {
        List<SpecIssue> own = SpecValidator.Validate(spec);
        check(own.All(i => i.Warning), F($"--validate-spec de la muestra: {own.Count(i => !i.Warning)} fallo(s) {string.Join("; ", own.Where(i => !i.Warning).Select(i => i.Rule + " " + i.Level.ToString(CultureInfo.InvariantCulture)))}"));
        foreach ((string path, string rule) in Fixtures)
        {
            bool present;
            try
            {
                CampaignSpec f = CampaignSpec.Load(path);
                present = f.ExpectRule == rule && SpecValidator.Validate(f).Any(i => !i.Warning && i.Rule == rule);
            }
            catch (CampaignSpecException)
            {
                present = false;
            }

            check(present, F($"fixture {Path.GetFileName(path)}: falla con [{rule}]"));
        }
    }

    private static string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
}
