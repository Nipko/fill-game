// --self-test holgura (P7d de la tanda 4): la holgura repartida en cuias a medias del tope de 14 cuias (02 R1), sin proyecto
// de tests (las herramientas no tienen proyecto de tests en la slnx; ARQUITECTURA-T4 §6). Con campaign_v2.json y semillas
// fijas comprueba:
//   - SlotGenerator.SplitSlackPour sobre un tablero fijo: vierte el tramo entero de la cima (1-3 dosis) de una cuia llena en
//     el último vacío de 4, nunca de una cuia llena de un solo color; conserva las dosis de cada color; la misma semilla da el
//     mismo tablero, y sin vacío o sin cuia con tramo de 1-3 dosis devuelve false sin tocar nada;
//   - la configuración de las etapas (SlotGenerator.Config) en los slots de 561-700 que lo necesitan:
//       * hitos de 14 cuias (K = 13 + 1 vacío; 03 §11; d.5.1): el 580 (Matapi + Brote de dos hojas) y el 600 (Hoja atada +
//         Brote de dos hojas + Barro) reparten la holgura desde la etapa 0, con 14 cuias, K = 13, sin vacío de la etapa y
//         uno de la fuente, y también en las etapas 1-2 con endurecimiento; el 620 (con la Lluvia, que necesita el vacío de 4)
//         no la reparte en ninguna etapa y conserva en las etapas 1-2 la fuente deep (endurecimiento de 03 §4.2: deep y deep
//         con 60.000 nodos; sin él, la etapa 1 repite la 0 y la 2 es deep con 60.000 nodos) con K = 13, 1 vacío y 14 cuias;
//         ninguna etapa de un hito de 14 cuias, con y sin endurecimiento, cambia K ni pasa de 14 cuias; el 640 (Matapi +
//         Barro + Hoja) no tiene ninguna etapa: la Hoja se lleva el vacío entero y el Matapi y el Barro no caben con K = 13
//         (pendiente del dueño y del tech-lead);
//       * con un hito de 14 cuias de Matapi + Barro (sin la Hoja), las etapas 1-2 reparten la holgura (relajación del hito);
//       * los retos de 14 cuias con la cueva de la Piraña y el Brote (592 Pi+B2 y 646 Pi+Br+Ha; K = 12): la cuia de holgura
//         sustituye al vacío;
//       * las etapas 1-2 del 592 y el 646, con y sin endurecimiento: null o una configuración posible; solo la relajación
//         «walk con 1 vacío» de la etapa 2 reparte la holgura;
//   - un slot de 11-560 no reparte la holgura en ninguna etapa (los packs 11-300 no cambian; --check lo confirma con los packs);
//   - en 201-700 (variantes a y b), toda etapa con la holgura repartida lleva la cuia de holgura y la fuente walk, y en 561-700
//     solo la reparten los hitos de 14 cuias y los retos de K = 12 + la cueva en la etapa 0 y en la relajación «walk con 1
//     vacío» de la etapa 2 (HolguraSelfTest.SplitLevels).
// Imprime ASCII (PASS / FAIL) y sale con 0 si todo pasa, 1 si algo falla y 2 si no encuentra la especificación.
using AF.Core.Board;
using AF.Core.Determinism;
using AF.Solver.Generation;
using AmazonFill.Tools.LevelGenerator.Campaign;

namespace AmazonFill.Tools.LevelGenerator.Generation;

internal static class HolguraSelfTest
{
    public const string SpecPath = "tools/level-generator/specs/campaign_v2.json";
    public const string CampaignV1Path = "tools/level-generator/specs/campaign_v1.json";

    public static int Run()
    {
        CampaignSpec spec;
        CampaignSpec v1;
        try
        {
            spec = CampaignSpec.Load(SpecPath);
            v1 = CampaignSpec.Load(CampaignV1Path);
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

        SplitPour(Check);
        Stages(spec, Check);
        Scope(v1, Check);

        Console.WriteLine(failures == 0 ? "self-test holgura: todo PASS" : $"self-test holgura: {failures} FAIL");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>Tablero fijo: dos cuias llenas con tramo de 2 en la cima, una llena de un solo color y un vacío al final.</summary>
    private static GenBoard Fixed(bool withEmpty = true, bool onlyMono = false)
    {
        var b = new GenBoard();
        List<int>[] cuias = onlyMono
            ? [[1, 1, 1, 1], [2, 2, 2, 2]]
            : [[1, 1, 2, 2], [2, 2, 1, 1], [3, 3, 3, 3]];
        foreach (List<int> c in cuias)
        {
            b.Caps.Add(Cuia.DefaultCapacity);
            b.Cuias.Add(c);
            b.Attrs.Add(AF.Core.Mechanics.MechanicSpec.None);
        }

        if (withEmpty)
        {
            b.Caps.Add(Cuia.DefaultCapacity);
            b.Cuias.Add([]);
            b.Attrs.Add(AF.Core.Mechanics.MechanicSpec.None);
        }

        return b;
    }

    private static string Text(GenBoard b) => string.Join(" ", b.Cuias.Select(c => "[" + string.Join(",", c) + "]"));

    private static void SplitPour(Action<bool, string> check)
    {
        bool allOk = true;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (ulong seed = 1; seed <= 20; seed++)
        {
            GenBoard b = Fixed();
            bool ok = SlotGenerator.SplitSlackPour(b, new Prng(seed));
            GenBoard again = Fixed();
            SlotGenerator.SplitSlackPour(again, new Prng(seed));
            string text = Text(b);
            seen.Add(text);
            bool shape = text is "[1,1] [2,2,1,1] [3,3,3,3] [2,2]" or "[1,1,2,2] [2,2] [3,3,3,3] [1,1]";
            bool units = b.Units().OrderBy(kv => kv.Key).Select(kv => kv.Value).SequenceEqual([4, 4, 4]);
            allOk &= ok && shape && units && text == Text(again);
        }

        check(allOk && seen.Count == 2,
            "SplitSlackPour: el tramo de 2 de la cima de una cuia llena va entero al vacio (dos cuias a medias, una de un solo color), nunca de la cuia llena de un solo color; las U_c no cambian; misma semilla, mismo tablero; con 20 semillas salen las dos cuias de origen");
        GenBoard noEmpty = Fixed(withEmpty: false);
        GenBoard mono = Fixed(onlyMono: true);
        check(!SlotGenerator.SplitSlackPour(noEmpty, new Prng(1)) && Text(noEmpty) == Text(Fixed(withEmpty: false))
              && !SlotGenerator.SplitSlackPour(mono, new Prng(1)) && Text(mono) == Text(Fixed(onlyMono: true)),
            "SplitSlackPour: sin vacio de 4, o con solo cuias llenas de un color, devuelve false y no toca el tablero");
    }

    private static int Cuias(StageConfig c) => c.K + c.Empties + c.Chicas.Length + (c.Slack ? 1 : 0) + c.Mechanics.Count(m => m.Code == "Pi");

    private static void Stages(CampaignSpec spec, Action<bool, string> check)
    {
        var gen = new SlotGenerator(spec);
        StageConfig?[] At(int level, bool harder = false) =>
            [.. Enumerable.Range(0, SlotGenerator.Stages).Select(s => gen.Config(spec.Plan(level), s, dropMurky: false, harder))];

        foreach (int level in new[] { 580, 600 })
        {
            StageConfig? s0 = At(level)[0];
            check(s0 is not null && s0.SplitSlack && s0.K == SpecValidator.HitoK && s0.Empties == 0 && s0.Slack && s0.SourceEmpties == 1
                  && Cuias(s0) == SpecValidator.MaxCuias,
                $"{level} ({spec.Plan(level).Cell.Literal}): la etapa 0 reparte la holgura (K = 13, 14 cuias, sin vacio de la etapa y uno de la fuente)");
        }

        // Cada hito de 14 cuias de la lista tiene al menos una etapa (el 640, sin ninguna, tiene su comprobación abajo): si todas
        // fueran null, la forma fija no probaría nada.
        bool fixedShape = new[] { 580, 600, 620, 660, 680, 700 }.All(level => new[] { false, true }.All(harder =>
            At(level, harder).Any(c => c is not null)
            && At(level, harder).All(c => c is null || (c.K == SpecValidator.HitoK && Cuias(c) == SpecValidator.MaxCuias))));
        check(fixedShape, "hitos de 14 cuias de 561-700 (580, 600, 620, 660, 680 y 700), con y sin endurecimiento: cada uno con al menos una etapa, y ninguna cambia K = 13 ni pasa de 14 cuias (QA-HITO-14)");
        StageConfig?[] lluvia = At(620);
        StageConfig?[] lluviaHarder = At(620, harder: true);
        check(lluvia.All(c => c is not null && !c.SplitSlack && c.Empties == 1 && !c.Slack)
              && lluviaHarder.All(c => c is not null && !c.SplitSlack && c.Empties == 1 && !c.Slack),
            "620 (Ln+Ll+Re): con la Lluvia, que necesita el vacio de 4, ninguna etapa reparte la holgura, con y sin endurecimiento");
        // Sin la holgura repartida, el hito conserva la fuente deep de las etapas 1-2 (endurecimiento de 03 sec. 4.2 y relajación
        // «deep con 60.000 nodos» del hito), con la forma de la etapa 0: K = 13, 1 vacío y 14 cuias.
        bool Deep(StageConfig? c, int nodes) => c is { Source: "deep", K: SpecValidator.HitoK, Empties: 1, SplitSlack: false }
                                                && c.DeepNodes == nodes && Cuias(c) == SpecValidator.MaxCuias;
        check(Deep(lluviaHarder[1], InverseDeep.DefaultMaxNodes) && Deep(lluviaHarder[2], 60000),
            "620 con endurecimiento: etapa 1 deep y etapa 2 deep con 60.000 nodos (03 sec. 4.2), K = 13, 1 vacio y 14 cuias");
        check(lluvia[1] is not null && lluvia[0] is not null && lluvia[1]!.Source == lluvia[0]!.Source && lluvia[1]!.DeepNodes == lluvia[0]!.DeepNodes
              && Deep(lluvia[2], 60000),
            "620 sin endurecimiento: la etapa 1 repite la fuente de la 0 y la etapa 2 es la relajacion del hito, deep con 60.000 nodos (K = 13, 1 vacio, 14 cuias)");
        foreach (int level in new[] { 580, 600 })
        {
            StageConfig?[] h = At(level, harder: true);
            check(h.Skip(1).All(c => c is { K: SpecValidator.HitoK, SplitSlack: true, Slack: true, Source: "walk" } && Cuias(c) == SpecValidator.MaxCuias),
                $"{level} con endurecimiento: las etapas 1-2 reparten la holgura (walk), con K = 13 y 14 cuias");
        }
        check(At(640).All(c => c is null),
            "640 (Ma+Ba+Ho): ninguna etapa (la Hoja se lleva el vacio entero; el Matapi y el Barro no caben con K = 13)");

        CampaignSpec maBa = WithCell(spec, 640, "Ma+Ba (MD; K = 13)");
        var genMaBa = new SlotGenerator(maBa);
        StageConfig?[] relax = [.. Enumerable.Range(0, SlotGenerator.Stages).Select(s => genMaBa.Config(maBa.Plan(640), s, dropMurky: false))];
        check(relax[0] is { SplitSlack: false, Empties: 1, Slack: false } && relax.Skip(1).All(c => c is { SplitSlack: true, Empties: 0, Slack: true } && Cuias(c) == SpecValidator.MaxCuias),
            "un hito de 14 cuias de Matapi + Barro: la etapa 0 con el vacio y las etapas 1-2 con la holgura repartida (relajacion del hito)");

        foreach (int level in new[] { 592, 646 })
        {
            StageConfig? s0 = At(level)[0];
            check(s0 is not null && s0.SplitSlack && s0.K == 12 && s0.Empties == 0 && s0.Slack && Cuias(s0) == SpecValidator.MaxCuias,
                $"{level} ({spec.Plan(level).Cell.Literal}): K = 12 + la cueva de la Pirana + la cuia de holgura en lugar del vacio (14 cuias)");
            // Etapas 1-2 (con y sin endurecimiento): null o una configuración posible. La relajación de 03 §4.4 hacia fill con 2
            // vacíos pasa de 14 cuias y da null (antes quitaba la holgura y dejaba SplitSlack sin la cuia que reparte); el
            // endurecimiento con deep queda sin la cuia de holgura; solo la relajación «walk con 1 vacío» de la etapa 2 reparte
            // la holgura, con la forma de la etapa 0.
            bool later = true;
            foreach (bool harder in new[] { false, true })
            {
                for (int stage = 1; stage < SlotGenerator.Stages; stage++)
                {
                    StageConfig? c = gen.Config(spec.Plan(level), stage, dropMurky: false, harder);
                    bool walkRelax = stage == 2 && !harder;
                    later &= walkRelax
                        ? c is { SplitSlack: true, Slack: true, Empties: 0, Source: "walk" } && Cuias(c) == SpecValidator.MaxCuias
                        : c is null || (!c.SplitSlack && Cuias(c) <= SpecValidator.MaxCuias);
                }
            }

            check(later, $"{level}: las etapas 1-2, con y sin endurecimiento, son null o una configuracion de 14 cuias como mucho; solo la relajacion walk con 1 vacio de la etapa 2 reparte la holgura (con la cuia de holgura)");
        }
    }

    /// <summary>La especificación con una celda cambiada (copia en memoria por un archivo temporal que la extiende).</summary>
    private static CampaignSpec WithCell(CampaignSpec spec, int level, string cell)
    {
        string dir = Path.Combine(Path.GetTempPath(), "af-holgura-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string path = Path.Combine(dir, "campaign_v2_640.json");
            var root = new System.Text.Json.Nodes.JsonObject
            {
                ["extends"] = Path.GetFullPath(SpecPath),
                ["description"] = "self-test holgura: celda cambiada",
                ["calendar"] = new System.Text.Json.Nodes.JsonObject { [level.ToString(System.Globalization.CultureInfo.InvariantCulture)] = cell },
            };
            File.WriteAllText(path, root.ToJsonString(), new System.Text.UTF8Encoding(false));
            return CampaignSpec.Load(path);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// Niveles de 561-700 que reparten la holgura en alguna etapa (variantes a y b): los hitos de 14 cuias con una mecánica de
    /// cuias a medias desde la etapa 0 (580, 600, 660 y 700), los de 14 cuias sin la Hoja ni la Lluvia en sus etapas 1-2 (680,
    /// Li+Ha+Re) y los retos de K = 12 + la cueva de la Piraña con el Brote en la etapa 0 y en la relajación «walk con 1
    /// vacío» de la etapa 2 (592 y 646) (README, «Holgura repartida»).
    /// </summary>
    internal static readonly int[] SplitLevels = [580, 592, 600, 646, 660, 680, 700];

    private static void Scope(CampaignSpec v1, Action<bool, string> check)
    {
        // 11-560: ningún slot reparte la holgura (los packs 11-300 no cambian; 301-560, de P7b y P7c, generan como antes).
        // 201-700, variantes a y b: SplitSlack ⇒ Slack (la cuia que reparte) y la fuente de la configuración es walk; en 561-700
        // solo en los casos declarados, y en los niveles de SplitLevels.
        bool none = true;
        bool coherent = true;
        bool declared = true;
        var found = new SortedSet<int>();
        var specs = new List<(CampaignSpec Spec, int From, int To)> { (v1, v1.FirstLevel, v1.LastLevel) };
        foreach (string variant in new[] { CampaignSpec.VariantA, CampaignSpec.VariantB })
        {
            CampaignSpec v2 = CampaignSpec.Load(SpecPath, variant);
            specs.Add((v2, v2.FirstLevel, v2.LastLevel));
        }

        foreach ((CampaignSpec spec, int from, int to) in specs)
        {
            var gen = new SlotGenerator(spec);
            for (int level = from; level <= to; level++)
            {
                LevelPlan plan = spec.Plan(level);
                for (int stage = 0; stage < SlotGenerator.Stages; stage++)
                {
                    foreach (bool harder in new[] { false, true })
                    {
                        StageConfig? c = gen.Config(plan, stage, dropMurky: false, harder);
                        if (c is not { SplitSlack: true })
                        {
                            continue;
                        }

                        coherent &= c.Slack && c.Source == "walk";
                        none &= level > 560;
                        bool hito14 = plan.Role == SlotRole.Milestone && c.K == SpecValidator.HitoK;
                        bool challenge = plan.Role == SlotRole.Challenge && c.K == SpecValidator.HitoK - 1 && c.Mechanics.Any(m => m.Code == "Pi")
                                         && (stage == 0 || c.Notes.Contains("relajacion: walk con 1 vacio"));
                        declared &= hito14 || challenge;
                        found.Add(level);
                    }
                }
            }
        }

        check(none, "11-560 (campaign_v1 y campaign_v2 en las variantes a y b): ningun slot reparte la holgura en ninguna etapa, con y sin endurecimiento");
        check(coherent, "201-700, variantes a y b: toda etapa con la holgura repartida lleva la cuia de holgura (SplitSlack => Slack) y la fuente walk en la configuracion");
        check(declared && found.SequenceEqual(SplitLevels),
            $"561-700: la holgura repartida solo en los hitos de 14 cuias y en los retos de K = 12 + la cueva (etapa 0 y relajacion walk con 1 vacio), en {string.Join(", ", SplitLevels)} (sale en {string.Join(", ", found)})");
    }
}
