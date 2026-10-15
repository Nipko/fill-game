// --self-test registry (P4 de la tanda 4): la especificación de 201-700 (specs/campaign_v2.json) con sus dos variantes, el
// registro de combinaciones (d.5.6 de la propuesta de D-009) y la regla de 60 niveles (D-009 punto 8), sin proyecto de tests
// (las pruebas de tools/ van como fixtures y modos --self-test). Comprueba:
//   - las dos variantes: la «b» cambia exactamente las 19 celdas de "fallback" (d.5.4) y no tiene la Tinaja; la «a» se publica;
//   - --validate-spec en verde en las dos y en campaign_v1 (21 reglas en la v1 y 24 en la v2);
//   - las cifras de D2 (d.5.6): 102 entradas en la «a» (40 de 11-200 y 62 de 201-700) y 96 en la «b» (40 y 56), las del
//     ejemplo de d.5.6, códigos en orden ordinal y un conjunto por entrada; el hueco máximo entre primeras veces, 34 (526 → 560);
//   - el registro versionado (specs/combinations_registry_v1.json) es el de la variante publicada, byte a byte, y regenerarlo
//     da los mismos bytes (UTF-8 sin BOM y LF);
//   - lo que sale de la especificación y no del código: max_risk_pairs «libre», K = 13 en los hitos de 561-700 y el tope de
//     mecánicas por rol;
//   - la tabla de temporada de campaign_v1 + campaign_v2 (SE1): 34 ciclos seguidos, 21-681;
//   - los fixtures negativos de P4 (specs/tests) fallan con su regla y, si la declaran, en su variante.
// La salida es ASCII (.claude/rules/ci.md). Desde la raíz del repositorio.
using System.Globalization;
using AmazonFill.Tools.LevelGenerator.Generation;

namespace AmazonFill.Tools.LevelGenerator.Campaign;

internal static class RegistrySelfTest
{
    public const string CampaignV1 = "tools/level-generator/specs/campaign_v1.json";
    public const string CampaignV2 = "tools/level-generator/specs/campaign_v2.json";
    public const string RegistryV1 = "tools/level-generator/specs/combinations_registry_v1.json";

    /// <summary>Fixtures negativos de P4: regla que tiene que fallar y variante (null = cualquiera).</summary>
    public static readonly (string Path, string Rule, string? Variant)[] Fixtures =
    [
        ("tools/level-generator/specs/tests/novelty_gap_61.json", "NOVELTY-60", null),
        ("tools/level-generator/specs/tests/combination_from_v1_not_new.json", "FIRST-TIME", null),
        ("tools/level-generator/specs/tests/variant_b_forbidden.json", "FORBIDDEN", CampaignSpec.VariantB),
        ("tools/level-generator/specs/tests/tinaja_without_fallback.json", "FALLBACK", CampaignSpec.VariantB),
        ("tools/level-generator/specs/tests/hito14_with_capacity.json", "HITO-14", null),
        ("tools/level-generator/specs/tests/variant_with_base.json", "FORBIDDEN", null),
    ];

    /// <summary>Las 19 celdas con la Tinaja de d.5.4 (las que cambia la variante «b»).</summary>
    private static readonly int[] TinajaCells = [323, 324, 325, 327, 329, 332, 338, 340, 352, 355, 392, 454, 494, 531, 574, 591, 631, 651, 691];

    public static int Run()
    {
        int failures = 0;
        void Check(bool ok, string what)
        {
            Console.WriteLine(AsciiText.Of((ok ? "PASS " : "FAIL ") + what));
            failures += ok ? 0 : 1;
        }

        CampaignSpec v1;
        CampaignSpec a;
        CampaignSpec b;
        try
        {
            v1 = CampaignSpec.Load(CampaignV1);
            a = CampaignSpec.Load(CampaignV2);
            b = CampaignSpec.Load(CampaignV2, CampaignSpec.VariantB);
        }
        catch (CampaignSpecException e)
        {
            Console.Error.WriteLine(AsciiText.Of("level-generator --self-test registry: " + e.Message));
            return 2;
        }

        // 1. Variantes (d.5.4).
        Check(a.Variants.SequenceEqual([CampaignSpec.VariantA, CampaignSpec.VariantB]) && a.PublishedVariant == CampaignSpec.VariantA
              && a.Variant == CampaignSpec.VariantA && !v1.HasVariants,
            "campaign_v2: variantes a y b, publicada la a (d.5.8); campaign_v1 con una sola");
        int[] changed = [.. Enumerable.Range(a.FirstLevel, a.LastLevel - a.FirstLevel + 1).Where(n => a.Calendar[n].Literal != b.Calendar[n].Literal)];
        Check(changed.SequenceEqual(TinajaCells) && a.FallbackCells.Keys.Order().SequenceEqual(TinajaCells),
            F($"la variante b cambia exactamente las 19 celdas con la Tinaja de d.5.4 ({changed.Length} celdas)"));
        Check(a.MechanicTable.Any(m => m.Code == MechanicCodes.Tinaja) && b.MechanicTable.All(m => m.Code != MechanicCodes.Tinaja)
              && b.Calendar.Values.All(c => !c.Has(MechanicCodes.Tinaja)) && !b.IntroLevels.ContainsKey(323) && a.IntroLevels[323] == "tinaja",
            "la Tinaja solo en la variante a (only_variant): sin celdas ni intro_mechanic en la b");
        Check(a.Previous?.SpecId == "campaign_v1" && a.SourceIds.SequenceEqual(["campaign_v1", "campaign_v2"]) && a.CellsWithHistory().Count == 690,
            "previous: la historia de campaign_v2 es campaign_v1 (11-200 + 201-700 = 690 celdas)");

        // 2. --validate-spec en verde.
        List<SpecIssue> issuesA = SpecValidator.Validate(a);
        List<SpecIssue> issuesB = SpecValidator.Validate(b);
        List<SpecIssue> issuesV1 = SpecValidator.Validate(v1);
        Check(issuesA.Count(i => !i.Warning) == 0 && issuesB.Count(i => !i.Warning) == 0 && issuesV1.Count(i => !i.Warning) == 0,
            F($"--validate-spec sin fallos: v2 a {issuesA.Count}, v2 b {issuesB.Count}, v1 {issuesV1.Count}"));
        int rulesV1 = SpecValidator.Rules.Count(r => SpecValidator.Applies(r.Id, v1));
        int rulesV2 = SpecValidator.Rules.Count(r => SpecValidator.Applies(r.Id, a));
        Check(rulesV1 == 21 && rulesV2 == 24, F($"reglas aplicadas: {rulesV1} en campaign_v1 (las 21 de siempre) y {rulesV2} en campaign_v2 (+ FALLBACK, HITO-14 y NOVELTY-60)"));

        // 3. Registro (d.5.6).
        List<CombinationEntry> regA = CombinationsRegistry.Build(a.CellsWithHistory(), a.VariantCodes);
        List<CombinationEntry> regB = CombinationsRegistry.Build(b.CellsWithHistory(), b.VariantCodes);
        Check(regA.Count == 102 && regA.Count(e => e.Level <= 200) == 40 && regB.Count == 96 && regB.Count(e => e.Level <= 200) == 40,
            F($"registro: {regA.Count} entradas en la a ({regA.Count(e => e.Level <= 200)} + {regA.Count(e => e.Level > 200)}) y {regB.Count} en la b ({regB.Count(e => e.Level <= 200)} + {regB.Count(e => e.Level > 200)}); D2: 102 (40 + 62) y 96 (40 + 56)"));
        bool Has(List<CombinationEntry> reg, int level, string kind, params string[] codes) =>
            reg.Any(e => e.Level == level && e.Kind == kind && e.Codes.SequenceEqual(codes));
        Check(Has(regA, 14, "debut", "Ch") && Has(regA, 35, "combination", "Al", "Ch") && Has(regA, 243, "variant", "Ln")
              && Has(regA, 320, "combination", "Br", "Ha", "Ll") && Has(regA, 323, "debut", "Ti") && Has(regA, 646, "combination", "Br", "Ha", "Pi"),
            "entradas del ejemplo de d.5.6 (14 Ch, 35 Al+Ch, 243 Ln variant, 320 Br+Ha+Ll) y 323 Ti y 646 (primer reto con tres)");
        Check(Has(regB, 332, "combination", "Ba", "Ho") && regB.All(e => !e.Codes.Contains(MechanicCodes.Tinaja)) && !Has(regA, 332, "combination", "Ba", "Ho"),
            "la b: Ba+Ho en el 332 (la primera vez de 321-340) y ninguna entrada de la Tinaja; en la a, Ba+Ho no es del 332");
        bool ordered = regA.Concat(regB).All(e => e.Codes.SequenceEqual(e.Codes.Order(StringComparer.Ordinal)))
            && Increasing(regA) && Increasing(regB)
            && regA.Select(e => string.Join("+", e.Codes)).Distinct().Count() == regA.Count;
        Check(ordered, "codigos en orden ordinal, entradas por nivel y un conjunto por entrada");
        Check(regA.Where(e => e.Level <= 200).Select(Key).SequenceEqual(CombinationsRegistry.Build(v1.CellsWithHistory(), v1.VariantCodes).Select(Key)),
            "las 40 entradas de 11-200 son las de campaign_v1 sola");

        // 4. Bytes: deterministas e iguales al versionado (la variante publicada).
        byte[] once = CombinationsRegistry.Bytes(a);
        byte[] twice = CombinationsRegistry.Bytes(CampaignSpec.Load(CampaignV2));
        byte[]? onDisk = File.Exists(RegistryV1) ? File.ReadAllBytes(RegistryV1) : null;
        Check(once.AsSpan().SequenceEqual(twice) && onDisk is not null && onDisk.AsSpan().SequenceEqual(once),
            F($"{RegistryV1}: identico al regenerado (variante a, {once.Length} bytes) y dos generaciones dan los mismos bytes"));
        Check(once.Length > 3 && !(once[0] == 0xEF && once[1] == 0xBB && once[2] == 0xBF) && !once.Contains((byte)'\r') && once[^1] == (byte)'\n',
            "UTF-8 sin BOM, LF y salto de linea final");
        Check(!CombinationsRegistry.Bytes(b).AsSpan().SequenceEqual(once), "el registro de la b no es el versionado (cada variante con el suyo, d.5.6)");

        // 5. NOVELTY-60: hueco máximo 34 (526 → 560) en las dos (d.5 punto 5).
        (int gapA, int fromA, int toA) = SpecValidator.MaxNoveltyGap(SpecValidator.NoveltyLevels(a.CellsWithHistory(), regA), a.FirstLevel);
        (int gapB, int fromB, int toB) = SpecValidator.MaxNoveltyGap(SpecValidator.NoveltyLevels(b.CellsWithHistory(), regB), b.FirstLevel);
        Check(a.NoveltyMaxGap == 60 && v1.NoveltyMaxGap is null && (gapA, fromA, toA) == (34, 526, 560) && (gapB, fromB, toB) == (34, 526, 560),
            F($"hueco maximo entre primeras veces: a {gapA} ({fromA} -> {toA}), b {gapB} ({fromB} -> {toB}); D2: 34 (526 -> 560); tope 60 solo en campaign_v2"));

        // 6. Lo que sale de la especificación.
        Check(a.MaxRiskPairsAt(201) == a.RiskPairs.Count && a.RiskPairs.Count == 10 && v1.MaxRiskPairsAt(200) == 2,
            "max_risk_pairs \"libre\" desde el 201 = los 10 pares de riesgo (03 sec. 11); campaign_v1 sin cambios (2 en el 200)");
        Check(a.KAt(580) == 13 && a.KAt(700) == 13 && a.KAt(560) == 11 && a.KAt(578) == 12 && a.KAt(561) == 10,
            "k_plan: K = 13 en los hitos de 561-700 (580 el primero), 11 en el 560, 12 en los retos y 10 en recuperacion del mundo 9");
        Check(a.MaxMechanicsAt(300, SlotRole.Milestone) == 2 && a.MaxMechanicsAt(320, SlotRole.Milestone) == 3 && a.MaxMechanicsAt(301, SlotRole.Challenge) == 3
              && a.MaxMechanicsAt(310, SlotRole.Comfort) == 2 && v1.MaxMechanicsAt(200, SlotRole.Milestone) == 2,
            "mechanics_per_level: 3 en retos desde el 301 y en hitos desde el 320; 2 en el resto y en campaign_v1");
        Check(a.CapacityLimitsBefore == 200 && v1.CapacityLimitsBefore == 200 && !a.HasRamp && v1.HasRamp,
            "capacities_per_level antes del 200 en las dos; campaign_v2 sin rampa");

        // Plan de cada nivel (mundos, rangos, k_plan y ventanas de la especificación) y los estrenos de la 1.0 (intro_mechanic).
        try
        {
            List<LevelPlan> plansA = [.. Enumerable.Range(a.FirstLevel, a.LastLevel - a.FirstLevel + 1).Select(a.Plan)];
            List<LevelPlan> plansB = [.. Enumerable.Range(b.FirstLevel, b.LastLevel - b.FirstLevel + 1).Select(b.Plan)];
            string introsA = string.Join(" ", plansA.Where(p => p.Intro is not null).Select(p => F($"{p.Level}:{p.Intro}")));
            string introsB = string.Join(" ", plansB.Where(p => p.Intro is not null).Select(p => F($"{p.Level}:{p.Intro}")));
            Check(introsA == "243:liana_n 283:hoja_atada 323:tinaja 343:brote_n2 403:flor" && introsB == "243:liana_n 283:hoja_atada 343:brote_n2 403:flor"
                  && plansA.All(p => !p.Ramp && p.MaxRiskPairs == 10) && plansA.Where(p => p.Role == SlotRole.Milestone && p.Level >= 561).All(p => p.K == 13)
                  && plansA.Select(p => p.World.World).Distinct().SequenceEqual([6, 7, 8, 9]),
                F($"plan de 201-700 en las dos variantes (mundos 6-9, sin rampa, 10 pares de riesgo); estrenos a: {introsA}; b: {introsB}"));
        }
        catch (CampaignSpecException e)
        {
            Check(false, "plan de 201-700: " + e.Message);
        }

        // 7. Tabla de temporada (SE1) con las dos especificaciones.
        try
        {
            List<SeasonCycle> cycles = SeasonTableWriter.Cycles([SeasonTableWriter.SourceOf(v1, CampaignV1), SeasonTableWriter.SourceOf(a, CampaignV2)]);
            Check(cycles.Count == 34 && cycles[0].FromLevel == 21 && cycles[^1].FromLevel == 681
                  && cycles.Zip(cycles.Skip(1)).All(p => p.Second.FromLevel == p.First.FromLevel + 20)
                  && cycles.Where(c => c.FromLevel >= 201).All(c => c.Season == a.Seasons[c.FromLevel]),
                F($"--seasons campaign_v1 + campaign_v2: {cycles.Count} ciclos 21-681 seguidos, con las temporadas de d.5 punto 4 desde el 201"));
        }
        catch (SeasonTableException e)
        {
            Check(false, "--seasons campaign_v1 + campaign_v2: " + e.Message);
        }

        // 8. Fixtures negativos de P4.
        foreach ((string path, string rule, string? variant) in Fixtures)
        {
            bool present = false;
            try
            {
                CampaignSpec f = CampaignSpec.Load(path);
                present = f.ExpectRule == rule && f.ExpectVariant == variant && f.Variants
                    .SelectMany(v => SpecValidator.Validate(CampaignSpec.Load(path, v)))
                    .Any(i => !i.Warning && i.Rule == rule && (variant is null || i.Variant == variant));
            }
            catch (CampaignSpecException e)
            {
                Console.WriteLine(AsciiText.Of("  " + e.Message));
            }

            Check(present, F($"fixture {Path.GetFileName(path)}: falla con [{rule}]{(variant is null ? "" : " en la variante " + variant)}"));
        }

        Console.WriteLine(failures == 0 ? "self-test registry: todo PASS" : F($"self-test registry: {failures} FAIL"));
        return failures == 0 ? 0 : 1;
    }

    private static string Key(CombinationEntry e) => F($"{e.Level}:{string.Join("+", e.Codes)}:{e.Kind}");

    private static bool Increasing(List<CombinationEntry> reg) => reg.Zip(reg.Skip(1)).All(p => p.First.Level < p.Second.Level);

    private static string F(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
}
