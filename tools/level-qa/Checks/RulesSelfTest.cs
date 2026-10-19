// --self-test reglas (P6 de la tanda 4, revisión): las reglas de level-qa que los fixtures negativos no pueden probar en positivo,
// sobre tableros escritos aquí y sobre las especificaciones de muestra:
//   - el tope de mecánicas por nivel (QA-MECH-COUNT; CampaignSpec.MaxMechanicsAt): 2 antes del 300 y 3 después, 3 solo en retos,
//     desde el 301, y en hitos, desde el 320 (02 §9.2 regla 4; 03 §12, fila p20; d.5.1 de la propuesta); sin la clave
//     mechanics_per_level, 2 siempre (campaign_v1);
//   - QA-HITO-14 (CycleChecks.Hito14Problems): el hito de K = 13 con el vacío repartido en cuias a medias (Brote de dos hojas y
//     Matapi con dosis, como el 580 antes de D-027; con Barro, como el 600) pasa, igual que con una cuia vacía literal; un
//     reto con K = 12 + 1 vacío + la cueva de la Piraña (14 cuias) no es de esta comprobación; K = 13 fuera de su hito, con
//     capacidades, con la Piraña o con otra holgura, falla; el Brote de dos hojas del 580 y del 600 cumple QA-VAR-B2
//     (VariantChecks.B2Problems) y con 3 dosis no;
//   - las casas de la Flor (QA-VAR-FLOR; VariantChecks.FlorProblems y CanSealOutside, D-024): una Hoja atada, un Matapi con dosis
//     o la Lluvia solo son casa de su fruta, y la Liana, otra Flor o la Tinaja, de ninguna;
//   - la Tinaja (decisión del dueño del 2026-10-18, D-027): el tope de cuias con la pareja es 12 en el nivel (QA-TINAJA-CUIAS)
//     y 13 en sus variantes de alivio (QA-RELIEF-VALID; TinajaChecks.Structure con Superposition.TinajaReliefMaxCuias), y QA-PAR
//     exime del par del rango solo los tableros de estreno de la Tinaja con K <= 6 (BaseChecks.TinajaDebutParExempt): el 323-325
//     de campaign_v2.json en la a, no en la b (sin tinaja), ni la práctica 327, ni con K = 7, ni el estreno sin la tinaja.
// Imprime ASCII (PASS / FAIL) y sale con 0 si todo pasa, 1 si algo falla y 2 si no encuentra las especificaciones.
using AF.Core.Board;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Generation;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class RulesSelfTest
{
    public const string VariantsSpec = "tools/level-generator/specs/sample_variants.json";
    public const string CampaignSpecPath = "tools/level-generator/specs/campaign_v1.json";
    public const string CampaignV2Path = "tools/level-generator/specs/campaign_v2.json";

    public static int Run()
    {
        CampaignSpec variants;
        CampaignSpec campaign;
        CampaignSpec v2a;
        CampaignSpec v2b;
        try
        {
            variants = CampaignSpec.Load(VariantsSpec);
            campaign = CampaignSpec.Load(CampaignSpecPath);
            v2a = CampaignSpec.Load(CampaignV2Path, CampaignSpec.VariantA);
            v2b = CampaignSpec.Load(CampaignV2Path, CampaignSpec.VariantB);
        }
        catch (CampaignSpecException e)
        {
            Console.Error.WriteLine(AsciiText.Of("level-qa --self-test reglas: " + e.Message + " (ejecuta desde la raiz del repositorio)"));
            return 2;
        }

        int failures = 0;
        void Check(bool ok, string what)
        {
            Console.WriteLine(AsciiText.Of((ok ? "PASS " : "FAIL ") + what));
            failures += ok ? 0 : 1;
        }

        MechanicsCap(variants, campaign, Check);
        Hito14(Check);
        Flor(Check);
        Tinaja(v2a, v2b, Check);

        Console.WriteLine(failures == 0 ? "self-test reglas: todo PASS" : $"self-test reglas: {failures} FAIL");
        return failures == 0 ? 0 : 1;
    }

    // ---------------------------------------------------------------------------------------------------------------
    // QA-MECH-COUNT
    // ---------------------------------------------------------------------------------------------------------------

    private static void MechanicsCap(CampaignSpec variants, CampaignSpec campaign, Action<bool, string> check)
    {
        (int Level, SlotRole? Role, int Max)[] cases =
        [
            (286, SlotRole.Challenge, 2),
            (299, SlotRole.Challenge, 2),
            (300, SlotRole.Challenge, 2),
            (300, SlotRole.Milestone, 2),
            (301, SlotRole.Challenge, 3),
            (306, SlotRole.Challenge, 3),
            (646, SlotRole.Challenge, 3),
            (307, SlotRole.Comfort, 2),
            (301, SlotRole.Recovery, 2),
            (319, SlotRole.Remanso, 2),
            (319, SlotRole.Milestone, 2),
            (320, SlotRole.Milestone, 3),
            (700, SlotRole.Milestone, 3),
            (650, null, 2),
        ];
        foreach ((int level, SlotRole? role, int max) in cases)
        {
            int got = variants.MaxMechanicsAt(level, role);
            check(got == max, $"QA-MECH-COUNT con sample_variants.json: {level} {role?.ToString() ?? "sin plan"} -> {got} (espera {max})");
        }

        check(campaign.RaisedMechanicsFrom is null && campaign.MaxMechanicsAt(646, SlotRole.Challenge) == 2
            && campaign.MaxMechanicsAt(700, SlotRole.Milestone) == 2,
            "QA-MECH-COUNT con campaign_v1.json (sin mechanics_per_level): 2 en todos los niveles, tambien en retos e hitos");

        // Sin raised_milestone_from_level, los hitos suben con los retos; un raised_milestone_from_level sin raised_from_level y un
        // raised_max por debajo de max no se cargan.
        string dir = Path.Combine(Path.GetTempPath(), "af-level-qa-reglas-" + Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(dir);
        try
        {
            string baseSpec = Path.GetFullPath(VariantsSpec).Replace('\\', '/');
            string Spec(string name, string mechanics)
            {
                string path = Path.Combine(dir, name);
                File.WriteAllText(path, "{\"extends\": \"" + baseSpec + "\", \"mechanics_per_level\": " + mechanics + "}");
                return path;
            }

            CampaignSpec single = CampaignSpec.Load(Spec("solo_retos.json", "{\"max\": 2, \"raised_from_level\": 301, \"raised_max\": 3}"));
            check(single.MaxMechanicsAt(301, SlotRole.Milestone) == 3 && single.MaxMechanicsAt(300, SlotRole.Milestone) == 2,
                "QA-MECH-COUNT sin raised_milestone_from_level: los hitos suben desde raised_from_level, como los retos");
            check(Throws(() => CampaignSpec.Load(Spec("solo_hitos.json", "{\"max\": 2, \"raised_milestone_from_level\": 320, \"raised_max\": 3}"))),
                "mechanics_per_level con raised_milestone_from_level y sin raised_from_level: no se carga");
            check(Throws(() => CampaignSpec.Load(Spec("max_al_reves.json", "{\"max\": 3, \"raised_from_level\": 301, \"raised_max\": 2}"))),
                "mechanics_per_level con raised_max < max: no se carga");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static bool Throws(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch (CampaignSpecException)
        {
            return true;
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // QA-HITO-14
    // ---------------------------------------------------------------------------------------------------------------

    private static void Hito14(Action<bool, string> check)
    {
        const int K = CycleChecks.HitoK;

        // 13 frutas de 4 en 13 cuias llenas y 1 vacía (la forma literal de 03 §1).
        BoardData literal = Board(Full(K).Append([]).ToArray());
        Expect(check, literal, K, SlotRole.Milestone, K, pass: true, "hito K = 13 con 13 cuias llenas y 1 vacia");

        // 580 (Ma+B2): el Brote de dos hojas con 2 dosis y un Matapi con 2 dosis de su fruta; la holgura (4) va en cuias a medias.
        int[][] b2 = Full(K - 1).ToArray();
        int[][] cuias580 = [.. b2, [K, K], [K, K]];
        var attrs580 = new MechanicSpec[cuias580.Length];
        attrs580[K - 1] = new MechanicSpec(MechanicType.Brote, n: 2);
        attrs580[K] = new MechanicSpec(MechanicType.Matapi);
        BoardData hito580 = Board(cuias580, attrs580);
        Expect(check, hito580, K, SlotRole.Milestone, K, pass: true, "hito K = 13 con el Brote de dos hojas (2 dosis) y un Matapi con 2 dosis, sin cuia vacia (580)");
        string[] b2Reasons580 = [.. VariantChecks.B2Problems(hito580, K - 1)];
        check(VariantChecks.Units(hito580)[K] == Cuia.DefaultCapacity && b2Reasons580.Length == 0,
            "el Brote de dos hojas de ese hito cumple QA-VAR-B2 (1-2 dosis y espacio >= 2)"
            + (b2Reasons580.Length > 0 ? " [" + string.Join(" | ", b2Reasons580) + "]" : " [sin motivos]"));

        // El mismo hito con 3 dosis en el Brote de dos hojas (espacio 1): QA-VAR-B2 da su motivo, así que el PASS anterior no es vacío.
        int[][] cuias580Full = [.. b2, [K, K, K], [K]];
        BoardData hito580Full = Board(cuias580Full, attrs580);
        string[] b2ReasonsFull = [.. VariantChecks.B2Problems(hito580Full, K - 1)];
        check(b2ReasonsFull.Length == 1 && b2ReasonsFull[0].Contains("espacio 1", StringComparison.Ordinal),
            "el Brote de dos hojas con 3 dosis (espacio 1) no cumple QA-VAR-B2 [" + string.Join(" | ", b2ReasonsFull) + "]");

        // 600 (Ha+B2+Ba): una dosis de Barro ocupa sitio, pero no es una fruta; la holgura de la aritmética sigue en 4.
        int[][] cuias600 = [.. b2, [K, K, Cuia.MudColor], [K, K]];
        var attrs600 = new MechanicSpec[cuias600.Length];
        attrs600[K] = new MechanicSpec(MechanicType.Brote, n: 2);
        BoardData hito600 = Board(cuias600, attrs600);
        Expect(check, hito600, K, SlotRole.Milestone, K, pass: true, "hito K = 13 con Barro y el Brote de dos hojas: el Barro no cuenta en la holgura (600)");
        check(!VariantChecks.B2Problems(hito600, K).Any(), "el Brote de dos hojas del 600 cumple QA-VAR-B2 (2 dosis y espacio 2)");

        // K = 12 + 1 vacío + la cueva de la Piraña: 14 cuias en un reto (o en un hito de 401-560), válido (03 §11; el 920 de d.5.7).
        int[][] cave = [.. Full(12), [], []];
        int[] caveCaps = [.. Enumerable.Repeat(Cuia.DefaultCapacity, 13), 2];
        var caveAttrs = new MechanicSpec[cave.Length];
        caveAttrs[13] = new MechanicSpec(MechanicType.Pirana);
        BoardData pirana12 = new(caveCaps, cave, caveAttrs, Hidden(cave.Length));
        Expect(check, pirana12, 12, SlotRole.Challenge, 12, pass: true, "reto con K = 12 + 1 vacio + la cueva de la Pirana (14 cuias): no es de QA-HITO-14");
        Expect(check, pirana12, 12, SlotRole.Milestone, 12, pass: true, "hito de 401-560 con K = 12 + 1 vacio + la cueva (14 cuias): no es de QA-HITO-14");

        // Lo que falla.
        Expect(check, literal, K, SlotRole.Challenge, 9, pass: false, "K = 13 en un reto: falla");
        Expect(check, literal, K, SlotRole.Milestone, 9, pass: false, "K = 13 en un hito con K = 9 en el k_plan: falla");
        int[] alta = [5, .. Enumerable.Repeat(Cuia.DefaultCapacity, 13)];
        int[][] altaCuias = [[1, 1, 1, 1, 1], .. Full(K).Skip(1), []];
        Expect(check, new BoardData(alta, altaCuias, new MechanicSpec[14], Hidden(14)), K, SlotRole.Milestone, K, pass: false,
            "hito K = 13 con una Cuia alta de 5: falla (sin capacidades)");
        var piranaAttrs = new MechanicSpec[14];
        piranaAttrs[0] = new MechanicSpec(MechanicType.Pirana);
        Expect(check, Board(Full(K).Append([]).ToArray(), piranaAttrs), K, SlotRole.Milestone, K, pass: false, "hito K = 13 con la Pirana: falla");
        int[][] over = [.. Full(K - 1), [K, K, K, K], [K]];
        Expect(check, Board(over), K, SlotRole.Milestone, K, pass: false, "hito K = 13 con 5 dosis de una fruta (holgura 3): falla");
    }

    private static void Expect(Action<bool, string> check, BoardData b, int k, SlotRole role, int planK, bool pass, string what)
    {
        string[] problems = [.. CycleChecks.Hito14Problems(b, k, role, planK, role.ToString())];
        check(pass ? problems.Length == 0 : problems.Length > 0,
            "QA-HITO-14: " + what + (problems.Length > 0 ? " [" + string.Join(" | ", problems) + "]" : " [sin motivos]"));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // QA-VAR-FLOR
    // ---------------------------------------------------------------------------------------------------------------

    private static void Flor(Action<bool, string> check)
    {
        // Frutas 1 y 2 en la Flor (cuia 0); 3 y 4, las demás. Cuias de 4 (bloqueo C1-9).
        MechanicSpec flor = new(MechanicType.Flor, key: 1, key2: 2);

        // Dos cuias sin mecánica fuera de la Flor: pasa.
        Expect(check, [[3, 4], [1, 2, 1, 2], [2, 1, 2, 1], [3, 4, 3], [4, 3, 4]], [flor], pass: true, "Flor con dos casas libres fuera");

        // Una Hoja atada de la fruta 3, un Matapi con dosis de la 4 y una Hoja atada de la 4: solo una casa libre para las dos
        // frutas de la Flor, aunque haya cuatro cuias de 4 fuera de ella (la cuenta solo por capacidad de AF.Core la deja pasar).
        MechanicSpec[] blocked = [flor, new(MechanicType.HojaAtada, key: 3), new(MechanicType.Matapi), new(MechanicType.HojaAtada, key: 4)];
        Expect(check, [[3, 4], [3, 1, 2, 1], [4, 4], [4, 2, 1, 2], [3, 2, 1, 3]], blocked, pass: false,
            "Flor con Hojas atadas y un Matapi con dosis de otras frutas: una sola casa libre, falla");

        // Una Hoja atada de la propia fruta 1 es su casa: la otra cuia libre queda para la 2.
        MechanicSpec[] ownLeaf = [flor, new(MechanicType.HojaAtada, key: 1), new(MechanicType.Matapi), MechanicSpec.None, new(MechanicType.HojaAtada, key: 4)];
        Expect(check, [[3, 4], [1, 3, 2, 1], [3, 3], [2, 1, 2, 1], [4, 2, 4, 4]], ownLeaf, pass: true,
            "Flor con una Hoja atada de su fruta 1 y una cuia libre para la 2: pasa");

        // Un Matapi con dosis de la fruta 2 es su casa.
        MechanicSpec[] ownTrap = [flor, MechanicSpec.None, new(MechanicType.Matapi), new(MechanicType.HojaAtada, key: 3), new(MechanicType.Liana, key: 4)];
        Expect(check, [[3, 4], [1, 4, 1, 4], [2, 2], [3, 1, 2, 1], [3, 2, 4, 3]], ownTrap, pass: true,
            "Flor con un Matapi con dosis de su fruta 2 y una cuia libre para la 1: pasa");

        // Una Liana y otra Flor no son casa de nadie.
        MechanicSpec[] closed = [flor, new(MechanicType.Liana, key: 3), new(MechanicType.Flor, key: 3, key2: 4), MechanicSpec.None, new(MechanicType.Liana, n: 1)];
        Expect(check, [[3, 4], [4, 1, 2, 1], [3, 4], [2, 1, 2, 1], [3, 2, 4, 3]], closed, pass: false,
            "Flor con dos Lianas (con fruta y de cuenta) y otra Flor: no son casa, falla");

        // La Lluvia solo es casa de su agua (10): con la Flor sobre el agua 10 y la fruta 2, es la casa del agua.
        MechanicSpec florWater = new(MechanicType.Flor, key: 10, key2: 2);
        MechanicSpec[] rain = [florWater, new(MechanicType.Lluvia, key: 10, n: 0, k: 2), new(MechanicType.HojaAtada, key: 3), MechanicSpec.None, new(MechanicType.Liana, key: 4)];
        Expect(check, [[3, 4], [10, 2, 10], [3, 4, 4, 4], [2, 10, 2, 10], [3, 2, 3]], rain, pass: true,
            "Flor sobre el agua 10 con la Lluvia (casa de su agua) y una cuia libre para la 2: pasa");
        // Con gotas pendientes, U_agua = dosis + gotas (02 R3): 2 dosis de agua y 2 gotas son 4, y la Lluvia sigue siendo su casa.
        MechanicSpec[] rainPending = [florWater, new(MechanicType.Lluvia, key: 10, n: 2, k: 2), new(MechanicType.HojaAtada, key: 3), MechanicSpec.None, new(MechanicType.Liana, key: 4)];
        Expect(check, [[3, 4], [10, 2], [3, 4, 4, 4], [2, 10, 2], [3, 2, 3]], rainPending, pass: true,
            "Flor sobre el agua 10 con la Lluvia con 2 gotas pendientes (U_agua = 2 dosis + 2 gotas): pasa");
        MechanicSpec florOther = new(MechanicType.Flor, key: 1, key2: 2);
        MechanicSpec[] rainOther = [florOther, new(MechanicType.Lluvia, key: 10, n: 0, k: 2), new(MechanicType.HojaAtada, key: 3), MechanicSpec.None, new(MechanicType.Liana, key: 3)];
        Expect(check, [[10, 10], [10, 1, 10], [3, 1, 2, 1], [2, 1, 2, 2], [3, 3, 3]], rainOther, pass: false,
            "Flor sobre las frutas 1 y 2 con la Lluvia: la Lluvia no es su casa, falla");

        // U_c distintas: la fruta 1 con U_c = 5 cuya única cuia de 5 es la Flor.
        BoardData alta = new([5, 4, 4, 4], [[3], [1, 1, 1, 2], [1, 1, 2, 2], [2, 3, 3, 3]], [flor, MechanicSpec.None, MechanicSpec.None, MechanicSpec.None], Hidden(4));
        ExpectBoard(check, alta, pass: false, "Flor con la fruta 1 de U_c = 5 y la Flor como su unica cuia de 5: falla");

        // La llave ausente y las dos llaves iguales tienen su motivo propio.
        BoardData missing = Board([[3, 4], [1, 3, 1, 4], [3, 1, 4, 1], [3, 4, 3, 4], []], [new MechanicSpec(MechanicType.Flor, key: 1, key2: 7)]);
        string[] reasons = [.. VariantChecks.FlorProblems(missing, 0, VariantChecks.Units(missing))];
        check(reasons.Length == 1 && reasons[0].Contains("no esta en el tablero", StringComparison.Ordinal),
            "QA-VAR-FLOR: la llave 7 ausente del tablero da su motivo [" + string.Join(" | ", reasons) + "]");
    }

    private static void Expect(Action<bool, string> check, int[][] cuias, MechanicSpec[] attrs, bool pass, string what) =>
        ExpectBoard(check, Board(cuias, attrs), pass, what);

    private static void ExpectBoard(Action<bool, string> check, BoardData b, bool pass, string what)
    {
        int[] units = VariantChecks.Units(b);
        string[] problems = [.. VariantChecks.FlorProblems(b, 0, units)];
        check(pass ? problems.Length == 0 : problems.Length == 1 && problems[0].Contains("no tienen casa", StringComparison.Ordinal),
            "QA-VAR-FLOR: " + what + (problems.Length > 0 ? " [" + string.Join(" | ", problems) + "]" : " [sin motivos]"));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Tinaja: topes de cuias y par de su estreno (D-027)
    // ---------------------------------------------------------------------------------------------------------------

    private static void Tinaja(CampaignSpec v2a, CampaignSpec v2b, Action<bool, string> check)
    {
        check(Superposition.TinajaReliefMaxCuias == Superposition.TinajaMaxCuias + 1,
            $"tope de cuias con la tinaja: {Superposition.TinajaMaxCuias} en el nivel y {Superposition.TinajaReliefMaxCuias} en el alivio (D-027)");

        // Tableros de n cuias con la tinaja y su cuia en la misma fila: el tope solo mira cuántas son.
        foreach ((int n, int max, bool pass, string what) in new[]
        {
            (12, Superposition.TinajaMaxCuias, true, "nivel de 12 cuias con tinaja"),
            (13, Superposition.TinajaMaxCuias, false, "nivel de 13 cuias con tinaja"),
            (13, Superposition.TinajaReliefMaxCuias, true, "alivio de 13 cuias con tinaja (plus_empty de un nivel de 12)"),
            (14, Superposition.TinajaReliefMaxCuias, false, "alivio de 14 cuias con tinaja"),
        })
        {
            BoardData b = TinajaBoard(n);
            string[] cuias = [.. TinajaChecks.Structure(b, max).Where(x => x.Id == "QA-TINAJA-CUIAS").Select(x => x.Message)];
            check(pass ? cuias.Length == 0 : cuias.Length == 1,
                $"QA-TINAJA-CUIAS (tope {max}): {what} {(pass ? "pasa" : "falla")}" + (cuias.Length > 0 ? " [" + string.Join(" | ", cuias) + "]" : " [sin motivos]"));
        }

        // QA-PAR: el par del rango no se exige en los tableros de estreno de la Tinaja con K <= 6.
        BoardData withTinaja = TinajaBoard(8);
        BoardData without = Board([.. Full(6), [], []]);
        foreach ((CampaignSpec spec, int level, BoardData b, int k, bool exempt, string what) in new[]
        {
            (v2a, 323, withTinaja, 6, true, "estreno 323 (★Ti) con K = 6"),
            (v2a, 324, withTinaja, 5, true, "estreno 324 (★Ti) con K = 5"),
            (v2a, 325, withTinaja, 6, true, "estreno 325 (★Ti) con K = 6"),
            (v2a, 325, withTinaja, 7, false, "estreno 325 (★Ti) con K = 7"),
            (v2a, 327, withTinaja, 6, false, "practica 327 (Ti), no es estreno"),
            (v2a, 343, without, 6, false, "estreno 343 (★B2), sin la tinaja"),
            (v2b, 324, without, 6, false, "324 de la variante b (C), sin la tinaja"),
        })
        {
            LevelPlan plan = spec.Plan(level);
            bool got = BaseChecks.TinajaDebutParExempt(plan, b, k);
            check(got == exempt, $"QA-PAR: {what} {(exempt ? "exento" : "no exento")} del par del rango (D-027) [DebutBoard={plan.DebutBoard}, tinaja={b.HasTinaja}]");
        }
    }

    /// <summary>
    /// Tablero de <paramref name="n"/> cuias: la tinaja (cap 5, 3 dosis con T4 y la máscara) y su cuia vacía (cap 2) en las
    /// ranuras 0 y 1 (la misma fila con cualquier reparto), y el resto, cuias de 4 vacías o llenas.
    /// </summary>
    private static BoardData TinajaBoard(int n)
    {
        int[] caps = [5, 2, .. Enumerable.Repeat(Cuia.DefaultCapacity, n - 2)];
        int[][] cuias = [[1, 2, 1], [], .. Enumerable.Range(0, n - 2).Select(i => i < 3 ? Enumerable.Repeat(i + 3, 4).ToArray() : Array.Empty<int>())];
        var attrs = new MechanicSpec[n];
        attrs[0] = new MechanicSpec(MechanicType.Tinaja);
        attrs[1] = new MechanicSpec(MechanicType.Cucharon);
        return new BoardData(caps, cuias, attrs, [[0, 0], [0, 1]]);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Tableros
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>Cuias llenas de las frutas 1..<paramref name="k"/>, una cuia de 4 dosis por fruta.</summary>
    private static IEnumerable<int[]> Full(int k) => Enumerable.Range(1, k).Select(c => Enumerable.Repeat(c, Cuia.DefaultCapacity).ToArray());

    /// <summary>Tablero de cuias de 4 con <paramref name="attrs"/> en las primeras cuias (las demás, sin mecánica).</summary>
    private static BoardData Board(int[][] cuias, MechanicSpec[]? attrs = null)
    {
        var all = new MechanicSpec[cuias.Length];
        attrs?.CopyTo(all, 0);
        return new BoardData([.. Enumerable.Repeat(Cuia.DefaultCapacity, cuias.Length)], cuias, all, Hidden(cuias.Length));
    }

    private static int[][] Hidden(int n) => [.. Enumerable.Range(0, n).Select(_ => Array.Empty<int>())];
}
