// Mecánicas (P-9 a P-11; GDD 03 §17 #6 y 02 §9.2): debut de D-007 anterior o igual al nivel, un solo ejemplar en los
// tableros de estreno (CycleChecks), llave presente y la del tutorial en el estreno, mecánica permitida por la celda del
// calendario y como mucho 2 mecánicas por nivel antes del 300. El resto de reglas de cada mecánica (Barro en la capa 2-3,
// U_agua = 4 con las gotas, cueva de la Piraña, pares del Remolino…) las comprueba LevelValidation (QA-VALID).
using System.Globalization;
using AF.Core.Board;
using AF.Core.Mechanics;
using AmazonFill.Tools.LevelGenerator.Campaign;
using AmazonFill.Tools.LevelGenerator.Metrics;

namespace AmazonFill.Tools.LevelQa.Checks;

internal static class MechanicChecks
{
    public static readonly (string Id, string Rule)[] Ids =
    [
        ("QA-MECH-DEBUT", "cada mecanica y capacidad con debut anterior o igual al nivel (D-007; 02 sec. 8)"),
        ("QA-MECH-SPEC", "las mecanicas del nivel estan en su celda del calendario (especificacion)"),
        ("QA-MECH-KEY", "llave presente en el tablero; agua del mundo en la Lluvia; llave del tutorial en el estreno (02 sec. 9.1; D-007)"),
        ("QA-MECH-COUNT", "como mucho 2 mecanicas por nivel antes del 300 (02 sec. 9.2 regla 4)"),
    ];

    /// <summary>Códigos de calendario de las mecánicas presentes (las capacidades no son mecánicas).</summary>
    public static SortedSet<string> Codes(BoardData b)
    {
        var codes = new SortedSet<string>(StringComparer.Ordinal);
        foreach (MechanicSpec a in b.Attrs.Where(a => !a.IsNone))
        {
            codes.Add(a.Type switch
            {
                MechanicType.Hoja => "Ho",
                MechanicType.Brote => "Br",
                MechanicType.Liana => "Li",
                MechanicType.Matapi => "Ma",
                MechanicType.Remolino => "Re",
                MechanicType.Lluvia => "Ll",
                MechanicType.Pirana => "Pi",
                _ => "?",
            });
        }

        if (b.Cuias.Any(c => c.Contains(Cuia.MudColor)))
        {
            codes.Add("Ba");
        }

        if (b.Hidden.Length > 0)
        {
            codes.Add("Tu");
        }

        return codes;
    }

    /// <summary>Ejemplares de mecánica: un attr (el par del Remolino cuenta como uno), cada dosis de Barro y cada cuia turbia.</summary>
    public static int Instances(BoardData b)
    {
        int remolino = b.Attrs.Count(a => a.Type == MechanicType.Remolino);
        int attrs = b.Attrs.Count(a => !a.IsNone && a.Type != MechanicType.Remolino);
        int mud = b.Cuias.Sum(c => c.Count(x => x == Cuia.MudColor));
        int murky = b.Hidden.Select(h => h[0]).Distinct().Count();
        return attrs + (remolino / 2) + mud + murky;
    }

    public static IEnumerable<QaIssue> Run(QaContext ctx, LevelEval e)
    {
        QaLevel lv = e.Level;
        QaIssue Fail(string id, string msg) => new(id, lv.Pack, lv.Level, msg);
        CampaignSpec spec = ctx.Spec;
        BoardData b = lv.Board;
        SortedSet<string> codes = Codes(b);
        foreach (string code in codes)
        {
            MechanicInfo? m = spec.MechanicTable.FirstOrDefault(x => x.Code == code);
            if (m is not null && m.Debut > lv.Level)
            {
                yield return Fail("QA-MECH-DEBUT", string.Create(CultureInfo.InvariantCulture, $"{m.Id} antes de su debut ({m.Debut})"));
            }
        }

        // Capacidades (02 §8): la cueva de la Piraña no cuenta como Cuia chica.
        for (int i = 0; i < b.Caps.Length; i++)
        {
            int cap = b.Caps[i];
            if (cap == Cuia.DefaultCapacity || b.Attrs[i].Type == MechanicType.Pirana)
            {
                continue;
            }

            CapacityInfo? c = spec.CapacityTable.FirstOrDefault(x => x.Cap == cap);
            if (c is not null && c.Debut > lv.Level)
            {
                yield return Fail("QA-MECH-DEBUT", string.Create(CultureInfo.InvariantCulture, $"cuia de capacidad {cap} antes de su debut ({c.Debut})"));
            }
        }

        if (codes.Count > 2 && lv.Level < 300)
        {
            yield return Fail("QA-MECH-COUNT", $"{codes.Count} mecanicas ({string.Join("+", codes)}) antes del 300");
        }

        if (e.Plan is LevelPlan plan)
        {
            var allowed = plan.Mechanics.Select(m => m.Code).ToHashSet();
            foreach (string code in codes.Where(c => !allowed.Contains(c)))
            {
                yield return Fail("QA-MECH-SPEC", $"{code} no esta en la celda del calendario ({plan.Cell.Short})");
            }
        }

        // Llaves.
        var units = new int[Cuia.MaxColor + 1];
        foreach (int c in b.Cuias.SelectMany(x => x))
        {
            units[c]++;
        }

        int water = lv.WaterBase == "black" ? LevelValidation.RioNegroWater : LevelValidation.TapajosWater;
        for (int i = 0; i < b.Attrs.Length; i++)
        {
            MechanicSpec a = b.Attrs[i];
            if (a.Type is MechanicType.Hoja or MechanicType.Liana && (a.Key < 1 || a.Key > 14 || units[a.Key] == 0))
            {
                yield return Fail("QA-MECH-KEY", string.Create(CultureInfo.InvariantCulture, $"cuia {i}: la llave {a.Key} no esta en el tablero"));
            }

            if (a.Type == MechanicType.Lluvia && a.Key != water)
            {
                yield return Fail("QA-MECH-KEY", string.Create(CultureInfo.InvariantCulture, $"cuia {i}: la Lluvia cae con el tinte {a.Key}; el agua del mundo ({lv.WaterBase}) es {water}"));
            }

            if (!a.IsNone && e.Plan is { DebutBoard: true } p)
            {
                MechanicInfo? m = p.Mechanics.FirstOrDefault(x => x.DebutKey is not null && x.Id == MechanicTypes.ToKey(a.Type));
                if (m is not null)
                {
                    int key = a.Type == MechanicType.Brote ? (b.Cuias[i].Length > 0 ? b.Cuias[i][^1] : 0) : a.Key;
                    int expected = spec.Tint(m.DebutKey!).Engine;
                    if (key != expected)
                    {
                        yield return Fail("QA-MECH-KEY", string.Create(CultureInfo.InvariantCulture, $"estreno de {m.Id} con la llave {key}; la del tutorial es {m.DebutKey} ({expected})"));
                    }
                }
            }
        }
    }
}
