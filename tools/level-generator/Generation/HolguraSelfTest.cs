// --self-test holgura (P7d de la tanda 4): la holgura repartida en cuias a medias del tope de 14 cuias (02 R1), sin proyecto
// de tests (las herramientas no tienen proyecto de tests en la slnx; ARQUITECTURA-T4 §6). Con campaign_v2.json y semillas
// fijas comprueba:
//   - SlotGenerator.SplitSlackPour sobre un tablero fijo: vierte el tramo entero de la cima (1-3 dosis) de una cuia llena en
//     el último vacío de 4, nunca de una cuia llena de un solo color; conserva las dosis de cada color; la misma semilla da el
//     mismo tablero, y sin vacío o sin cuia con tramo de 1-3 dosis devuelve false sin tocar nada;
//   - la configuración de las etapas (SlotGenerator.Config) en los slots de 561-700 que lo necesitan:
//       * hitos de 14 cuias (K = 13 + 1 vacío; 03 §11; d.5.1): el 580 (Matapi + Brote de dos hojas) y el 600 (Hoja atada +
//         Brote de dos hojas + Barro) reparten la holgura desde la etapa 0, con 14 cuias, K = 13, sin vacío de la etapa y
//         uno de la fuente; el 620 (con la Lluvia, que necesita el vacío de 4) no la reparte en ninguna etapa; ninguna
//         etapa de un hito de 14 cuias cambia K ni pasa de 14 cuias; el 640 (Matapi + Barro + Hoja) no tiene ninguna etapa:
//         la Hoja se lleva el vacío entero y el Matapi y el Barro no caben con K = 13 (pendiente del dueño y del tech-lead);
//       * con un hito de 14 cuias de Matapi + Barro (sin la Hoja), las etapas 1-2 reparten la holgura (relajación del hito);
//       * los retos de 14 cuias con la cueva de la Piraña y el Brote (592 Pi+B2 y 646 Pi+Br+Ha; K = 12): la cuia de holgura
//         sustituye al vacío;
//   - un slot de 11-300 no reparte la holgura en ninguna etapa (los packs 11-300 no cambian; --check lo confirma con los packs).
// Imprime ASCII (PASS / FAIL) y sale con 0 si todo pasa, 1 si algo falla y 2 si no encuentra la especificación.
using AF.Core.Board;
using AF.Core.Determinism;
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
        Before301(v1, spec, Check);

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
        StageConfig?[] At(int level) => [.. Enumerable.Range(0, SlotGenerator.Stages).Select(s => gen.Config(spec.Plan(level), s, dropMurky: false))];

        foreach (int level in new[] { 580, 600 })
        {
            StageConfig? s0 = At(level)[0];
            check(s0 is not null && s0.SplitSlack && s0.K == SpecValidator.HitoK && s0.Empties == 0 && s0.Slack && s0.SourceEmpties == 1
                  && Cuias(s0) == SpecValidator.MaxCuias,
                $"{level} ({spec.Plan(level).Cell.Literal}): la etapa 0 reparte la holgura (K = 13, 14 cuias, sin vacio de la etapa y uno de la fuente)");
        }

        bool fixedShape = new[] { 580, 600, 620, 660, 680, 700 }.All(level => At(level).All(c => c is null || (c.K == SpecValidator.HitoK && Cuias(c) == SpecValidator.MaxCuias)));
        check(fixedShape, "hitos de 14 cuias de 561-700: ninguna etapa cambia K = 13 ni pasa de 14 cuias (QA-HITO-14)");
        StageConfig?[] lluvia = At(620);
        check(lluvia.All(c => c is not null && !c.SplitSlack && c.Empties == 1 && !c.Slack),
            "620 (Ln+Ll+Re): con la Lluvia, que necesita el vacio de 4, ninguna etapa reparte la holgura");
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

    private static void Before301(CampaignSpec v1, CampaignSpec v2, Action<bool, string> check)
    {
        bool none = true;
        foreach ((CampaignSpec spec, int from, int to) in new[] { (v1, v1.FirstLevel, v1.LastLevel), (v2, v2.FirstLevel, 300) })
        {
            var gen = new SlotGenerator(spec);
            for (int level = from; level <= to; level++)
            {
                LevelPlan plan = spec.Plan(level);
                for (int stage = 0; stage < SlotGenerator.Stages; stage++)
                {
                    foreach (bool harder in new[] { false, true })
                    {
                        none &= gen.Config(plan, stage, dropMurky: false, harder) is not { SplitSlack: true };
                    }
                }
            }
        }

        check(none, "11-300: ningun slot reparte la holgura en ninguna etapa (los packs 11-300 no cambian)");
    }
}
